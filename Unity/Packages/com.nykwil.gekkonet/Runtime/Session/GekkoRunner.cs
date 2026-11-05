using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
namespace Nykwil.GekkoNet.Session {
    /// <summary>
    /// Runs a GekkoNet peer, spectator or stress session. Owns one native session; call and dispose it on its
    /// creating thread. Without a transport factory (or when it returns null) the built-in UDP adapter is used,
    /// which allows one network session per process.
    /// </summary>
    public sealed unsafe class GekkoRunner : IGameRunner {
        static int adapterOwned;
        readonly int ownerThread = Environment.CurrentManagedThreadId;
        readonly SessionOptions options;
        readonly ILocalInputSource source;
        readonly long[] inputs;
        IntPtr session;
        bool ownsAdapter, disposed;
        int hostHandle = -1;
        // Frame and checksum of the most recent native save, reported by SampleDiagnostics.
        int lastSaveFrame = -1;
        uint lastSaveChecksum;
        bool registerSpectators;
        IGekkoNetworkTransport transport;
        public IGame Game { get; }
        public GameInfo GameInfo { get; }
        public SessionDiagnostics Diagnostics { get; }
        /// <summary>
        /// Raised from Poll or Tick for each detected desync when <see cref="SessionOptions.ThrowOnDesync"/> is false.
        /// To end the session, set a flag and dispose the runner after Poll or Tick returns.
        /// </summary>
        public event Action<DesyncReport> Desynced;

        public GekkoRunner(IGame game, ILocalInputSource source, SessionOptions options,
            Func<SessionOptions, IGekkoNetworkTransport> transportFactory = null) {
            Game = game ?? throw new ArgumentNullException(nameof(game));
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            if (options == null) throw new ArgumentNullException(nameof(options));
            options.ValidateFor(game);
            if (options.Mode == SessionMode.Local) throw new ArgumentException("Use LocalRunner for local play.");
            this.options = options.Copy();
            inputs = new long[game.PlayerCount];
            GameInfo = new GameInfo(game.PlayerCount);
            Diagnostics = new SessionDiagnostics(game.PlayerCount) {
                NativeMetricsAvailable = options.Mode != SessionMode.Stress,
                RollbackMetricsAvailable = options.Mode == SessionMode.Peer
            };
            for (int i = 0; i < game.PlayerCount; i++) GameInfo.Players[i].Handle = -1;
            try {
                bool network = options.Mode != SessionMode.Stress;
                IntPtr adapter = IntPtr.Zero;
                // A factory may return null to use the built-in UDP adapter.
                if (network) transport = transportFactory?.Invoke(this.options.Copy());
                if (transport != null) {
                    adapter = transport.NativeAdapter;
                    if (adapter == IntPtr.Zero) throw new InvalidOperationException("Transport returned an empty native adapter.");
                } else if (network) {
                    if (Interlocked.CompareExchange(ref adapterOwned, 1, 0) != 0)
                        throw new InvalidOperationException("The built-in UDP adapter supports one network session per process.");
                    ownsAdapter = true;
                    int port = options.Mode == SessionMode.Spectator ? options.SpectatorListenPort : options.Players[options.LocalPlayerIndex].Port;
                    adapter = Native.gekko_default_adapter((ushort)port);
                    if (adapter == IntPtr.Zero) throw new InvalidOperationException("Cannot open UDP port " + port + ". It may already be in use.");
                }
                var type = options.Mode == SessionMode.Stress ? Native.SessionType.Stress :
                    options.Mode == SessionMode.Spectator ? Native.SessionType.Spectate : Native.SessionType.Game;
                if (!Native.gekko_create(ref session, type)) throw new InvalidOperationException("Native session creation failed.");
                var config = new Native.Config {
                    Players = (byte)game.PlayerCount, Spectators = (byte)options.Spectators.Length,
                    PredictionWindow = (byte)options.PredictionLimit, SpectatorDelay = (uint)options.SpectatorDelay,
                    InputSize = sizeof(long), StateSize = (uint)game.SnapshotSize,
                    DesyncDetection = 1, CheckDistance = (uint)options.StressDistance
                };
                Native.gekko_start(session, ref config);
                if (network) Native.gekko_net_adapter_set(session, adapter);
                Native.gekko_set_disconnect_timeout(session, (uint)options.DisconnectTimeoutMs);
                Native.gekko_set_input_prediction_window(session, (byte)options.PredictionLimit);
                if (options.Mode == SessionMode.Spectator) {
                    hostHandle = AddRemote(Native.PlayerType.Remote, options.Players[options.SpectatorHostIndex]);
                    for (int i = 0; i < game.PlayerCount; i++) GameInfo.Players[i].State = PlayerConnectState.Unavailable;
                    GameInfo.Players[options.SpectatorHostIndex] = new PlayerConnectionInfo { Handle = hostHandle, State = PlayerConnectState.Connecting, CanDisconnect = true };
                    GameInfo.Status = "Synchronizing with spectator host";
                } else {
                    for (int i = 0; i < game.PlayerCount; i++) {
                        bool local = !options.IsRemotePlayer(i);
                        int handle = local ? Native.gekko_add_actor(session, Native.PlayerType.Local, null) :
                            AddRemote(Native.PlayerType.Remote, options.Players[i]);
                        if (handle < 0) throw new InvalidOperationException("Native player registration failed.");
                        GameInfo.Players[i] = new PlayerConnectionInfo { Handle = handle, Local = local,
                            State = local ? PlayerConnectState.Running : PlayerConnectState.Connecting, Progress = local ? 100 : 0,
                            CanDisconnect = options.Mode != SessionMode.Stress };
                        if (local) Native.gekko_set_local_delay(session, handle, (byte)options.InputDelay);
                    }
                    registerSpectators = options.HostsSpectators;
                    if (options.Mode == SessionMode.Stress) GameInfo.Status = "Rollback stress session";
                }
            } catch { Dispose(); throw; }
        }
        int AddRemote(Native.PlayerType type, SessionEndpoint endpoint) {
            byte[] bytes = Encoding.ASCII.GetBytes(endpoint.Validate());
            fixed (byte* pointer = bytes) {
                var address = new Native.Address { Data = pointer, Size = (uint)bytes.Length };
                int handle = Native.gekko_add_actor(session, type, &address);
                if (handle < 0) throw new InvalidOperationException("Native actor registration failed.");
                return handle;
            }
        }
        void CheckAlive() {
            if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Use the session on its creating thread.");
            if (disposed) throw new ObjectDisposedException(nameof(GekkoRunner));
        }
        public void Poll() {
            CheckAlive();
            Diagnostics.BeginUpdate();
            long start = Stopwatch.GetTimestamp();
            transport?.Poll();
            Native.gekko_network_poll(session);
            ConsumeSessionEvents();
            Diagnostics.Current.NetworkMs += Elapsed(start);
        }
        public float FramesAhead { get { CheckAlive(); return Native.gekko_frames_ahead(session); } }
        static double Elapsed(long start) => SessionDiagnostics.Elapsed(start);
        public void Tick() {
            CheckAlive();
            long start = Stopwatch.GetTimestamp();
            if (options.Mode != SessionMode.Spectator) {
                for (int i = 0; i < inputs.Length; i++) if (GameInfo.Players[i].Local) {
                    long input = source.ReadInput(options.Mode == SessionMode.Stress ? i : options.LocalControllerId, Game.FrameNumber);
                    Native.gekko_add_local_input(session, GameInfo.Players[i].Handle, &input);
                }
            }
            // Every pointer below belongs to this update only. Saves can feed later loads in the same batch.
            long networkStart = Stopwatch.GetTimestamp();
            var events = Native.gekko_update_session(session, out int count);
            double networkMs = Elapsed(networkStart);
            for (int i = 0; i < count; i++) {
                var ev = *events[i];
                switch ((Native.GameEventType)ev.Type) {
                    case Native.GameEventType.Save:
                        var save = ev.Data.Save;
                        var destination = new Span<byte>(save.State, Game.SnapshotSize);
                        Game.SaveState(destination);
                        *save.Length = (uint)Game.SnapshotSize;
                        uint checksum = SnapshotChecksum.Compute(Game, Game.FrameNumber, destination);
                        *save.Checksum = checksum;
                        lastSaveFrame = Game.FrameNumber;
                        lastSaveChecksum = checksum;
                        break;
                    case Native.GameEventType.Load:
                        if (ev.Data.Load.Length != Game.SnapshotSize) throw new InvalidOperationException("Native snapshot size mismatch.");
                        Game.LoadState(new ReadOnlySpan<byte>(ev.Data.Load.State, Game.SnapshotSize));
                        break;
                    case Native.GameEventType.Advance:
                        if (ev.Data.Advance.InputLength != inputs.Length * sizeof(long)) throw new InvalidOperationException("Native input size mismatch.");
                        // Copy unaligned native inputs explicitly; no native pointer escapes this call.
                        var bytes = new ReadOnlySpan<byte>(ev.Data.Advance.Inputs, (int)ev.Data.Advance.InputLength);
                        for (int p = 0; p < inputs.Length; p++)
                            inputs[p] = System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian(bytes.Slice(p * sizeof(long), sizeof(long)));
                        Game.AdvanceFrame(inputs);
                        break;
                    default: throw new InvalidOperationException("Unexpected native game event.");
                }
            }
            // Native update resets its session events, so they are read after every update.
            ConsumeSessionEvents();
            Diagnostics.Current.NetworkMs += networkMs;
            Diagnostics.Current.SimulationMs += Elapsed(start) - networkMs;
        }
        public void EndFrame() {
            CheckAlive();
            if (transport == null) return;
            long start = Stopwatch.GetTimestamp();
            transport.Flush();
            Diagnostics.Current.NetworkMs += Elapsed(start);
        }
        void ConsumeSessionEvents() {
            var events = Native.gekko_session_events(session, out int count);
            // Poll and update each expose only their own recent events.
            for (int i = 0; i < count; i++) {
                var ev = *events[i];
                switch ((Native.SessionEventType)ev.Type) {
                    case Native.SessionEventType.PlayerSyncing:
                        SetPlayerState(ev.Data.Handle, PlayerConnectState.Synchronizing, ev.Data.Maximum == 0 ? 0 : ev.Data.Current * 100 / ev.Data.Maximum);
                        break;
                    case Native.SessionEventType.PlayerConnected: SetPlayerState(ev.Data.Handle, PlayerConnectState.Running, 100); break;
                    case Native.SessionEventType.PlayerDisconnected: SetPlayerState(ev.Data.Handle, PlayerConnectState.Disconnected, 0); break;
                    case Native.SessionEventType.Started:
                        GameInfo.Status = "Running";
                        // Pre-start spectators participate in the native startup barrier. Register
                        // them after it opens, so absent spectators never hold the players back.
                        if (registerSpectators) {
                            registerSpectators = false;
                            foreach (var endpoint in options.Spectators) AddRemote(Native.PlayerType.Spectator, endpoint);
                        }
                        break;
                    case Native.SessionEventType.SpectatorPaused: GameInfo.Status = "Buffering spectator inputs"; break;
                    case Native.SessionEventType.SpectatorUnpaused: GameInfo.Status = "Spectating"; break;
                    case Native.SessionEventType.DesyncDetected:
                        // Native frame f is saved after its advance, when the game's FrameNumber is f + 1.
                        var report = new DesyncReport(ev.Data.Handle + 1, options.Mode == SessionMode.Stress ? -1 : PlayerIndexOf(ev.Data.RemoteHandle),
                            ev.Data.LocalChecksum, ev.Data.RemoteChecksum);
                        Diagnostics.DesyncCount++;
                        Diagnostics.LastDesync = report;
                        if (options.ThrowOnDesync) throw new InvalidOperationException(report.ToString());
                        Desynced?.Invoke(report);
                        if (disposed) return; // the handler ended the session; the native events are gone
                        break;
                }
            }
        }
        int PlayerIndexOf(int handle) {
            for (int p = 0; p < GameInfo.Players.Length; p++) if (GameInfo.Players[p].Handle == handle) return p;
            return -1;
        }
        void SetPlayerState(int handle, PlayerConnectState state, int progress) {
            for (int p = 0; p < GameInfo.Players.Length; p++) {
                if (GameInfo.Players[p].Handle != handle) continue;
                GameInfo.Players[p].State = state;
                GameInfo.Players[p].Progress = progress;
                if (state == PlayerConnectState.Disconnected) GameInfo.Players[p].CanDisconnect = false;
            }
        }
        public void SampleDiagnostics() {
            CheckAlive();
            // Never saves: a game's SaveState runs only inside native save events.
            Diagnostics.Current.Frame = lastSaveFrame;
            Diagnostics.Current.Checksum = lastSaveChecksum;
            Diagnostics.Current.FramesAhead = FramesAhead;
            Diagnostics.Current.SpectatorBufferedFrames = Native.gekko_spectator_buffered_frames(session);
            Native.gekko_rollback_stats(session, out Diagnostics.Current.Rollback);
            for (int i = 0; i < inputs.Length; i++) {
                bool available = options.IsRemotePlayer(i);
                Diagnostics.NetworkAvailable[i] = available;
                if (available) Native.gekko_network_stats(session, options.Mode == SessionMode.Spectator ? hostHandle : GameInfo.Players[i].Handle, out Diagnostics.Network[i]);
            }
            Diagnostics.Record();
        }
        public bool DisconnectPlayer(int index) {
            CheckAlive();
            if ((uint)index >= inputs.Length) throw new ArgumentOutOfRangeException(nameof(index));
            bool leaving = options.Mode == SessionMode.Spectator || options.Mode == SessionMode.Peer && GameInfo.Players[index].Local;
            if (options.Mode == SessionMode.Spectator && index != options.SpectatorHostIndex)
                throw new InvalidOperationException("A spectator can disconnect only from its host.");
            if (!Native.gekko_disconnect_actor(session, GameInfo.Players[index].Handle)) throw new InvalidOperationException("Native disconnect failed.");
            transport?.Flush();
            return leaving;
        }
        public void Dispose() {
            if (disposed) return;
            if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Dispose on the creating thread.");
            disposed = true;
            try { if (session != IntPtr.Zero) Native.gekko_destroy(ref session); }
            finally {
                transport?.Dispose();
                transport = null;
                if (ownsAdapter) {
                    try { Native.gekko_default_adapter_destroy(); }
                    finally { ownsAdapter = false; Interlocked.Exchange(ref adapterOwned, 0); }
                }
            }
        }
    }
}
