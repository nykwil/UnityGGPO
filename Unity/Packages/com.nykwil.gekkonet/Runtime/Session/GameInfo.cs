namespace Nykwil.GekkoNet.Session {
    /// <summary>Connection state of one roster player. Unavailable means the local session cannot observe it (e.g. non-host players seen by a spectator).</summary>
    public enum PlayerConnectState { Connecting, Synchronizing, Running, Disconnected, Unavailable }
    public struct PlayerConnectionInfo {
        /// <summary>Native actor handle, or -1 when not registered.</summary>
        public int Handle;
        /// <summary>True when this player's input is produced on this machine.</summary>
        public bool Local;
        public PlayerConnectState State;
        /// <summary>Synchronization progress, 0–100.</summary>
        public int Progress;
        /// <summary>Set by the runner: whether DisconnectPlayer is currently allowed for this player.</summary>
        public bool CanDisconnect;
    }
    /// <summary>Runner-owned, read-only view of session status for UI. Indexed by roster player.</summary>
    public sealed class GameInfo {
        public readonly PlayerConnectionInfo[] Players;
        /// <summary>Human-readable session status, e.g. "Running" or "Buffering spectator inputs".</summary>
        public string Status = "Connecting";
        public GameInfo(int count) { Players = new PlayerConnectionInfo[count]; }
    }
}
