using System;
using System.Diagnostics;
using System.Threading;
using System.IO;
using VectorWar;
using Nykwil.GekkoNet.Session;
internal static class Program {
    sealed class ObservedGame : IGame {
        readonly VwGame game;
        readonly byte[] snapshot;
        public bool Trace;
        public readonly uint[] Checksums = new uint[4096];
        public int PlayerCount => game.PlayerCount;
        public int FrameNumber => game.FrameNumber;
        public int SnapshotSize => game.SnapshotSize;
        public ObservedGame(int count) { game = new VwGame(count); snapshot = new byte[game.SnapshotSize]; }
        public void SaveState(Span<byte> target) => game.SaveState(target);
        public void LoadState(ReadOnlySpan<byte> source) { game.LoadState(source); Record(); }
        public void AdvanceFrame(ReadOnlySpan<long> input) {
            game.AdvanceFrame(input); Record();
            if (Trace && FrameNumber >= 110 && FrameNumber <= 160)
                Console.WriteLine($"TRACE {FrameNumber} {Checksums[FrameNumber % Checksums.Length]:X8} {input[0]} {input[1]} {input[2]} {input[3]}");
        }
        void Record() => Checksums[FrameNumber % Checksums.Length] = SnapshotChecksum.Compute(game, snapshot);
    }
    static int Main(string[] args) {
        try {
            if (args.Length > 0 && args[0].StartsWith("peer")) { Peer(args); return 0; }
            Run("ABI and C booleans", CoreChecks.Layouts);
            Run("Snapshot round trip, rejection and byte coverage", CoreChecks.Snapshots);
            Run("Configuration validation", CoreChecks.Options);
            Run("Desync reporting and game-supplied checksums", CoreChecks.Desync);
            Run("SaveState only in native save events", CoreChecks.SaveDiscipline);
            Run("Socket failure, ownership and repeated cleanup", CoreChecks.Lifecycle);
            Run("100000 native stress updates", () => CoreChecks.Stress());
            Run("Zero managed allocation hot path", () => {
                long bytes = CoreChecks.Allocations(); Console.WriteLine("Allocated bytes: " + bytes);
                CoreChecks.Require(bytes == 0, "Hot path allocations: " + bytes);
            });
            return 0;
        } catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    static void Run(string title, Action action) { var watch = Stopwatch.StartNew(); action(); Console.WriteLine($"PASS {title} ({watch.Elapsed.TotalSeconds:F2}s)"); }
    static void Peer(string[] args) {
        int count = int.Parse(args[1]), local = int.Parse(args[2]), frames = int.Parse(args[3]), basePort = int.Parse(args[4]);
        bool spectate = local < 0;
        var roster = SessionEndpoint.LoopbackRoster(count, local, basePort, args.Length > 5 && !spectate ? int.Parse(args[5]) : 0);
        var options = new SessionOptions { Mode = spectate ? SessionMode.Spectator : SessionMode.Peer, Players = roster,
            LocalPlayerIndex = Math.Max(0, local), SpectatorListenPort = basePort + 10,
            Spectators = new[] { new SessionEndpoint { Port = basePort + 10 } }, InputDelay = 2 };
        var input = new CoreChecks.SequenceInput();
        var game = new ObservedGame(count) { Trace = args[0] == "peer-trace" };
        using (var runner = new GekkoRunner(game, input, options)) {
            var watch = Stopwatch.StartNew(); double next = 0;
            float peakTx = 0, peakRx = 0;
            double lastSimulationMs = 0;
            while (game.FrameNumber < frames + 120 && watch.Elapsed.TotalSeconds < 45) {
                runner.Poll();
                if (args[0] == "peer-drop" && game.FrameNumber >= 120) {
                    runner.DisconnectPlayer(local);
                    var leaving = Stopwatch.StartNew();
                    while (leaving.Elapsed.TotalSeconds < 1) { runner.Poll(); Thread.Sleep(1); }
                    Console.WriteLine("LEFT player=" + local);
                    return;
                }
                if (watch.Elapsed.TotalSeconds >= next) {
                    input.Frame = game.FrameNumber; runner.Tick(); next += 1.0 / 60;
                    lastSimulationMs = runner.Diagnostics.Current.SimulationMs;
                    if (game.FrameNumber % 60 == 0) {
                        runner.SampleDiagnostics();
                        for (int i = 0; i < count; i++) if (runner.Diagnostics.NetworkAvailable[i]) {
                            peakTx = Math.Max(peakTx, runner.Diagnostics.Network[i].KbSent);
                            peakRx = Math.Max(peakRx, runner.Diagnostics.Network[i].KbReceived);
                        }
                    }
                }
                Thread.Sleep(1);
            }
            // Keep sending pending output while slower peers finish, avoiding a test-induced
            // timeout caused by the first process closing its socket at the target frame.
            var settle = Stopwatch.StartNew();
            while (settle.Elapsed.TotalSeconds < (args.Length > 5 ? 8 : 2)) { runner.Poll(); Thread.Sleep(1); }
            runner.SampleDiagnostics();
            var sample = runner.Diagnostics.Current;
            Console.WriteLine($"METRICS frame={game.FrameNumber} rollbacks={sample.Rollback.TotalCount} depth={sample.Rollback.MaxFrames} stalls={sample.Rollback.TotalStalls} simMs={lastSimulationMs:F3} networkMs={sample.NetworkMs:F3}");
            Console.WriteLine($"BANDWIDTH peakTx={peakTx:F3} peakRx={peakRx:F3} nativeUnits=KiB-per-second");
            for (int i = 0; i < count; i++) if (runner.Diagnostics.NetworkAvailable[i]) {
                var network = runner.Diagnostics.Network[i];
                Console.WriteLine($"NETWORK player={i} pingMs={network.AveragePing:F3} jitterMs={network.Jitter:F3} txKBps={network.KbSent:F3} rxKBps={network.KbReceived:F3}");
            }
            foreach (var player in runner.GameInfo.Players) Console.WriteLine($"PLAYER {player.Handle} {player.State} {player.Progress}");
            CoreChecks.Require(game.FrameNumber >= frames + 120, "Network timeout at frame " + game.FrameNumber);
            Console.WriteLine($"RESULT frame={frames} checksum={game.Checksums[frames % game.Checksums.Length]:X8} desyncs={runner.Diagnostics.DesyncCount}");
        }
    }
}
