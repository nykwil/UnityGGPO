using System;
using System.Buffers.Binary;
namespace Nykwil.GekkoNet.Session {
    /// <summary>A deterministic simulation. Identical inputs must produce bit-identical state on every peer.</summary>
    public interface IGame {
        int PlayerCount { get; }
        /// <summary>Number of frames simulated; 0 for a fresh game.</summary>
        int FrameNumber { get; }
        /// <summary>Fixed snapshot size in bytes (1–1048576).</summary>
        int SnapshotSize { get; }
        /// <summary>Writes exactly SnapshotSize bytes of complete simulation state. Do not retain the span.</summary>
        void SaveState(Span<byte> destination);
        /// <summary>Restores a snapshot from SaveState. Reject incompatible data before modifying any state.
        /// A load only restores one of the most recent <see cref="SessionOptions.SavedStateDepth"/> saves, so a game
        /// that saves a handle into its own snapshot ring needs at least that many entries.</summary>
        void LoadState(ReadOnlySpan<byte> source);
        /// <summary>Simulates one frame with one input per player, in roster order. Do not retain the span.</summary>
        void AdvanceFrame(ReadOnlySpan<long> inputs);
    }
    /// <summary>
    /// Optional for an <see cref="IGame"/>: supplies the checksum peers compare for desync detection instead of the
    /// default hash of the saved bytes. Use it when the saved bytes are not comparable between peers, e.g. a game that
    /// saves only a frame number or a handle to its own snapshot.
    /// </summary>
    /// <remarks>
    /// Peers compare the checksum of each newly confirmed frame, taken from that frame's latest save, so this is
    /// called for roughly every frame. An expensive canonical hash can be computed on every Nth frame only, returning
    /// a constant (e.g. 0) on all other frames: every peer sees the same frame numbers, so the constant frames always
    /// match and only the interval frames are really compared. The result must depend only on simulation state and
    /// <paramref name="frame"/>, never on local history such as allocation order.
    /// </remarks>
    public interface ISnapshotChecksum {
        /// <summary>Checksum for the state just saved into <paramref name="saved"/> for <paramref name="frame"/>.</summary>
        uint Checksum(int frame, ReadOnlySpan<byte> saved);
    }
    public interface ILocalInputSource {
        /// <summary>Samples local input for <paramref name="frame"/>, the latest simulated frame when the input is read.</summary>
        long ReadInput(int controllerId, int frame);
    }
    /// <summary>Runner-owned native adapter. Dispose only after the native session.</summary>
    public interface IGekkoNetworkTransport : IDisposable {
        IntPtr NativeAdapter { get; }
        void Poll();
        void Flush();
    }
    /// <summary>Drives one session. Call Poll, then Tick zero or more times, then EndFrame, once per presentation update, on the creating thread.</summary>
    public interface IGameRunner : IDisposable {
        IGame Game { get; }
        GameInfo GameInfo { get; }
        SessionDiagnostics Diagnostics { get; }
        /// <summary>Live frames-ahead estimate used for pacing; 0 when there is no remote peer.</summary>
        float FramesAhead { get; }
        /// <summary>Receives network traffic and resets per-update timings.</summary>
        void Poll();
        /// <summary>Samples local input and advances the session by one frame, including any rollback.</summary>
        void Tick();
        /// <summary>Called once per presentation update after all ticks, e.g. to flush outgoing packets.</summary>
        void EndFrame();
        /// <summary>Records a diagnostics sample into <see cref="Diagnostics"/>.</summary>
        void SampleDiagnostics();
        /// <summary>Disconnects a player. Returns true when the local session has ended as a result.</summary>
        bool DisconnectPlayer(int index);
    }
    public static class SnapshotChecksum {
        const ulong Offset = 14695981039346656037, Prime = 1099511628211;
        /// <summary>FNV-1a over little-endian 64-bit words (then trailing bytes), folded to 32 bits.</summary>
        public static uint Compute(ReadOnlySpan<byte> bytes) {
            ulong hash = Offset;
            int whole = bytes.Length & ~(sizeof(ulong) - 1);
            for (int i = 0; i < whole; i += sizeof(ulong))
                hash = unchecked((hash ^ BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(i))) * Prime);
            for (int i = whole; i < bytes.Length; i++) hash = unchecked((hash ^ bytes[i]) * Prime);
            return (uint)(hash ^ (hash >> 32));
        }
        /// <summary>The game's own checksum when it implements <see cref="ISnapshotChecksum"/>, otherwise the default hash.</summary>
        public static uint Compute(IGame game, int frame, ReadOnlySpan<byte> saved) =>
            game is ISnapshotChecksum custom ? custom.Checksum(frame, saved) : Compute(saved);
        /// <summary>Saves the game's current state into the scratch buffer and checksums it as <see cref="Compute(IGame, int, ReadOnlySpan{byte})"/> does.</summary>
        public static uint Compute(IGame game, byte[] scratch) {
            game.SaveState(scratch);
            return Compute(game, game.FrameNumber, scratch);
        }
    }
}
