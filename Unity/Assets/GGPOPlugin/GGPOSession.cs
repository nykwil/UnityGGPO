using System;
using System.Collections.Generic;
using Unity.Collections;

public class GGPOSession {

    public delegate bool SafeLoadGameStateDelegate(NativeArray<byte> data);

    public delegate bool SafeLogGameStateDelegate(NativeArray<byte> data);

    public delegate NativeArray<byte> SafeSaveGameStateDelegate(out int checksum, int frame);

    public delegate void SafeFreeBufferDelegate(NativeArray<byte> data);

    int handle;
    Dictionary<long, NativeArray<byte>> cache = new Dictionary<long, NativeArray<byte>>();

    SafeLoadGameStateDelegate loadGameStateCallback;
    SafeLogGameStateDelegate logGameStateCallback;
    SafeSaveGameStateDelegate saveGameStateCallback;
    SafeFreeBufferDelegate freeBufferCallback;

    public Action<string> logDelegate;

    GGPO.LoadGameStateDelegate _loadGameStateCallback;
    GGPO.LogGameStateDelegate _logGameStateCallback;
    GGPO.SaveGameStateDelegate _saveGameStateCallback;
    GGPO.FreeBufferDelegate _freeBufferCallback;

    public void StartSession(
            GGPO.BeginGameDelegate beginGame,
            GGPO.AdvanceFrameDelegate advanceFrame,
            SafeLoadGameStateDelegate loadGameState,
            SafeLogGameStateDelegate logGameState,
            SafeSaveGameStateDelegate saveGameState,
            SafeFreeBufferDelegate freeBuffer,
            GGPO.OnEventConnectedToPeerDelegate onEventConnectedToPeer,
            GGPO.OnEventSynchronizingWithPeerDelegate onEventSynchronizingWithPeer,
            GGPO.OnEventSynchronizedWithPeerDelegate onEventSynchronizedWithPeer,
            GGPO.OnEventRunningDelegate onEventRunning,
            GGPO.OnEventConnectionInterruptedDelegate onEventConnectionInterrupted,
            GGPO.OnEventConnectionResumedDelegate onEventConnectionResumedDelegate,
            GGPO.OnEventDisconnectedFromPeerDelegate onEventDisconnectedFromPeerDelegate,
            GGPO.OnEventEventcodeTimesyncDelegate onEventTimesyncDelegate,
            string gameName, int numPlayers, int inputSize, int localport
        ) {
        loadGameStateCallback = loadGameState;
        logGameStateCallback = logGameState;
        saveGameStateCallback = saveGameState;
        freeBufferCallback = freeBuffer;

        unsafe {
            _loadGameStateCallback = LoadGameState;
            _logGameStateCallback = LogGameState;
            _saveGameStateCallback = SaveGameState;
            _freeBufferCallback = FreeBuffer;
        }
        handle = GGPO.DllStartSession(beginGame,
            advanceFrame,
            _loadGameStateCallback,
            _logGameStateCallback,
            _saveGameStateCallback,
            _freeBufferCallback,
            onEventConnectedToPeer,
            onEventSynchronizingWithPeer,
            onEventSynchronizedWithPeer,
            onEventRunning,
            onEventConnectionInterrupted,
            onEventConnectionResumedDelegate,
            onEventDisconnectedFromPeerDelegate,
            onEventTimesyncDelegate,
            gameName, numPlayers, inputSize, localport);
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

    unsafe void FreeBuffer(void* dataPtr, int length) {
        if (cache.TryGetValue((long)dataPtr, out var data)) {
            freeBufferCallback(data);
        }
    }

    unsafe void* SaveGameState(out int length, out int checksum, int frame) {
        var data = saveGameStateCallback(out checksum, frame);
        var ptr = GGPO.ToPtr(data);
        cache[(long)ptr] = data;
        length = data.Length;
        return ptr;
    }

    unsafe bool LogGameState(string text, void* buffer, int length) {
        return logGameStateCallback(GGPO.ToArray(buffer, length));
    }

    unsafe public bool LoadGameState(void* buffer, int length) {
        return loadGameStateCallback(GGPO.ToArray(buffer, length));
    }
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
