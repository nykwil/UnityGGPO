using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public enum GGPOPlayerType {
    GGPO_PLAYERTYPE_LOCAL,
    GGPO_PLAYERTYPE_REMOTE,
    GGPO_PLAYERTYPE_SPECTATOR,
}

public class GGPOSession {
    public int handle;

    public GGPO.SafeLoadGameStateDelegate loadGameStateCallback;
    public GGPO.SafeLoadGameStateDelegate logGameStateCallback;
    public GGPO.SafeSaveGameStateDelegate saveGameStateCallback;

    GGPO.LoadGameStateDelegate _loadGameStateCallback;
    GGPO.LogGameStateDelegate _logGameStateCallback;
    GGPO.SaveGameStateDelegate _saveGameStateCallback;

    internal Action<string> logDelegate;

    public void Init() {
        unsafe {
            _loadGameStateCallback = cb1;
            _logGameStateCallback = cb2;
            _saveGameStateCallback = cb3;
        }
    }

    public void GetNetworkStats(int p, out GGPONetworkStats stats) {
        stats = new GGPONetworkStats();
        GGPO.ggpo_get_network_stats(handle, p,
            out stats.send_queue_len,
            out stats.recv_queue_len,
            out stats.ping,
            out stats.kbps_sent,
            out stats.local_frames_behind,
            out stats.remote_frames_behind
        );
    }

    public void SetDisconnectNotifyStart(int v) {
        GGPO.ggpo_set_disconnect_notify_start(handle, v);
    }

    public void SetDisconnectTimeout(int v) {
        GGPO.ggpo_set_disconnect_timeout(handle, v);
    }

    public int SynchronizeInput(out byte[] inputs, out int disconnect_flags) {
        return GGPO.ggpo_synchronize_input(handle, out inputs, out disconnect_flags);
    }

    public int AddLocalInput(int local_player_handle, byte[] inputs) {
        return GGPO.ggpo_add_local_input(handle, local_player_handle, inputs);
    }

    public void ggpo_close_session() {
        GGPO.ggpo_close_session(handle);
    }

    public void ggpo_idle(int time) {
        GGPO.ggpo_idle(handle, time);
    }

    public int ggpo_add_player(int ggpo, GGPOPlayer player, out int handle) {
        return GGPO.ggpo_add_player(this.handle,
                player.size,
        (int)player.type,
    player.player_num,
        player.ip_address,
        player.port,
out handle);
    }

    public int ggpo_disconnect_player(int ggpo, int handle) {
        return GGPO.ggpo_disconnect_player(this.handle, handle);
    }

    public void ggpo_set_frame_delay(int ggpo, int handle, int frame_delay) {
        GGPO.ggpo_set_frame_delay(this.handle, handle, frame_delay);
    }

    public void AdvanceFrame(int ggpo) {
        GGPO.ggpo_advance_frame(handle);
    }

    public void Log(int ggpo, string v) {
        GGPO.ggpo_log(handle, v);
    }

    private unsafe void* cb3(out int length, out int checksum, int frame) {
        var data = saveGameStateCallback(out checksum, frame);
        length = data.Length;
        return GGPO.ToPtr(data);
    }

    private unsafe bool cb2(void* buffer, int length) {
        return logGameStateCallback(GGPO.ToArray(buffer, length));
    }

    unsafe public bool cb1(void* buffer, int length) {
        return loadGameStateCallback(GGPO.ToArray(buffer, length));
    }
}

[Serializable]
public struct GGPOPlayer {
    public int size;
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

public class GGPOSessionCallbacks {
    public GGPO.BeginGameDelegate begin_game;

    public GGPO.AdvanceFrameDelegate advance_frame;

    public GGPO.LoadGameStateDelegate load_game_state;

    public GGPO.LogGameStateDelegate log_game_state;

    public GGPO.SaveGameStateDelegate save_game_state;
}

public class GGPO {
    private static string version;

    private const string libraryName = "GGPOPlugin";

    public delegate void LogDelegate(string text);

    public delegate bool BeginGameDelegate();

    public delegate bool AdvanceFrameDelegate(int flags);

    public delegate bool SafeLoadGameStateDelegate(NativeArray<byte> data);

    public delegate bool SafeLogGameStateDelegate(NativeArray<byte> data);

    public delegate NativeArray<byte> SafeSaveGameStateDelegate(out int checksum, int frame);

    unsafe public delegate bool LoadGameStateDelegate([MarshalAs(UnmanagedType.LPArray)] void* buffer, int length);

    unsafe public delegate bool LogGameStateDelegate([MarshalAs(UnmanagedType.LPArray)] void* buffer, int length);

    [return: MarshalAs(UnmanagedType.LPArray)]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void* SaveGameStateDelegate(out int length, out int checksum, int frame);

    unsafe public delegate void FreeBufferDelegate(void* buffer, int length);

    public delegate bool OnEventConnectedToPeerDelegate(int connected_player);

    public delegate bool OnEventSynchronizingWithPeerDelegate(int synchronizing_player, int synchronizing_count, int synchronizing_total);

    public delegate bool OnEventSynchronizedWithPeerDelegate(int synchronizing_player);

    public delegate bool OnEventRunningDelegate();

    public delegate bool OnEventConnectionInterruptedDelegate(int connection_interrupted_player, int connection_interrupted_disconnect_timeout);

    public delegate bool OnEventConnectionResumedDelegate(int connection_resumed_player);

    public delegate bool OnEventDisconnectedFromPeerDelegate(int disconnected_player);

    public delegate bool OnEventEventcodeTimesyncDelegate(int timesync_frames_ahead);

    [DllImport(libraryName, CharSet = CharSet.Ansi)]
    private static extern IntPtr GetPluginVersion();

    [DllImport(libraryName)]
    private static extern int GetPluginBuildNumber();

    [DllImport(libraryName)]
    public static extern void TestSaveGameStateDelegate(SaveGameStateDelegate callback);

    [DllImport(libraryName)]
    public static extern void SetLogDelegate(LogDelegate callback);

    [DllImport(libraryName)]
    public static extern void TestLogGameStateDelegate(LogGameStateDelegate callback);

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
                IntPtr ptr = GetPluginVersion();
                if (ptr != IntPtr.Zero) {
                    version = Marshal.PtrToStringAnsi(ptr);
                }
            }
            return version;
        }
    }

    public static int BuildNumber {
        get {
            return GetPluginBuildNumber();
        }
    }

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

    //public delegate bool BeginGameDelegate(string name);

    //public delegate bool AdvanceFrameDelegate(int flags);

    //public delegate bool LoadGameStateDelegate(byte[] buffer);

    //public delegate void FreeBufferDelegate(byte[] buffer);

    //public delegate bool OnEventDelegate(int code, int player, int synchronizingcount, int synchronizingtotal, int disconnect_timeout, int frames_ahead);

    //public delegate bool LogGameStateDelegate(string filename, byte[] buffer);

    //public delegate bool SaveGameStateDelegate(ref byte[] buffer, ref int len, ref int checksum, int frame);

    [DllImport(libraryName)]
    public static extern int ggpo_start_session(
        BeginGameDelegate begin_game,
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
            string game, int num_players, int input_size, int localport);

    [DllImport(libraryName)]
    public static extern int ggpo_start_spectating(BeginGameDelegate begin_game,
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
            string game, int num_players, int input_size, int localport, string host_ip, int host_port);

    [DllImport(libraryName)]
    public static extern void ggpo_set_disconnect_notify_start(int ggpo, int v);

    [DllImport(libraryName)]
    public static extern void ggpo_set_disconnect_timeout(int ggpo, int v);

    [DllImport(libraryName)]
    public static extern int ggpo_synchronize_input(int ggpo, out byte[] inputs, out int disconnect_flags);

    [DllImport(libraryName)]
    public static extern int ggpo_add_local_input(int ggpo, int local_player_handle, byte[] inputs);

    [DllImport(libraryName)]
    public static extern void ggpo_close_session(int ggpo);

    [DllImport(libraryName)]
    public static extern void ggpo_idle(int ggpo, int time);

    [DllImport(libraryName)]
    public static extern int ggpo_add_player(int ggpo,

            int player_size,
    int player_type,
    int player_num,
    string player_ip_address,
    short player_port,
    out int handle);

    [DllImport(libraryName)]
    public static extern int ggpo_disconnect_player(int ggpo, int handle);

    [DllImport(libraryName)]
    public static extern void ggpo_set_frame_delay(int ggpo, int handle, int frame_delay);

    [DllImport(libraryName)]
    public static extern void ggpo_advance_frame(int ggpo);

    [DllImport(libraryName)]
    public static extern void ggpo_log(int ggpo, string v);

    public static bool GGPO_SUCCEEDED(int result) {
        return result == GGPO_OK;
    }

    [DllImport(libraryName)]
    public static extern void ggpo_get_network_stats(int ggpo, int p,
        out int send_queue_len,
        out int recv_queue_len,
        out int ping,
        out int kbps_sent,
        out int local_frames_behind,
        out int remote_frames_behind
    );
}
