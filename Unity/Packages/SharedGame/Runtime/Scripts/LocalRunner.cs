using System;
using System.Diagnostics;
using Nykwil.GekkoNet.Session;
namespace SharedGame {
    /// <summary>Runs every player on this machine without GekkoNet; controller i drives player i.</summary>
    public sealed class LocalRunner : IGameRunner {
        public IGame Game { get; }
        public GameInfo GameInfo { get; }
        public SessionDiagnostics Diagnostics { get; }
        readonly ILocalInputSource source;
        readonly long[] inputs;
        readonly byte[] snapshot;
        bool disposed;
        public LocalRunner(IGame game, ILocalInputSource source) {
            Game = game ?? throw new ArgumentNullException(nameof(game));
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            inputs = new long[game.PlayerCount];
            snapshot = new byte[game.SnapshotSize];
            GameInfo = new GameInfo(game.PlayerCount) { Status = "Local session" };
            Diagnostics = new SessionDiagnostics(game.PlayerCount);
            for (int i = 0; i < inputs.Length; i++) GameInfo.Players[i] = new PlayerConnectionInfo {
                Handle = i, Local = true, State = PlayerConnectState.Running, Progress = 100, CanDisconnect = true
            };
        }
        public float FramesAhead => 0;
        public void Poll() => Diagnostics.BeginUpdate();
        public void EndFrame() { }
        public void Tick() {
            if (disposed) throw new ObjectDisposedException(nameof(LocalRunner));
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < inputs.Length; i++) inputs[i] = GameInfo.Players[i].State == PlayerConnectState.Disconnected ? 0 : source.ReadInput(i, Game.FrameNumber);
            Game.AdvanceFrame(inputs);
            Diagnostics.Current.SimulationMs += SessionDiagnostics.Elapsed(start);
        }
        public void SampleDiagnostics() {
            // No native saves in local play, so sample the current state directly.
            Diagnostics.Current.Frame = Game.FrameNumber;
            Diagnostics.Current.Checksum = SnapshotChecksum.Compute(Game, snapshot);
            Diagnostics.Record();
        }
        public bool DisconnectPlayer(int index) {
            if ((uint)index >= inputs.Length) throw new ArgumentOutOfRangeException(nameof(index));
            GameInfo.Players[index].State = PlayerConnectState.Disconnected;
            GameInfo.Players[index].CanDisconnect = false;
            return false;
        }
        public void Dispose() { disposed = true; }
    }
}