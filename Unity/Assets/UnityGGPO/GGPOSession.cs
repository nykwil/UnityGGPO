using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;

public class UGGPO {
    const string libraryName = "GGPOPlugin";

    public delegate void LogDelegate(string text);

    public delegate bool BeginGameDelegate(string text);

    public delegate bool AdvanceFrameDelegate(int flags);

    unsafe public delegate bool LoadGameStateDelegate(void* buffer, int length);

    unsafe public delegate bool LogGameStateDelegate(string text, void* buffer, int length);

    unsafe public delegate bool SaveGameStateDelegate(void** buffer, int* len, int* checksum, int frame);

    unsafe public delegate void FreeBufferDelegate(void* buffer);

    public delegate bool OnEventDelegate(IntPtr evt);

    [DllImport(libraryName, CharSet = CharSet.Ansi)]
    public static extern IntPtr UggPluginVersion();

    [DllImport(libraryName)]
    public static extern int UggPluginBuildNumber();

    [DllImport(libraryName)]
    public static extern void UggSetLogDelegate(LogDelegate callback);

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
    public static extern int UggAddPlayer(IntPtr ggpo, int player_type, int player_num, string player_ip_address, short player_port, out int phandle);

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

public class GGPOSession {
    // Pass throughs

    public delegate bool OnEventConnectedToPeerDelegate(int connected_player);

    public delegate bool OnEventSynchronizingWithPeerDelegate(int synchronizing_player, int synchronizing_count, int synchronizing_total);

    public delegate bool OnEventSynchronizedWithPeerDelegate(int synchronizing_player);

    public delegate bool OnEventRunningDelegate();

    public delegate bool OnEventConnectionInterruptedDelegate(int connection_interrupted_player, int connection_interrupted_disconnect_timeout);

    public delegate bool OnEventConnectionResumedDelegate(int connection_resumed_player);

    public delegate bool OnEventDisconnectedFromPeerDelegate(int disconnected_player);

    public delegate bool OnEventEventcodeTimesyncDelegate(int timesync_frames_ahead);

    public delegate bool SafeLoadGameStateDelegate(NativeArray<byte> data);

    public delegate bool SafeLogGameStateDelegate(string text, NativeArray<byte> data);

    public delegate NativeArray<byte> SafeSaveGameStateDelegate(out int checksum, int frame);

    public delegate void SafeFreeBufferDelegate(NativeArray<byte> data);

    IntPtr ggpo;
    public Action<string> logDelegate;
    readonly Dictionary<long, NativeArray<byte>> cache = new Dictionary<long, NativeArray<byte>>();

    SafeLoadGameStateDelegate loadGameStateCallback;
    SafeLogGameStateDelegate logGameStateCallback;
    SafeSaveGameStateDelegate saveGameStateCallback;
    SafeFreeBufferDelegate freeBufferCallback;

    OnEventConnectedToPeerDelegate onEventConnectedToPeer;
    OnEventSynchronizingWithPeerDelegate onEventSynchronizingWithPeer;
    OnEventSynchronizedWithPeerDelegate onEventSynchronizedWithPeer;
    OnEventRunningDelegate onEventRunning;
    OnEventConnectionInterruptedDelegate onEventConnectionInterrupted;
    OnEventConnectionResumedDelegate onEventConnectionResumed;
    OnEventDisconnectedFromPeerDelegate onEventDisconnectedFromPeer;
    OnEventEventcodeTimesyncDelegate onEventTimesync;

    UGGPO.LoadGameStateDelegate _loadGameStateCallback;
    UGGPO.LogGameStateDelegate _logGameStateCallback;
    UGGPO.SaveGameStateDelegate _saveGameStateCallback;
    UGGPO.FreeBufferDelegate _freeBufferCallback;
    UGGPO.OnEventDelegate _onEventCallback;

    public void StartSession(
            UGGPO.BeginGameDelegate beginGame,
            UGGPO.AdvanceFrameDelegate advanceFrame,
            SafeLoadGameStateDelegate loadGameState,
            SafeLogGameStateDelegate logGameState,
            SafeSaveGameStateDelegate saveGameState,
            SafeFreeBufferDelegate freeBuffer,
            OnEventConnectedToPeerDelegate onEventConnectedToPeer,
            OnEventSynchronizingWithPeerDelegate onEventSynchronizingWithPeer,
            OnEventSynchronizedWithPeerDelegate onEventSynchronizedWithPeer,
            OnEventRunningDelegate onEventRunning,
            OnEventConnectionInterruptedDelegate onEventConnectionInterrupted,
            OnEventConnectionResumedDelegate onEventConnectionResumed,
            OnEventDisconnectedFromPeerDelegate onEventDisconnectedFromPeer,
            OnEventEventcodeTimesyncDelegate onEventTimesync,
            string gameName, int numPlayers, int localport) {
        loadGameStateCallback = loadGameState;
        logGameStateCallback = logGameState;
        saveGameStateCallback = saveGameState;
        freeBufferCallback = freeBuffer;

        this.onEventConnectedToPeer = onEventConnectedToPeer;
        this.onEventSynchronizingWithPeer = onEventSynchronizingWithPeer;
        this.onEventSynchronizedWithPeer = onEventSynchronizedWithPeer;
        this.onEventRunning = onEventRunning;
        this.onEventConnectionInterrupted = onEventConnectionInterrupted;
        this.onEventConnectionResumed = onEventConnectionResumed;
        this.onEventDisconnectedFromPeer = onEventDisconnectedFromPeer;
        this.onEventTimesync = onEventTimesync;

        unsafe {
            _loadGameStateCallback = LoadGameState;
            _logGameStateCallback = LogGameState;
            _saveGameStateCallback = SaveGameState;
            _freeBufferCallback = FreeBuffer;
            _onEventCallback = OnEventCallback;
        }
        ggpo = UGGPO.UggStartSession(beginGame,
            advanceFrame,
            _loadGameStateCallback,
            _logGameStateCallback,
            _saveGameStateCallback,
            _freeBufferCallback,
            _onEventCallback,
            gameName, numPlayers, localport);
    }

    bool OnEventCallback(IntPtr evtPtr) {
        /*
connected.player = data[1];
synchronizing.player = data[1];
synchronizing.count = data[2];
synchronizing.total = data[3];
synchronized.player = data[1];
disconnected.player = data[1]
timesync.frames_ahead = data[1];
connection_interrupted.player = data[1];
connection_interrupted.disconnect_timeout = data[2];
connection_resumed.player = data[1];
*/

        int[] data = new int[4];
        Marshal.Copy(evtPtr, data, 0, 4);
        switch (data[0]) {
            case GGPOC.GGPO_EVENTCODE_CONNECTED_TO_PEER:
                return onEventConnectedToPeer(data[1]);

            case GGPOC.GGPO_EVENTCODE_SYNCHRONIZING_WITH_PEER:
                return onEventSynchronizingWithPeer(data[1], data[2], data[3]);

            case GGPOC.GGPO_EVENTCODE_SYNCHRONIZED_WITH_PEER:
                return onEventSynchronizedWithPeer(data[1]);

            case GGPOC.GGPO_EVENTCODE_RUNNING:
                return onEventRunning();

            case GGPOC.GGPO_EVENTCODE_DISCONNECTED_FROM_PEER:
                return onEventDisconnectedFromPeer(data[1]);

            case GGPOC.GGPO_EVENTCODE_TIMESYNC:
                return onEventTimesync(data[1]);

            case GGPOC.GGPO_EVENTCODE_CONNECTION_INTERRUPTED:
                return onEventConnectionInterrupted(data[1], data[2]);

            case GGPOC.GGPO_EVENTCODE_CONNECTION_RESUMED:
                return onEventConnectionResumed(data[1]);
        }
        return false;
    }

    public void GetNetworkStats(int p, out GGPONetworkStats stats) {
        stats = new GGPONetworkStats();
        UGGPO.UggGetNetworkStats(ggpo, p,
            out stats.send_queue_len,
            out stats.recv_queue_len,
            out stats.ping,
            out stats.kbps_sent,
            out stats.local_frames_behind,
            out stats.remote_frames_behind
        );
    }

    public void SetDisconnectNotifyStart(int timeout) {
        UGGPO.UggSetDisconnectNotifyStart(ggpo, timeout);
    }

    public void SetDisconnectTimeout(int timeout) {
        UGGPO.UggSetDisconnectTimeout(ggpo, timeout);
    }

    public int SynchronizeInput(ulong[] inputs, int length, out int disconnect_flags) {
        return UGGPO.UggSynchronizeInput(ggpo, inputs, length, out disconnect_flags);
    }

    public int AddLocalInput(int local_player_handle, ulong inputs) {
        return UGGPO.UggAddLocalInput(ggpo, local_player_handle, inputs);
    }

    public void CloseSession() {
        UGGPO.UggCloseSession(ggpo);
    }

    public void Idle(int time) {
        UGGPO.UggIdle(ggpo, time);
    }

    public int AddPlayer(GGPOPlayer player, out int phandle) {
        return UGGPO.UggAddPlayer(ggpo,
            (int)player.type,
            player.player_num,
            player.ip_address,
            player.port,
            out phandle);
    }

    public int DisconnectPlayer(int phandle) {
        return UGGPO.UggDisconnectPlayer(ggpo, phandle);
    }

    public void SetFrameDelay(int phandle, int frame_delay) {
        UGGPO.UggSetFrameDelay(ggpo, phandle, frame_delay);
    }

    public void AdvanceFrame() {
        UGGPO.UggAdvanceFrame(ggpo);
    }

    public void Log(string v) {
        UGGPO.UggLog(ggpo, v);
    }

    unsafe void FreeBuffer(void* dataPtr) {
        if (cache.TryGetValue((long)dataPtr, out var data)) {
            freeBufferCallback(data);
        }
    }

    unsafe bool SaveGameState(void** buffer, int* outLen, int* outChecksum, int frame) {
        var data = saveGameStateCallback(out int checksum, frame);
        var ptr = GGPO.ToPtr(data);
        cache[(long)ptr] = data;

        *buffer = ptr;
        *outLen = data.Length;
        *outChecksum = checksum;
        return true;
    }

    unsafe bool LogGameState(string text, void* buffer, int length) {
        return logGameStateCallback(text, GGPO.ToArray(buffer, length));
    }

    unsafe public bool LoadGameState(void* buffer, int length) {
        return loadGameStateCallback(GGPO.ToArray(buffer, length));
    }
}
