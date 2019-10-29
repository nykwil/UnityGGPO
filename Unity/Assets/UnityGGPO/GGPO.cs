using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public enum GGPOPlayerType {
    GGPO_PLAYERTYPE_LOCAL,
    GGPO_PLAYERTYPE_REMOTE,
    GGPO_PLAYERTYPE_SPECTATOR,
}

[Serializable]
public struct GGPOPlayer {
    public GGPOPlayerType type;
    public int player_num;
    public string ip_address;
    public short port;
}

public class GGPONetworkStats {
    public int send_queue_len;
    public int recv_queue_len;
    public int ping;
    public int kbps_sent;
    public int local_frames_behind;
    public int remote_frames_behind;
}

public static class GGPOC {
    public static bool GGPO_SUCCEEDED(int result) {
        return result == GGPO_OK;
    }

    public const int GGPO_MAX_PLAYERS = 4;
    public const int GGPO_MAX_PREDICTION_FRAMES = 8;
    public const int GGPO_MAX_SPECTATORS = 32;

    public const int GGPO_OK = 0;
    public const int GGPO_INVALID_HANDLE = -1;

    public const int GGPO_ERRORCODE_SUCCESS = 0;
    public const int GGPO_ERRORCODE_GENERAL_FAILURE = -1;
    public const int GGPO_ERRORCODE_INVALID_SESSION = 1;
    public const int GGPO_ERRORCODE_INVALID_PLAYER_HANDLE = 2;
    public const int GGPO_ERRORCODE_PLAYER_OUT_OF_RANGE = 3;
    public const int GGPO_ERRORCODE_PREDICTION_THRESHOLD = 4;
    public const int GGPO_ERRORCODE_UNSUPPORTED = 5;
    public const int GGPO_ERRORCODE_NOT_SYNCHRONIZED = 6;
    public const int GGPO_ERRORCODE_IN_ROLLBACK = 7;
    public const int GGPO_ERRORCODE_INPUT_DROPPED = 8;
    public const int GGPO_ERRORCODE_PLAYER_DISCONNECTED = 9;
    public const int GGPO_ERRORCODE_TOO_MANY_SPECTATORS = 10;
    public const int GGPO_ERRORCODE_INVALID_REQUEST = 11;

    public const int GGPO_EVENTCODE_CONNECTED_TO_PEER = 1000;
    public const int GGPO_EVENTCODE_SYNCHRONIZING_WITH_PEER = 1001;
    public const int GGPO_EVENTCODE_SYNCHRONIZED_WITH_PEER = 1002;
    public const int GGPO_EVENTCODE_RUNNING = 1003;
    public const int GGPO_EVENTCODE_DISCONNECTED_FROM_PEER = 1004;
    public const int GGPO_EVENTCODE_TIMESYNC = 1005;
    public const int GGPO_EVENTCODE_CONNECTION_INTERRUPTED = 1006;
    public const int GGPO_EVENTCODE_CONNECTION_RESUMED = 1007;
}

public static class GGPO {

    const string libraryName = "UnityGGPO";

    static string version;

    public delegate void LogDelegate(string text);

    public delegate bool BeginGameDelegate(string text);

    public delegate bool AdvanceFrameDelegate(int flags);

    unsafe public delegate bool LoadGameStateDelegate(void* buffer, int length);

    unsafe public delegate bool LogGameStateDelegate(string text, void* buffer, int length);

    public unsafe delegate void* SaveGameStateDelegate(out int length, out int checksum, int frame);

    unsafe public delegate void FreeBufferDelegate(void* buffer);

    public delegate bool OnEventConnectedToPeerDelegate(int connected_player);

    public delegate bool OnEventSynchronizingWithPeerDelegate(int synchronizing_player, int synchronizing_count, int synchronizing_total);

    public delegate bool OnEventSynchronizedWithPeerDelegate(int synchronizing_player);

    public delegate bool OnEventRunningDelegate();

    public delegate bool OnEventConnectionInterruptedDelegate(int connection_interrupted_player, int connection_interrupted_disconnect_timeout);

    public delegate bool OnEventConnectionResumedDelegate(int connection_resumed_player);

    public delegate bool OnEventDisconnectedFromPeerDelegate(int disconnected_player);

    public delegate bool OnEventEventcodeTimesyncDelegate(int timesync_frames_ahead);

    public unsafe delegate bool RealSaveGameStateDelegate(void** buffer, int* len, int* checksum, int frame);

    public delegate bool RealOnEventDelegate(IntPtr info);

    unsafe public static void* ToPtr(NativeArray<byte> data) {
        unsafe {
            return NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(data);
        }
    }

    unsafe public static NativeArray<byte> ToArray(void* dataPointer, int length) {
        unsafe {
            return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(dataPointer, length, Allocator.Persistent);
        }
    }

    public static string Version {
        get {
            if (version == null) {
                var ptr = DllPluginVersion();
                if (ptr != IntPtr.Zero) {
                    version = Marshal.PtrToStringAnsi(ptr);
                }
            }
            return version;
        }
    }

    public static int BuildNumber {
        get {
            return DllPluginBuildNumber();
        }
    }

    [DllImport(libraryName, CharSet = CharSet.Ansi)]
    static extern IntPtr DllPluginVersion();

    [DllImport(libraryName)]
    static extern int DllPluginBuildNumber();

    [DllImport(libraryName)]
    public static extern void DllSetLogDelegate(LogDelegate callback);

    [DllImport(libraryName)]
    public static extern void DllTestRealSaveGameDelegate(RealSaveGameStateDelegate realSaveGameCallback, FreeBufferDelegate freeBufferCallback);

    [DllImport(libraryName)]
    public static extern void DllTestRealOnEventDelegate(RealOnEventDelegate realOnEventCallback);

    [DllImport(libraryName)]
    public static extern void DllTestGameStateDelegates(
        SaveGameStateDelegate saveGameState,
        LogGameStateDelegate logGameState,
        LoadGameStateDelegate loadGameState,
        FreeBufferDelegate freeBuffer);

    [DllImport(libraryName)]
    public static extern int DllTestStartSession(
        BeginGameDelegate beginGame,
        AdvanceFrameDelegate advanceFrame,
        LoadGameStateDelegate loadGameState,
        LogGameStateDelegate logGameState,
        SaveGameStateDelegate saveGameState,
        FreeBufferDelegate freeBuffer,
        OnEventConnectedToPeerDelegate onEventConnectedToPeer,
        OnEventSynchronizingWithPeerDelegate onEventSynchronizingWithPeer,
        OnEventSynchronizedWithPeerDelegate onEventSynchronizedWithPeer,
        OnEventRunningDelegate onEventRunning,
        OnEventConnectionInterruptedDelegate onEventConnectionInterrupted,
        OnEventConnectionResumedDelegate onEventConnectionResumedDelegate,
        OnEventDisconnectedFromPeerDelegate onEventDisconnectedFromPeerDelegate,
        OnEventEventcodeTimesyncDelegate onEventTimesyncDelegate,
        string game, int num_players, int localport);

    [DllImport(libraryName)]
    public static extern int DllStartSession(
            BeginGameDelegate beginGame,
            AdvanceFrameDelegate advanceFrame,
            LoadGameStateDelegate loadGameState,
            LogGameStateDelegate logGameState,
            SaveGameStateDelegate saveGameState,
            FreeBufferDelegate freeBuffer,
            OnEventConnectedToPeerDelegate onEventConnectedToPeer,
            OnEventSynchronizingWithPeerDelegate onEventSynchronizingWithPeer,
            OnEventSynchronizedWithPeerDelegate onEventSynchronizedWithPeer,
            OnEventRunningDelegate onEventRunning,
            OnEventConnectionInterruptedDelegate onEventConnectionInterrupted,
            OnEventConnectionResumedDelegate onEventConnectionResumedDelegate,
            OnEventDisconnectedFromPeerDelegate onEventDisconnectedFromPeerDelegate,
            OnEventEventcodeTimesyncDelegate onEventTimesyncDelegate,
            string game, int num_players, int localport);

    [DllImport(libraryName)]
    public static extern int DllStartSpectating(BeginGameDelegate begin_game,
            AdvanceFrameDelegate advance_frame,
            LoadGameStateDelegate load_game_state,
            LogGameStateDelegate log_game_state,
            SaveGameStateDelegate save_game_state,
            FreeBufferDelegate free_buffer,
            OnEventConnectedToPeerDelegate on_even_connected_to_peer,
            OnEventSynchronizingWithPeerDelegate on_event_synchronizing_with_peer,
            OnEventSynchronizedWithPeerDelegate on_event_synchronized_withpeer,
            OnEventRunningDelegate onEventRunningDelegate,
            OnEventConnectionInterruptedDelegate onEventConnectionInterruptedDelegate,
            OnEventConnectionResumedDelegate onEventConnectionResumedDelegate,
            OnEventDisconnectedFromPeerDelegate onEventDisconnectedFromPeerDelegate,
            OnEventEventcodeTimesyncDelegate onEventEventcodeTimesyncDelegate,
            string game, int num_players, int localport, string host_ip, int host_port);

    [DllImport(libraryName)]
    public static extern int DllSetDisconnectNotifyStart(int ggpo, int timeout);

    [DllImport(libraryName)]
    public static extern int DllSetDisconnectTimeout(int ggpo, int timeout);

    [DllImport(libraryName)]
    public static extern int DllSynchronizeInput(int ggpo, ulong[] inputs, int length, out int disconnect_flags);

    [DllImport(libraryName)]
    public static extern int DllAddLocalInput(int ggpo, int local_player_handle, ulong input);

    [DllImport(libraryName)]
    public static extern int DllCloseSession(int ggpo);

    [DllImport(libraryName)]
    public static extern int DllIdle(int ggpo, int time);

    [DllImport(libraryName)]
    public static extern int DllAddPlayer(int ggpo,
            int player_type,
            int player_num,
            string player_ip_address,
            short player_port,
            out int handle);

    [DllImport(libraryName)]
    public static extern int DllDisconnectPlayer(int ggpo, int phandle);

    [DllImport(libraryName)]
    public static extern int DllSetFrameDelay(int ggpo, int phandle, int frame_delay);

    [DllImport(libraryName)]
    public static extern int DllAdvanceFrame(int ggpo);

    [DllImport(libraryName, CharSet = CharSet.Ansi)]
    public static extern void DllLog(int ggpo, string v);

    [DllImport(libraryName)]
    public static extern int DllGetNetworkStats(int ggpo, int phandle,
        out int send_queue_len,
        out int recv_queue_len,
        out int ping,
        out int kbps_sent,
        out int local_frames_behind,
        out int remote_frames_behind
    );
}
