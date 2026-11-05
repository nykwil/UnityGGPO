using System;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using SharedGame;
using VectorWar;
using Nykwil.GekkoNet;
public static class CoreChecks {
    public static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Reject(Action action) {
        bool rejected = false;
        try { action(); } catch (ArgumentException) { rejected = true; }
        Require(rejected, "Malformed input was accepted");
    }
    public static uint Hash(IGame game) {
        return SnapshotChecksum.Compute(game, new byte[game.SnapshotSize]);
    }
    public static void Snapshots() {
        foreach (int count in new[] { 2, 3, 4 }) {
            var game = new VwGame(count); var other = new VwGame(count);
            var inputs = new long[count];
            for (int f = 0; f < 500; f++) { for (int p = 0; p < count; p++) inputs[p] = (f * 17 + p * 11) & 63; game.AdvanceFrame(inputs); }
            var bytes = new byte[game.SnapshotSize]; game.SaveState(bytes); other.LoadState(bytes);
            Require(Hash(game) == Hash(other), "Snapshot round trip");
            Reject(() => other.LoadState(new byte[bytes.Length - 1]));
            bytes[4] ^= 1; Reject(() => other.LoadState(bytes)); bytes[4] ^= 1;
            bytes[8] ^= 1; Reject(() => other.LoadState(bytes)); bytes[8] ^= 1;
            uint before = Hash(other);
            bytes[48] = 2; Reject(() => other.LoadState(bytes));
            Require(Hash(other) == before, "Malformed load mutated state");
            game.SaveState(bytes);
            uint baseline = SnapshotChecksum.Compute(bytes);
            // Every serialized byte, including inactive bullets, influences the checksum.
            for (int b = 0; b < bytes.Length; b++) {
                bytes[b] ^= 1; Require(SnapshotChecksum.Compute(bytes) != baseline, "Checksum omitted byte " + b); bytes[b] ^= 1;
            }
        }
    }
    public static void Options() {
        var scheduler = new SessionScheduler(); scheduler.Reset(0);
        Require(scheduler.TicksDue(0) == 1 && scheduler.TicksDue(0) == 0, "Initial pacing");
        Require(scheduler.TicksDue(10) == 4 && scheduler.TicksDue(10) == 0, "Catch-up is not bounded");
        var options = new SessionOptions { Mode = SessionMode.Peer };
        options.Validate(100);
        options.Players[1].Port = 7000; Reject(() => options.Validate(100));
        options.Players[1].Port = 7001;
        options.Players[1].Address = "invalid"; Reject(() => options.Validate(100));
        options.Players[1].Address = "0.0.0.0"; Reject(() => options.Validate(100));
        options.Players[1].Address = "127.0.0.1";
        options.LocalPlayerIndex = 2; Reject(() => options.Validate(100));
        options.LocalPlayerIndex = 0; options.PredictionLimit = 0; Reject(() => options.Validate(100));
    }
    public static unsafe void Layouts() {
        Require(sizeof(Native.NetworkAdapter) == 24 && sizeof(Native.NetworkResult) == 32 &&
            Marshal.OffsetOf<Native.NetworkAdapter>("Receive").ToInt32() == 8 &&
            Marshal.OffsetOf<Native.NetworkAdapter>("Free").ToInt32() == 16 &&
            Marshal.OffsetOf<Native.NetworkResult>("Length").ToInt32() == 16 &&
            Marshal.OffsetOf<Native.NetworkResult>("Data").ToInt32() == 24, "Custom adapter ABI");
        Require(sizeof(Native.Config) == 24 && Marshal.OffsetOf<Native.Config>("SpectatorDelay").ToInt32() == 4 &&
            Marshal.OffsetOf<Native.Config>("LimitedSaving").ToInt32() == 16 && Marshal.OffsetOf<Native.Config>("CheckDistance").ToInt32() == 20, "Config ABI");
        Require(sizeof(Native.Address) == 16 && sizeof(Native.Advance) == 24 && sizeof(Native.Save) == 32 &&
            sizeof(Native.Load) == 16 && sizeof(Native.GameData) == 32 && sizeof(Native.GameEvent) == 40 &&
            Marshal.OffsetOf<Native.GameEvent>("Data").ToInt32() == 8, "Game event ABI");
        Require(sizeof(Native.SessionData) == 16 && sizeof(Native.SessionEvent) == 20 &&
            sizeof(Native.NetworkStats) == 20 && sizeof(Native.RollbackStats) == 20, "Statistics/session ABI");
        Require(Marshal.OffsetOf<Native.Save>("Portable").ToInt32() == 4 && Marshal.OffsetOf<Native.Save>("Checksum").ToInt32() == 8 &&
            Marshal.OffsetOf<Native.Advance>("RollingBack").ToInt32() == 16 && Marshal.OffsetOf<Native.Advance>("RunningAhead").ToInt32() == 17, "C bool ABI");
        Require((int)Native.GameEventType.Empty == -1 && (int)Native.GameEventType.Load == 2 &&
            (int)Native.SessionEventType.DesyncDetected == 6 && (int)Native.SessionType.Spectate == 2 &&
            (int)Native.PlayerType.Spectator == 2, "Enum ABI");
        IntPtr session = IntPtr.Zero;
        Require(Native.gekko_create(ref session, Native.SessionType.Stress), "C bool create");
        Require(!Native.gekko_create(ref session, Native.SessionType.Stress), "C bool false");
        Require(Native.gekko_destroy(ref session) && session == IntPtr.Zero, "C bool destroy");
    }
    public sealed class SequenceInput : ILocalInputSource {
        public int Frame;
        // Uses the caller-set Frame so straight-line reference simulations can share the sequence.
        public long ReadInput(int controllerId, int frame) => (Frame * 17 + controllerId * 11) & 63;
    }
    public static void Stress(int updates = 100000) {
        var game = new VwGame(4); var input = new SequenceInput();
        var expected = new VwGame(4); var inputs = new long[4];
        using (var runner = new GekkoRunner(game, input, new SessionOptions {
            Mode = SessionMode.Stress, InputDelay = 0,
            Players = new[] { new SessionEndpoint(), new SessionEndpoint(), new SessionEndpoint(), new SessionEndpoint() }
        })) {
            for (int f = 0; f < updates; f++) {
                input.Frame = f;
                for (int p = 0; p < 4; p++) inputs[p] = input.ReadInput(p, f);
                expected.AdvanceFrame(inputs); runner.Tick();
                if (f % 997 == 0) Require(Hash(expected) == Hash(game), "Stress differs from straight simulation at " + f);
            }
            Require(Hash(expected) == Hash(game), "Stress final mismatch");
            Require(runner.Diagnostics.DesyncCount == 0, "Native stress desync");
        }
    }
    sealed class EmptyTransport : IGekkoNetworkTransport {
        public IntPtr NativeAdapter => IntPtr.Zero;
        public int Disposals;
        public void Poll() { }
        public void Flush() { }
        public void Dispose() { Disposals++; }
    }
    public static void Lifecycle() {
        var transport = new EmptyTransport();
        bool transportFailed = false;
        try { new GekkoRunner(new VwGame(2), new SequenceInput(), new SessionOptions { Mode = SessionMode.Peer }, _ => transport); }
        catch (InvalidOperationException) { transportFailed = true; }
        Require(transportFailed, "Empty custom adapter was accepted");
        Require(transport.Disposals == 1, "Custom adapter not disposed after failed startup");
        bool factoryCalled = false;
        using (var stress = new GekkoRunner(new VwGame(2), new SequenceInput(), new SessionOptions { Mode = SessionMode.Stress }, _ => {
            factoryCalled = true; return transport;
        })) { stress.Tick(); }
        Require(!factoryCalled, "Stress opened a network transport");
        var source = new SequenceInput();
        var options = new SessionOptions { Mode = SessionMode.Peer, Players = new[] {
            new SessionEndpoint { Port = 29100 }, new SessionEndpoint { Port = 29101 }
        }};
        using (var occupied = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)) {
            occupied.ExclusiveAddressUse = true;
            occupied.Bind(new IPEndPoint(IPAddress.Any, 29100));
            bool failed = false;
            try { using (var runner = new GekkoRunner(new VwGame(2), source, options)) { } }
            catch (InvalidOperationException) { failed = true; }
            Require(failed, "Occupied port was accepted");
        }
        for (int i = 0; i < 20; i++) {
            using (var runner = new GekkoRunner(new VwGame(2), source, options)) {
                bool rejected = false;
                try { using (var duplicate = new GekkoRunner(new VwGame(2), source, options)) { } }
                catch (InvalidOperationException) { rejected = true; }
                Require(rejected, "Multiple UDP sessions accepted");
                runner.Poll(); runner.Tick();
            }
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                socket.Bind(new IPEndPoint(IPAddress.Any, 29100));
        }
        var diagnostics = new SessionDiagnostics(2);
        for (int i = 0; i < 10000; i++) diagnostics.Record();
        Require(diagnostics.Count == SessionDiagnostics.HistoryCapacity, "Unbounded history");
    }
    public static long Allocations() {
        var game = new VwGame(4); var source = new SequenceInput();
        using (var runner = new GekkoRunner(game, source, new SessionOptions { Mode = SessionMode.Stress, InputDelay = 0,
            Players = new[] { new SessionEndpoint(), new SessionEndpoint(), new SessionEndpoint(), new SessionEndpoint() } })) {
            for (int i = 0; i < 1000; i++) { source.Frame = i; runner.Tick(); runner.SampleDiagnostics(); }
            long threadCalibration = GC.GetAllocatedBytesForCurrentThread();
            var threadProbe = new byte[128];
            bool threadCounterAvailable = GC.GetAllocatedBytesForCurrentThread() > threadCalibration;
            GC.KeepAlive(threadProbe);
            if (threadCounterAvailable) {
                long threadBefore = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 10000; i++) { source.Frame = i; runner.Tick(); runner.SampleDiagnostics(); }
                return GC.GetAllocatedBytesForCurrentThread() - threadBefore;
            }
#if UNITY_6000_0_OR_NEWER
            using (var bytes = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory, "GC Allocated In Frame", 1))
            using (var events = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory, "GC.Alloc", 32,
                Unity.Profiling.ProfilerRecorderOptions.StartImmediately | Unity.Profiling.ProfilerRecorderOptions.CollectOnlyOnCurrentThread)) {
                long calibration = bytes.CurrentValue;
                var probe = new byte[128];
                Require(bytes.CurrentValue > calibration && events.Count > 0, "Unity allocation counters failed calibration.");
                GC.KeepAlive(probe);
                long before = bytes.CurrentValue;
                int eventCount = events.Count;
                for (int i = 0; i < 10000; i++) { source.Frame = i; runner.Tick(); runner.SampleDiagnostics(); }
                return events.Count == eventCount ? 0 : Math.Max(1, bytes.CurrentValue - before);
            }
#else
            throw new InvalidOperationException("No calibrated allocation counter is available.");
#endif
        }
    }
}
