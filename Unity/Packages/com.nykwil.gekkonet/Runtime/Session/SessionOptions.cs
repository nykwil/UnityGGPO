using System;
using System.Net;
using System.Net.Sockets;
namespace Nykwil.GekkoNet.Session {
    /// <summary>Local: all players on this machine. Peer: one local player over UDP. Spectator: watch a host. Stress: local rollback/desync stress test.</summary>
    public enum SessionMode { Local, Peer, Spectator, Stress }
    /// <summary>A numeric unicast IPv4 address and UDP port.</summary>
    [Serializable]
    public sealed class SessionEndpoint {
        public string Address = "127.0.0.1";
        public int Port = 7000;
        /// <summary>Throws ArgumentException unless this is a valid unicast IPv4 endpoint; returns "address:port".</summary>
        public string Validate() {
            if (!IPAddress.TryParse(Address, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork ||
                ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.Broadcast) || ip.GetAddressBytes()[0] >= 224)
                throw new ArgumentException("Use a unicast numeric IPv4 address.");
            if (Port < 1 || Port > 65535) throw new ArgumentException("Ports must be between 1 and 65535.");
            return ip + ":" + Port;
        }
        internal SessionEndpoint Copy() => new SessionEndpoint { Address = Address, Port = Port };
        /// <summary>Loopback roster on consecutive ports. With a proxy port the remote player is reached through it, which
        /// supports only two players: every remote would otherwise share one endpoint.</summary>
        public static SessionEndpoint[] LoopbackRoster(int players, int localPlayer, int basePort, int proxyPort = 0) {
            if (proxyPort > 0 && players != 2) throw new ArgumentException("A proxied loopback roster supports exactly two players.", nameof(proxyPort));
            var roster = new SessionEndpoint[players];
            for (int i = 0; i < players; i++)
                roster[i] = new SessionEndpoint { Port = proxyPort > 0 && i != localPlayer ? proxyPort : basePort + i };
            return roster;
        }
    }
    /// <summary>A remote endpoint the local session talks to. PlayerIndex is -1 for spectators.</summary>
    public readonly struct RemoteActor {
        public readonly SessionEndpoint Endpoint;
        public readonly int PlayerIndex;
        public bool IsSpectator => PlayerIndex < 0;
        public RemoteActor(SessionEndpoint endpoint, int playerIndex) { Endpoint = endpoint; PlayerIndex = playerIndex; }
    }
    /// <summary>Session configuration. Every peer must use the same ordered roster.</summary>
    public sealed class SessionOptions {
        public SessionMode Mode = SessionMode.Local;
        /// <summary>2–4 player endpoints. Array order is the stable simulation player index on every peer; the local entry gives this peer's listening port.</summary>
        public SessionEndpoint[] Players = { new SessionEndpoint(), new SessionEndpoint { Port = 7001 } };
        /// <summary>Roster index of the local player (peer mode).</summary>
        public int LocalPlayerIndex;
        /// <summary>Controller passed to ILocalInputSource for the local player (peer mode). Local and stress modes use the player index.</summary>
        public int LocalControllerId;
        /// <summary>Roster index of the player that relays the session to spectators.</summary>
        public int SpectatorHostIndex;
        /// <summary>UDP port a spectator listens on (spectator mode).</summary>
        public int SpectatorListenPort = 7100;
        /// <summary>Up to 16 spectator endpoints, registered by the spectator host after startup.</summary>
        public SessionEndpoint[] Spectators = Array.Empty<SessionEndpoint>();
        /// <summary>Local input delay in frames (0–8).</summary>
        public int InputDelay = 2;
        /// <summary>Maximum predicted frames before stalling (1–32).</summary>
        public int PredictionLimit = 8;
        /// <summary>Milliseconds without packets before a peer is disconnected.</summary>
        public int DisconnectTimeoutMs = 5000;
        /// <summary>Frames re-simulated per check in stress mode (2–32).</summary>
        public int StressDistance = 8;
        /// <summary>Frames a spectator buffers behind the host (0–120).</summary>
        public int SpectatorDelay = 8;
        /// <summary>
        /// Whether a detected desync throws from Poll or Tick (the default), which stops a managed session. When false the
        /// runner records it in <see cref="SessionDiagnostics.LastDesync"/>, raises <see cref="GekkoRunner.Desynced"/> and keeps running.
        /// </summary>
        public bool ThrowOnDesync = true;
        /// <summary>
        /// How many of the most recent saves a load can restore: the native state ring holds PredictionLimit + 2 states
        /// (StressDistance + 2 in stress mode). A game that keeps its own snapshot ring needs at least this many entries.
        /// </summary>
        public int SavedStateDepth => (Mode == SessionMode.Stress ? StressDistance : PredictionLimit) + 2;
        /// <summary>Throws ArgumentException describing the first invalid setting.</summary>
        public void Validate(int snapshotSize) {
            if (!Enum.IsDefined(typeof(SessionMode), Mode)) throw new ArgumentException("Unknown session mode.");
            if (Players == null || Players.Length < 2 || Players.Length > 4) throw new ArgumentException("Use 2–4 players.");
            if (snapshotSize < 1 || snapshotSize > 1024 * 1024) throw new ArgumentException("Snapshot size must be 1–1048576 bytes.");
            if (InputDelay < 0 || InputDelay > 8 || PredictionLimit < 1 || PredictionLimit > 32 ||
                StressDistance < 2 || StressDistance > 32 || DisconnectTimeoutMs < 1 || DisconnectTimeoutMs > 600000 ||
                SpectatorDelay < 0 || SpectatorDelay > 120 || LocalControllerId < 0)
                throw new ArgumentException("Invalid delay, prediction, timeout, controller or stress configuration.");
            if (LocalPlayerIndex < 0 || LocalPlayerIndex >= Players.Length || SpectatorHostIndex < 0 || SpectatorHostIndex >= Players.Length)
                throw new ArgumentException("Player and host indices must belong to the roster.");
            if (Spectators == null || Spectators.Length > 16) throw new ArgumentException("Use at most 16 spectator endpoints.");
            if (Mode == SessionMode.Local || Mode == SessionMode.Stress) return;
            var endpoints = new System.Collections.Generic.HashSet<string>();
            foreach (var player in Players)
                if (player == null || !endpoints.Add(player.Validate())) throw new ArgumentException("Player endpoints must be unique.");
            foreach (var spectator in Spectators)
                if (spectator == null || !endpoints.Add(spectator.Validate())) throw new ArgumentException("Spectator endpoints must be unique and separate from players.");
            if (Mode == SessionMode.Spectator) {
                if (SpectatorListenPort < 1 || SpectatorListenPort > 65535) throw new ArgumentException("Invalid spectator listening port.");
                foreach (var player in Players)
                    if (IPAddress.IsLoopback(IPAddress.Parse(player.Address)) && player.Port == SpectatorListenPort)
                        throw new ArgumentException("The spectator needs its own listening port.");
            }
        }
        /// <summary>Validates these options for a freshly created game whose roster matches.</summary>
        public void ValidateFor(IGame game) {
            if (game == null) throw new ArgumentNullException(nameof(game));
            Validate(game.SnapshotSize);
            if (game.PlayerCount != Players.Length || game.FrameNumber != 0)
                throw new ArgumentException("Start with a fresh game matching the roster.");
        }
        /// <summary>True when the local peer relays the session to the configured spectators.</summary>
        public bool HostsSpectators => Mode == SessionMode.Peer && LocalPlayerIndex == SpectatorHostIndex;
        /// <summary>True when player <paramref name="index"/> is reached over the network.</summary>
        public bool IsRemotePlayer(int index) =>
            Mode == SessionMode.Peer ? index != LocalPlayerIndex : Mode == SessionMode.Spectator && index == SpectatorHostIndex;
        /// <summary>Every remote endpoint: remote players in roster order, then spectators this peer hosts.</summary>
        public RemoteActor[] RemoteActors() {
            var actors = new System.Collections.Generic.List<RemoteActor>();
            for (int i = 0; i < Players.Length; i++) if (IsRemotePlayer(i)) actors.Add(new RemoteActor(Players[i], i));
            if (HostsSpectators) foreach (var spectator in Spectators) actors.Add(new RemoteActor(spectator, -1));
            return actors.ToArray();
        }
        /// <summary>Deep copy, including the endpoint arrays.</summary>
        public SessionOptions Copy() {
            var copy = (SessionOptions)MemberwiseClone();
            copy.Players = new SessionEndpoint[Players.Length];
            for (int i = 0; i < Players.Length; i++) copy.Players[i] = Players[i]?.Copy();
            copy.Spectators = new SessionEndpoint[Spectators.Length];
            for (int i = 0; i < Spectators.Length; i++) copy.Spectators[i] = Spectators[i]?.Copy();
            return copy;
        }
    }
}