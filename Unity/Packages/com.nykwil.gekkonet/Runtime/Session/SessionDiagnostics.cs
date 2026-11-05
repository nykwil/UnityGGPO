namespace Nykwil.GekkoNet.Session {
    /// <summary>One diagnostics sample. Timings cover the presentation update in which the sample was taken.</summary>
    public struct DiagnosticSample {
        /// <summary>
        /// GekkoRunner: the game frame of the most recent native save (FrameNumber at that save), which may be predicted or,
        /// during a rollback, an earlier re-simulated frame; -1 before the first save. LocalRunner: the current frame.
        /// </summary>
        public int Frame;
        /// <summary>
        /// GekkoRunner: the checksum written for that save (0 before the first save). LocalRunner: the current state's checksum.
        /// Native desync detection compares confirmed frames only.
        /// </summary>
        public uint Checksum;
        /// <summary>CPU milliseconds spent simulating and in network work during the update.</summary>
        public double SimulationMs, NetworkMs;
        /// <summary>Whole local-minus-remote frame gap at the sample; see <see cref="IGameRunner.FramesAhead"/>.</summary>
        public float FramesAhead;
        public int SpectatorBufferedFrames;
        /// <summary>Cumulative rollback count, maximum depth and stalls (peer sessions only).</summary>
        public Native.RollbackStats Rollback;
    }
    /// <summary>A desync reported by a remote peer: both peers' checksums for the same confirmed frame differ.</summary>
    public readonly struct DesyncReport {
        /// <summary>Game frame whose checksums differ: the <see cref="IGame.FrameNumber"/> at that save, as passed to <see cref="ISnapshotChecksum"/>.</summary>
        public readonly int Frame;
        /// <summary>Roster index of the peer that reported it (the host, on a spectator), or -1 for a spectator this peer hosts or a stress-session self-check.</summary>
        public readonly int PlayerIndex;
        public readonly uint LocalChecksum, RemoteChecksum;
        public DesyncReport(int frame, int playerIndex, uint localChecksum, uint remoteChecksum) {
            Frame = frame; PlayerIndex = playerIndex; LocalChecksum = localChecksum; RemoteChecksum = remoteChecksum;
        }
        public override string ToString() =>
            $"Desync at frame {Frame}, player {PlayerIndex}: {LocalChecksum:X8} vs {RemoteChecksum:X8}.";
    }
    /// <summary>Diagnostics owned by a runner: the current sample, a 300-entry history ring (30 s at 10 Hz) and per-player network stats.</summary>
    public sealed class SessionDiagnostics {
        public const int HistoryCapacity = 300;
        public readonly DiagnosticSample[] History = new DiagnosticSample[HistoryCapacity];
        public readonly Native.NetworkStats[] Network;
        /// <summary>Whether <see cref="Network"/>[i] holds real stats for player i (remote players only).</summary>
        public readonly bool[] NetworkAvailable;
        public DiagnosticSample Current;
        /// <summary>Valid entries in <see cref="History"/>.</summary>
        public int Count { get; private set; }
        /// <summary>Index the next sample will be written to; the newest sample is at Next - 1 (wrapping).</summary>
        public int Next { get; private set; }
        public bool NativeMetricsAvailable;
        public bool RollbackMetricsAvailable;
        /// <summary>Desyncs detected. With <see cref="SessionOptions.ThrowOnDesync"/> (the default) the runner throws on the first.</summary>
        public int DesyncCount;
        /// <summary>The most recent desync; valid when <see cref="DesyncCount"/> is above 0.</summary>
        public DesyncReport LastDesync;
        public SessionDiagnostics(int count) {
            Network = new Native.NetworkStats[count];
            NetworkAvailable = new bool[count];
        }
        /// <summary>Milliseconds since a Stopwatch timestamp.</summary>
        public static double Elapsed(long start) =>
            (System.Diagnostics.Stopwatch.GetTimestamp() - start) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        /// <summary>Clears per-update timings. Runners call this at the start of Poll and accumulate in Tick.</summary>
        public void BeginUpdate() { Current.SimulationMs = 0; Current.NetworkMs = 0; }
        /// <summary>Appends <see cref="Current"/> to the history ring.</summary>
        public void Record() {
            History[Next] = Current;
            Next = (Next + 1) % HistoryCapacity;
            if (Count < HistoryCapacity) Count++;
        }
    }
}
