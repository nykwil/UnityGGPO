using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public class BitAccess {

    public struct Section {
        public int pos;
        public int length;
        public ulong mask;

        public Section(int pos, int length) {
            this.pos = pos;
            this.length = length;
            this.mask = GetMask(pos, length);
        }
    }

    public static Section GetSection(ulong maxValue) {
        return new Section(GetSize(maxValue), 0);
    }

    public static Section GetSection(ulong maxValue, Section prev) {
        return new Section(GetSize(maxValue), prev.pos + prev.length);
    }

    static public int GetSize(ulong value) {
        var i = 1;
        while ((1ul << i) <= value) {
            ++i;
        }
        return i;
    }

    static ulong GetMask(int start, int length) {
        return ((1ul << length) - 1) << start;
    }

    public ulong Get(int start, int length) {
        var mask = GetMask(start, length);
        var d = data & mask;
        return d >> start;
    }

    public ulong Get(Section s) {
        return Get(s.pos, s.length);
    }

    public void Set(int start, int length, int value) {
        var nmask = ~GetMask(start, length);
        data = (data & nmask) | ((ulong)value << start);
    }

    public void Set(Section s, int value) {
        Set(value, s.pos, s.length);
    }

    public ulong data;
}

public enum GGPOPlayerType {
    GGPO_PLAYERTYPE_LOCAL,
    GGPO_PLAYERTYPE_REMOTE,
    GGPO_PLAYERTYPE_SPECTATOR,
}

public class GGPOSession {
    public int handle;

    public GGPO.SafeLoadGameStateDelegate loadGameStateCallback;
    public GGPO.SafeLogGameStateDelegate logGameStateCallback;
    public GGPO.SafeSaveGameStateDelegate saveGameStateCallback;
    public Action<string> logDelegate;

    GGPO.LoadGameStateDelegate _loadGameStateCallback;
    GGPO.LogGameStateDelegate _logGameStateCallback;
    GGPO.SaveGameStateDelegate _saveGameStateCallback;
    GGPO.FreeBufferDelegate _freeBufferCallback;

    public void StartSession(
            GGPO.BeginGameDelegate beginGame,
            GGPO.AdvanceFrameDelegate advanceFrame,
            GGPO.SafeLoadGameStateDelegate loadGameState,
            GGPO.SafeLogGameStateDelegate logGameState,
            GGPO.SafeSaveGameStateDelegate saveGameState,
            GGPO.OnEventConnectedToPeerDelegate onEventConnectedToPeer,
            GGPO.OnEventSynchronizingWithPeerDelegate onEventSynchronizingWithPeer,
            GGPO.OnEventSynchronizedWithPeerDelegate onEventSynchronizedWithPeer,
            GGPO.OnEventRunningDelegate on_event_running,
            GGPO.OnEventConnectionInterruptedDelegate on_event_connection_interrupted,
            GGPO.OnEventConnectionResumedDelegate onEventConnectionResumedDelegate,
            GGPO.OnEventDisconnectedFromPeerDelegate onEventDisconnectedFromPeerDelegate,
            GGPO.OnEventEventcodeTimesyncDelegate onEventTimesyncDelegate,
            string game, int num_players, int input_size, int localport
        ) {
        loadGameStateCallback = loadGameState;
        logGameStateCallback = logGameState;
        saveGameStateCallback = saveGameState;

        unsafe {
            _loadGameStateCallback = LoadGameState;
            _logGameStateCallback = LogGameState;
            _saveGameStateCallback = SaveGameState;
            _freeBufferCallback = FreeBuffer;
        }
        GGPO.DllStartSession(beginGame,
            advanceFrame,
            _loadGameStateCallback,
            _logGameStateCallback,
            _saveGameStateCallback,
            _freeBufferCallback,
            onEventConnectedToPeer,
            onEventSynchronizingWithPeer,
            onEventSynchronizedWithPeer,
            on_event_running,
            on_event_connection_interrupted,
            onEventConnectionResumedDelegate,
            onEventDisconnectedFromPeerDelegate,
            onEventTimesyncDelegate,
            game, num_players, input_size, localport);
    }

    public void GetNetworkStats(int p, out GGPONetworkStats stats) {
        stats = new GGPONetworkStats();
        GGPO.DllGetNetworkStats(handle, p,
            out stats.send_queue_len,
            out stats.recv_queue_len,
            out stats.ping,
            out stats.kbps_sent,
            out stats.local_frames_behind,
            out stats.remote_frames_behind
        );
    }

    public void SetDisconnectNotifyStart(int timeout) {
        GGPO.DllSetDisconnectNotifyStart(handle, timeout);
    }

    public void SetDisconnectTimeout(int timeout) {
        GGPO.DllSetDisconnectTimeout(handle, timeout);
    }

    public int SynchronizeInput(ulong[] inputs, out int disconnect_flags) {
        return GGPO.DllSynchronizeInput(handle, inputs, out disconnect_flags);
    }

    public int AddLocalInput(int local_player_handle, ulong inputs) {
        return GGPO.DllAddLocalInput(handle, local_player_handle, inputs);
    }

    public void ggpo_close_session() {
        GGPO.DllCloseSession(handle);
    }

    public void ggpo_idle(int time) {
        GGPO.DllIdle(handle, time);
    }

    public int ggpo_add_player(int ggpo, GGPOPlayer player, out int handle) {
        return GGPO.DllAddPlayer(this.handle,
                player.size,
        (int)player.type,
    player.player_num,
        player.ip_address,
        player.port,
out handle);
    }

    public int DisconnectPlayer(int ggpo, int phandle) {
        return GGPO.DllDisconnectPlayer(this.handle, phandle);
    }

    public void ggpo_set_frame_delay(int ggpo, int phandle, int frame_delay) {
        GGPO.DllSetFrameDelay(this.handle, handle, frame_delay);
    }

    public void AdvanceFrame(int ggpo) {
        GGPO.DllAdvanceFrame(handle);
    }

    public void Log(int ggpo, string v) {
        GGPO.DllLog(handle, v);
    }

    private unsafe void FreeBuffer(void* buffer, int length) {
        GGPO.ToArray(buffer, length).Dispose();
    }

    private unsafe void* SaveGameState(out int length, out int checksum, int frame) {
        var data = saveGameStateCallback(out checksum, frame);
        length = data.Length;
        return GGPO.ToPtr(data);
    }

    private unsafe bool LogGameState(void* buffer, int length) {
        return logGameStateCallback(GGPO.ToArray(buffer, length));
    }

    unsafe public bool LoadGameState(void* buffer, int length) {
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

public class GGPO {
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

    private const string libraryName = "GGPOPlugin";

    private static string version;

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
                var ptr = GetPluginVersion();
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

    public static bool GGPO_SUCCEEDED(int result) {
        return result == GGPO_OK;
    }

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

    [DllImport(libraryName)]
    public static extern int DllStartSession(
            BeginGameDelegate begin_game,
            AdvanceFrameDelegate advance_frame,
            LoadGameStateDelegate load_game_state,
            LogGameStateDelegate log_game_state,
            SaveGameStateDelegate save_game_state,
            FreeBufferDelegate free_buffer,
            OnEventConnectedToPeerDelegate on_even_connected_to_peer,
            OnEventSynchronizingWithPeerDelegate on_event_synchronizing_with_peer,
            OnEventSynchronizedWithPeerDelegate on_event_synchronized_with_peer,
            OnEventRunningDelegate on_event_running,
            OnEventConnectionInterruptedDelegate on_event_connection_interrupted,
            OnEventConnectionResumedDelegate onEventConnectionResumedDelegate,
            OnEventDisconnectedFromPeerDelegate onEventDisconnectedFromPeerDelegate,
            OnEventEventcodeTimesyncDelegate onEventTimesyncDelegate,
            string game, int num_players, int input_size, int localport);

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
            string game, int num_players, int input_size, int localport, string host_ip, int host_port);

    [DllImport(libraryName)]
    public static extern int DllSetDisconnectNotifyStart(int ggpo, int timeout);

    [DllImport(libraryName)]
    public static extern int DllSetDisconnectTimeout(int ggpo, int timeout);

    [DllImport(libraryName)]
    public static extern int DllSynchronizeInput(int ggpo, ulong[] inputs, out int disconnect_flags);

    [DllImport(libraryName)]
    public static extern int DllAddLocalInput(int ggpo, int local_player_handle, ulong inputs);

    [DllImport(libraryName)]
    public static extern int DllCloseSession(int ggpo);

    [DllImport(libraryName)]
    public static extern int DllIdle(int ggpo, int time);

    [DllImport(libraryName)]
    public static extern int DllAddPlayer(int ggpo,
            int player_size,
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

    [DllImport(libraryName)]
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


/*extern "C" const char UNITY_INTERFACE_EXPORT * UNITY_INTERFACE_API GetPluginVersion();
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetPluginBuildNumber();
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetLogDelegate(LogDelegate callback);
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllTestLogGameStateDelegate(LogGameStateDelegate callback);
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllTestFreeGameStateDelegate(LogGameStateDelegate callback);
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllTestSaveGameStateDelegate(SaveGameStateDelegate callback);
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllStartSession(
    BeginGameDelegate beginGame,
    AdvanceFrameDelegate advanceFrame,
    LoadGameStateDelegate loadGameState,
    LogGameStateDelegate logGameState,
    SaveGameStateDelegate saveGameState,
    FreeBufferDelegate freeBuffer,
    OnEventConnectedToPeerDelegate onEventConnectedToPeer,
    OnEventSynchronizingWithPeerDelegate on_event_synchronizing_with_peer,
    OnEventSynchronizedWithPeerDelegate on_event_synchronized_with_peer,
    OnEventRunningDelegate on_event_running,
    OnEventConnectionInterruptedDelegate on_event_connection_interrupted,
    OnEventConnectionResumedDelegate onEventConnectionResumedDelegate,
    OnEventDisconnectedFromPeerDelegate onEventDisconnectedFromPeerDelegate,
    OnEventTimesyncDelegate onEventEventcodeTimesyncDelegate,
	const char* game, int num_players, int input_size, int localport);
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllStartSpectating(BeginGameDelegate begin_game,
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
    OnEventTimesyncDelegate onEventEventcodeTimesyncDelegate,
	const char* game, int num_players, int input_size, int localport, const char* host_ip, int host_port);
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllSetDisconnectNotifyStart(int ggpo, int timeout) {
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllSetDisconnectTimeout(int ggpo, int timeout) {
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllSynchronizeInput(int ggpo, unsigned long long* inputs, int length, int& disconnect_flags) {
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllAddLocalInput(int ggpo, int local_player_handle, unsigned long long input, int length) {
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllCloseSession(int ggpo) {
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllIdle(int ggpo, int timeout) {
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllAddPlayer(int ggpo,
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllDisconnectPlayer(int ggpo, int phandle) {
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllSetFrameDelay(int ggpo, int phandle, int frame_delay) {
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllAdvanceFrame(int ggpo) {
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllLog(int ggpo, const char* v) {
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllGetNetworkStats(int ggpo, int phandle,
*/