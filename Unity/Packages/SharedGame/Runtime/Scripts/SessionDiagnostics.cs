using Nykwil.GekkoNet;
namespace SharedGame {
    /// <summary>One diagnostics sample. Timings cover the presentation update in which the sample was taken.</summary>
    public struct DiagnosticSample {
        /// <summary>Presented frame; may be predicted.</summary>
        public int Frame;
        /// <summary>Checksum of the presented (possibly predicted) state. Native desync detection compares confirmed frames.</summary>
        public uint Checksum;
        /// <summary>CPU milliseconds spent simulating and in network work during the update.</summary>
        public double SimulationMs, NetworkMs;
        public float FramesAhead;
        public int SpectatorBufferedFrames;
        /// <summary>Cumulative rollback count, maximum depth and stalls (peer sessions only).</summary>
        public Native.RollbackStats Rollback;
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
        /// <summary>Native desyncs detected; the runner throws on the first, which stops a managed session.</summary>
        public int DesyncCount;
        public SessionDiagnostics(int count) {
            Network = new Native.NetworkStats[count];
            NetworkAvailable = new bool[count];
        }
        /// <summary>Milliseconds since a Stopwatch timestamp.</summary>
        public static double Elapsed(long start) =>
            (System.Diagnostics.Stopwatch.GetTimestamp() - start) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        /// <summary>Clears per-update timings. Runners call this at the start of Poll and accumulate in Tick.</summary>
        public void BeginUpdate() { Current.SimulationMs = 0; Current.NetworkMs = 0; }
        /// <summary>Sets Current.Frame and Current.Checksum from the game, using scratch as the snapshot buffer.</summary>
        public void SampleState(IGame game, byte[] scratch) {
            Current.Frame = game.FrameNumber;
            Current.Checksum = SnapshotChecksum.Compute(game, scratch);
        }
        /// <summary>Appends <see cref="Current"/> to the history ring.</summary>
        public void Record() {
            History[Next] = Current;
            Next = (Next + 1) % HistoryCapacity;
            if (Count < HistoryCapacity) Count++;
        }
    }
}
