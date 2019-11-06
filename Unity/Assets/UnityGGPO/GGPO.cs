using System;
using System.Runtime.InteropServices;
using UnityEngine;

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
    public ushort port;
}

public class GGPONetworkStats {
    public int send_queue_len;
    public int recv_queue_len;
    public int ping;
    public int kbps_sent;
    public int local_frames_behind;
    public int remote_frames_behind;
}

public static class GGPO {

    public static bool SUCCEEDED(int result) {
        return result == ERRORCODE_SUCCESS;
    }

    public static string GetErrorCodeMessage(int result) {
        switch (result) {
            case ERRORCODE_SUCCESS:
                return "ERRORCODE_SUCCESS";

            case ERRORCODE_GENERAL_FAILURE:
                return "ERRORCODE_GENERAL_FAILURE";

            case ERRORCODE_INVALID_SESSION:
                return "ERRORCODE_INVALID_SESSION";

            case ERRORCODE_INVALID_PLAYER_HANDLE:
                return "ERRORCODE_INVALID_PLAYER_HANDLE";

            case ERRORCODE_PLAYER_OUT_OF_RANGE:
                return "ERRORCODE_PLAYER_OUT_OF_RANGE";

            case ERRORCODE_PREDICTION_THRESHOLD:
                return "ERRORCODE_PREDICTION_THRESHOLD";

            case ERRORCODE_UNSUPPORTED:
                return "ERRORCODE_UNSUPPORTED";

            case ERRORCODE_NOT_SYNCHRONIZED:
                return "ERRORCODE_NOT_SYNCHRONIZED";

            case ERRORCODE_IN_ROLLBACK:
                return "ERRORCODE_IN_ROLLBACK";

            case ERRORCODE_INPUT_DROPPED:
                return "ERRORCODE_INPUT_DROPPED";

            case ERRORCODE_PLAYER_DISCONNECTED:
                return "ERRORCODE_PLAYER_DISCONNECTED";

            case ERRORCODE_TOO_MANY_SPECTATORS:
                return "ERRORCODE_TOO_MANY_SPECTATORS";

            case ERRORCODE_INVALID_REQUEST:
                return "ERRORCODE_INVALID_REQUEST";
        }
        return "INVALID_ERRORCODE";
    }

    public static void ReportFailure(int result) {
        if (result != ERRORCODE_SUCCESS) {
            Debug.LogWarning(GetErrorCodeMessage(result));
        }
    }

    public const int MAX_PLAYERS = 4;
    public const int MAX_PREDICTION_FRAMES = 8;
    public const int MAX_SPECTATORS = 32;

    public const int OK = 0;
    public const int INVALID_HANDLE = -1;

    public const int ERRORCODE_SUCCESS = 0;
    public const int ERRORCODE_GENERAL_FAILURE = -1;
    public const int ERRORCODE_INVALID_SESSION = 1;
    public const int ERRORCODE_INVALID_PLAYER_HANDLE = 2;
    public const int ERRORCODE_PLAYER_OUT_OF_RANGE = 3;
    public const int ERRORCODE_PREDICTION_THRESHOLD = 4;
    public const int ERRORCODE_UNSUPPORTED = 5;
    public const int ERRORCODE_NOT_SYNCHRONIZED = 6;
    public const int ERRORCODE_IN_ROLLBACK = 7;
    public const int ERRORCODE_INPUT_DROPPED = 8;
    public const int ERRORCODE_PLAYER_DISCONNECTED = 9;
    public const int ERRORCODE_TOO_MANY_SPECTATORS = 10;
    public const int ERRORCODE_INVALID_REQUEST = 11;

    public const int EVENTCODE_CONNECTED_TO_PEER = 1000;
    public const int EVENTCODE_SYNCHRONIZING_WITH_PEER = 1001;
    public const int EVENTCODE_SYNCHRONIZED_WITH_PEER = 1002;
    public const int EVENTCODE_RUNNING = 1003;
    public const int EVENTCODE_DISCONNECTED_FROM_PEER = 1004;
    public const int EVENTCODE_TIMESYNC = 1005;
    public const int EVENTCODE_CONNECTION_INTERRUPTED = 1006;
    public const int EVENTCODE_CONNECTION_RESUMED = 1007;

    public static string Version {
        get {
            return Helper.GetString(UggPluginVersion());
        }
    }

    public static int BuildNumber {
        get {
            return UggPluginBuildNumber();
        }
    }

    const string libraryName = "UnityGGPO";

    public delegate void LogDelegate(string text);

    public delegate bool BeginGameDelegate(string text);

    public delegate bool AdvanceFrameDelegate(int flags);

    unsafe public delegate bool LoadGameStateDelegate(void* buffer, int length);

    unsafe public delegate bool LogGameStateDelegate(string text, void* buffer, int length);

    unsafe public delegate bool SaveGameStateDelegate(void** buffer, int* len, int* checksum, int frame);

    unsafe public delegate void FreeBufferDelegate(void* buffer);

    public delegate bool OnEventDelegate(IntPtr evt);

    [DllImport(libraryName, CharSet = CharSet.Ansi)]
    static extern IntPtr UggPluginVersion();

    [DllImport(libraryName)]
    static extern int UggPluginBuildNumber();

    [DllImport(libraryName)]
    public static extern void UggSetLogDelegate(LogDelegate callback);

    [DllImport(libraryName)]
    public static extern int UggTestStartSession(out IntPtr session,
        BeginGameDelegate beginGame,
        AdvanceFrameDelegate advanceFrame,
        LoadGameStateDelegate loadGameState,
        LogGameStateDelegate logGameState,
        SaveGameStateDelegate saveGameState,
        FreeBufferDelegate freeBuffer,
        OnEventDelegate onEvent,
        string game, int num_players, int localport);

    [DllImport(libraryName)]
    public static extern IntPtr UggStartSession(
        BeginGameDelegate beginGame,
        AdvanceFrameDelegate advanceFrame,
        LoadGameStateDelegate loadGameState,
        LogGameStateDelegate logGameState,
        SaveGameStateDelegate saveGameState,
        FreeBufferDelegate freeBuffer,
        OnEventDelegate onEvent,
        string game, int num_players, int localport);

    [DllImport(libraryName)]
    public static extern IntPtr UggStartSpectating(
        BeginGameDelegate beginGame,
        AdvanceFrameDelegate advanceFrame,
        LoadGameStateDelegate loadGameState,
        LogGameStateDelegate logGameState,
        SaveGameStateDelegate saveGameState,
        FreeBufferDelegate freeBuffer,
        OnEventDelegate onEvent,
        string game, int num_players, int localport, string host_ip, int host_port);

    [DllImport(libraryName)]
    public static extern int UggSetDisconnectNotifyStart(IntPtr ggpo, int timeout);

    [DllImport(libraryName)]
    public static extern int UggSetDisconnectTimeout(IntPtr ggpo, int timeout);

    [DllImport(libraryName)]
    public static extern int UggSynchronizeInput(IntPtr ggpo, ulong[] inputs, int length, out int disconnect_flags);

    [DllImport(libraryName)]
    public static extern int UggAddLocalInput(IntPtr ggpo, int local_player_handle, ulong input);

    [DllImport(libraryName)]
    public static extern int UggCloseSession(IntPtr ggpo);

    [DllImport(libraryName)]
    public static extern int UggIdle(IntPtr ggpo, int timeout);

    [DllImport(libraryName)]
    public static extern int UggAddPlayer(IntPtr ggpo, int player_type, int player_num, string player_ip_address, ushort player_port, out int phandle);

    [DllImport(libraryName)]
    public static extern int UggDisconnectPlayer(IntPtr ggpo, int phandle);

    [DllImport(libraryName)]
    public static extern int UggSetFrameDelay(IntPtr ggpo, int phandle, int frame_delay);

    [DllImport(libraryName)]
    public static extern int UggAdvanceFrame(IntPtr ggpo);

    [DllImport(libraryName)]
    public static extern void UggLog(IntPtr ggpo, string text);

    [DllImport(libraryName)]
    public static extern int UggGetNetworkStats(IntPtr ggpo, int phandle,
        out int send_queue_len,
        out int recv_queue_len,
        out int ping,
        out int kbps_sent,
        out int local_frames_behind,
        out int remote_frames_behind);
}
