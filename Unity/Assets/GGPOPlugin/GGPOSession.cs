using System;
using System.Collections.Generic;
using Unity.Collections;

public class GGPOSession {

    public delegate bool SafeLoadGameStateDelegate(NativeArray<byte> data);

    public delegate bool SafeLogGameStateDelegate(NativeArray<byte> data);

    public delegate NativeArray<byte> SafeSaveGameStateDelegate(out int checksum, int frame);

    public delegate void SafeFreeBufferDelegate(NativeArray<byte> data);

    int ghandle;
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
        ghandle = GGPO.DllStartSession(beginGame,
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
        GGPO.DllGetNetworkStats(ghandle, p,
            out stats.send_queue_len,
            out stats.recv_queue_len,
            out stats.ping,
            out stats.kbps_sent,
            out stats.local_frames_behind,
            out stats.remote_frames_behind
        );
    }

    public void SetDisconnectNotifyStart(int timeout) {
        GGPO.DllSetDisconnectNotifyStart(ghandle, timeout);
    }

    public void SetDisconnectTimeout(int timeout) {
        GGPO.DllSetDisconnectTimeout(ghandle, timeout);
    }

    public int SynchronizeInput(ulong[] inputs, int length, out int disconnect_flags) {
        return GGPO.DllSynchronizeInput(ghandle, inputs, length, out disconnect_flags);
    }

    public int AddLocalInput(int local_player_handle, ulong inputs) {
        return GGPO.DllAddLocalInput(ghandle, local_player_handle, inputs);
    }

    public void CloseSession() {
        GGPO.DllCloseSession(ghandle);
    }

    public void Idle(int time) {
        GGPO.DllIdle(ghandle, time);
    }

    public int AddPlayer(int ggpo, GGPOPlayer player, out int phandle) {
        return GGPO.DllAddPlayer(ghandle,
            player.size,
            (int)player.type,
            player.player_num,
            player.ip_address,
            player.port,
            out phandle);
    }

    public int DisconnectPlayer(int ggpo, int phandle) {
        return GGPO.DllDisconnectPlayer(ghandle, phandle);
    }

    public void SetFrameDelay(int ggpo, int phandle, int frame_delay) {
        GGPO.DllSetFrameDelay(ghandle, phandle, frame_delay);
    }

    public void AdvanceFrame(int ggpo) {
        GGPO.DllAdvanceFrame(ghandle);
    }

    public void Log(int ggpo, string v) {
        GGPO.DllLog(ghandle, v);
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
