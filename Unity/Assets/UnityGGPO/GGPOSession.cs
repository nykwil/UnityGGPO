using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

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

    GGPO.LoadGameStateDelegate _loadGameStateCallback;
    GGPO.LogGameStateDelegate _logGameStateCallback;
    GGPO.SaveGameStateDelegate _saveGameStateCallback;
    GGPO.FreeBufferDelegate _freeBufferCallback;
    GGPO.OnEventDelegate _onEventCallback;

    public void StartSession(
            GGPO.BeginGameDelegate beginGame,
            GGPO.AdvanceFrameDelegate advanceFrame,
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
            ggpo = GGPO.UggStartSession(beginGame,
                advanceFrame,
                _loadGameStateCallback,
                _logGameStateCallback,
                _saveGameStateCallback,
                _freeBufferCallback,
                _onEventCallback,
                gameName, numPlayers, localport);

            Debug.Assert(ggpo != IntPtr.Zero);
        }
    }

    public void StartSpectating(
            GGPO.BeginGameDelegate beginGame,
            GGPO.AdvanceFrameDelegate advanceFrame,
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
            string gameName, int numPlayers, int localport, string hostIp, int hostPort) {
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
        ggpo = GGPO.UggStartSpectating(beginGame,
            advanceFrame,
            _loadGameStateCallback,
            _logGameStateCallback,
            _saveGameStateCallback,
            _freeBufferCallback,
            _onEventCallback,
            gameName, numPlayers, localport, hostIp, hostPort);
    }

    public int GetNetworkStats(int phandle, out GGPONetworkStats stats) {
        stats = new GGPONetworkStats();
        var result = GGPO.UggGetNetworkStats(ggpo, phandle,
            out stats.send_queue_len,
            out stats.recv_queue_len,
            out stats.ping,
            out stats.kbps_sent,
            out stats.local_frames_behind,
            out stats.remote_frames_behind
        );
        Debug.Assert(GGPO.SUCCEEDED(result));
        return result;
    }

    public int SetDisconnectNotifyStart(int timeout) {
        var result = GGPO.UggSetDisconnectNotifyStart(ggpo, timeout);
        Debug.Assert(GGPO.SUCCEEDED(result));
        return result;
    }

    public int SetDisconnectTimeout(int timeout) {
        var result = GGPO.UggSetDisconnectTimeout(ggpo, timeout);
        Debug.Assert(GGPO.SUCCEEDED(result));
        return result;
    }

    public int SynchronizeInput(ulong[] inputs, int length, out int disconnect_flags) {
        var result = GGPO.UggSynchronizeInput(ggpo, inputs, length, out disconnect_flags);
        Debug.Assert(GGPO.SUCCEEDED(result));
        return result;
    }

    public int AddLocalInput(int local_player_handle, ulong inputs) {
        var result = GGPO.UggAddLocalInput(ggpo, local_player_handle, inputs);
        Debug.Assert(GGPO.SUCCEEDED(result));
        return result;
    }

    public int CloseSession() {
        var result = GGPO.UggCloseSession(ggpo);
        Debug.Assert(GGPO.SUCCEEDED(result));
        return result;
    }

    public int Idle(int time) {
        var result = GGPO.UggIdle(ggpo, time);
        Debug.Assert(GGPO.SUCCEEDED(result));
        return result;
    }

    public int AddPlayer(GGPOPlayer player, out int phandle) {
        var result = GGPO.UggAddPlayer(ggpo,
            (int)player.type,
            player.player_num,
            player.ip_address,
            player.port,
            out phandle);
        Debug.Assert(GGPO.SUCCEEDED(result));
        return result;
    }

    public int DisconnectPlayer(int phandle) {
        var result = GGPO.UggDisconnectPlayer(ggpo, phandle);
        Debug.Assert(GGPO.SUCCEEDED(result));
        return result;
    }

    public void SetFrameDelay(int phandle, int frame_delay) {
        var result = GGPO.UggSetFrameDelay(ggpo, phandle, frame_delay);
        Debug.Assert(GGPO.SUCCEEDED(result));
    }

    public void AdvanceFrame() {
        var result = GGPO.UggAdvanceFrame(ggpo);
        Debug.Assert(GGPO.SUCCEEDED(result));
    }

    public void Log(string v) {
        GGPO.UggLog(ggpo, v);
    }

    // Callbacks

    unsafe void FreeBuffer(void* dataPtr) {
        if (cache.TryGetValue((long)dataPtr, out var data)) {
            freeBufferCallback(data);
        }
    }

    unsafe bool SaveGameState(void** buffer, int* outLen, int* outChecksum, int frame) {
        var data = saveGameStateCallback(out int checksum, frame);
        var ptr = Helper.ToPtr(data);
        cache[(long)ptr] = data;

        *buffer = ptr;
        *outLen = data.Length;
        *outChecksum = checksum;
        return true;
    }

    unsafe bool LogGameState(string text, void* buffer, int length) {
        return logGameStateCallback(text, Helper.ToArray(buffer, length));
    }

    unsafe bool LoadGameState(void* buffer, int length) {
        return loadGameStateCallback(Helper.ToArray(buffer, length));
    }

    bool OnEventCallback(IntPtr evtPtr) {
        /*
        code = data[0];
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
            case GGPO.EVENTCODE_CONNECTED_TO_PEER:
                return onEventConnectedToPeer(data[1]);

            case GGPO.EVENTCODE_SYNCHRONIZING_WITH_PEER:
                return onEventSynchronizingWithPeer(data[1], data[2], data[3]);

            case GGPO.EVENTCODE_SYNCHRONIZED_WITH_PEER:
                return onEventSynchronizedWithPeer(data[1]);

            case GGPO.EVENTCODE_RUNNING:
                return onEventRunning();

            case GGPO.EVENTCODE_DISCONNECTED_FROM_PEER:
                return onEventDisconnectedFromPeer(data[1]);

            case GGPO.EVENTCODE_TIMESYNC:
                return onEventTimesync(data[1]);

            case GGPO.EVENTCODE_CONNECTION_INTERRUPTED:
                return onEventConnectionInterrupted(data[1], data[2]);

            case GGPO.EVENTCODE_CONNECTION_RESUMED:
                return onEventConnectionResumed(data[1]);
        }
        return false;
    }
}
