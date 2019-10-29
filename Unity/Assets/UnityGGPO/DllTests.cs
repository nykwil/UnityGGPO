using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using UnityEngine;

public class DllTests : MonoBehaviour {
    public int testId;
    public bool runTest;

    const int MAX_PLAYERS = 2;

    readonly static StringBuilder console = new StringBuilder();
    readonly Dictionary<long, NativeArray<byte>> cache = new Dictionary<long, NativeArray<byte>>();

    [Button]
    public void TestRealOnEventDelegate() {
        unsafe {
            GGPO.DllTestRealOnEventDelegate(RealOnEventCallback);
        }
    }

    [Button]
    public void TestRealSaveGameDelegate() {
        unsafe {
            GGPO.DllTestRealSaveGameDelegate(RealCallback, FreeBuffer);
        }
    }

    bool RealOnEventCallback(IntPtr info) {
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
        Marshal.Copy(info, data, 0, 4);
        switch (data[0]) {
            case GGPOC.GGPO_EVENTCODE_CONNECTED_TO_PEER:
                return OnEventConnectedToPeer(data[1]);

            case GGPOC.GGPO_EVENTCODE_SYNCHRONIZING_WITH_PEER:
                return OnEventSynchronizingWithPeer(data[1], data[2], data[3]);

            case GGPOC.GGPO_EVENTCODE_SYNCHRONIZED_WITH_PEER:
                return OnEventSynchronizedWithPeer(data[1]);

            case GGPOC.GGPO_EVENTCODE_RUNNING:
                return OnEventRunning();

            case GGPOC.GGPO_EVENTCODE_DISCONNECTED_FROM_PEER:
                return OnEventDisconnectedFromPeer(data[1]);

            case GGPOC.GGPO_EVENTCODE_TIMESYNC:
                return OnEventTimesync(data[1]);

            case GGPOC.GGPO_EVENTCODE_CONNECTION_INTERRUPTED:
                return OnEventConnectionInterrupted(data[1], data[2]);

            case GGPOC.GGPO_EVENTCODE_CONNECTION_RESUMED:
                return OnEventConnectionResumed(data[1]);
        }
        return false;
    }

    unsafe bool RealCallback(void** buffer, int* len, int* checksum, int frame) {
        var data = new NativeArray<byte>(12, Allocator.Persistent);
        for (int i = 0; i < data.Length; ++i) {
            data[i] = (byte)i;
        }
        var ptr = Helper.ToPtr(data);
        Debug.Log($"RealCallback({frame}, {(long)ptr})");
        cache[(long)ptr] = data;

        *buffer = ptr;
        *len = data.Length;
        *checksum = 99;
        return true;
    }

    // Use this for initialization
    void Start() {
        Log(string.Format("Plugin Version: {0} build {1}", GGPO.Version, GGPO.BuildNumber));
        GGPO.DllSetLogDelegate(Log);
    }

    void Update() {
        if (runTest) {
            runTest = false;
            Tests();
        }
    }

    bool BeginGame(string name) {
        Debug.Log($"BeginGame({name})");
        return true;
    }

    bool AdvanceFrame(int flags) {
        Debug.Log($"AdvanceFrame({flags})");
        return true;
    }

    unsafe void* SaveGameState(out int length, out int checksum, int frame) {
        var data = new NativeArray<byte>(12, Allocator.Persistent);
        for (int i = 0; i < data.Length; ++i) {
            data[i] = (byte)i;
        }
        var ptr = Helper.ToPtr(data);
        Debug.Log($"SaveGameState({frame}, {(long)ptr})");

        cache[(long)ptr] = data;
        length = data.Length;
        checksum = 99;
        return ptr;
    }

    unsafe bool LogGameState(string text, void* dataPtr, int length) {
        // var list = string.Join(",", Array.ConvertAll(data.ToArray(), x => x.ToString()));
        Debug.Log($"LogGameState({text})");
        return true;
    }

    unsafe bool LoadGameState(void* dataPtr, int length) {
        // var list = string.Join(",", Array.ConvertAll(data.ToArray(), x => x.ToString()));
        Debug.Log($"LoadGameState()");
        return true;
    }

    unsafe void FreeBuffer(void* dataPtr) {
        Debug.Log($"FreeBuffer({(long)dataPtr})");
        if (cache.TryGetValue((long)dataPtr, out var data)) {
            data.Dispose();
        }
    }

    bool OnEventTimesync(int timesync_frames_ahead) {
        Debug.Log($"OnEventEventcodeTimesync({timesync_frames_ahead})");
        return true;
    }

    bool OnEventDisconnectedFromPeer(int disconnected_player) {
        Debug.Log($"OnEventDisconnectedFromPeer({disconnected_player})");
        return true;
    }

    bool OnEventConnectionResumed(int connection_resumed_player) {
        Debug.Log($"OnEventConnectionResumed({connection_resumed_player})");
        return true;
    }

    bool OnEventConnectionInterrupted(int connection_interrupted_player, int connection_interrupted_disconnect_timeout) {
        Debug.Log($"OnEventConnectionInterrupted({connection_interrupted_player},{connection_interrupted_disconnect_timeout})");
        return true;
    }

    bool OnEventRunning() {
        Debug.Log($"OnEventRunning()");
        return true;
    }

    bool OnEventSynchronizedWithPeer(int synchronizing_player) {
        Debug.Log($"OnEventSynchronizedWithPeer({synchronizing_player})");
        return true;
    }

    bool OnEventSynchronizingWithPeer(int synchronizing_player, int synchronizing_count, int synchronizing_total) {
        Debug.Log($"OnEventSynchronizingWithPeer({synchronizing_player}, {synchronizing_count}, {synchronizing_total})");
        return true;
    }

    bool OnEventConnectedToPeer(int connected_player) {
        Debug.Log($"OnEventConnectedToPeer({connected_player})");
        return true;
    }

    public static void Log(string obj) {
        Debug.Log(obj);
        console.Append(obj + "\n");
    }

    void OnGUI() {
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), console.ToString());
    }

    void Tests() {
        int result = -1;
        int ggpo = 0;
        int timeout = 1;
        int player_type = 3;
        int player_num = 4;
        string player_ip_address = "127.0.0.1";
        short player_port = 9000;
        ulong[] inputs = new ulong[] { 3, 4 };
        int local_player_handle = 0;
        ulong input = 0;
        int time = 0;
        int phandle = 0;
        int frame_delay = 10;
        string logText = "";
        string host_ip = "127.0.0.1";
        int num_players = 2;
        int host_port = 0;
        int local_port = 0;

        switch (testId) {
            case 1:
                result = GGPO.DllSetDisconnectNotifyStart(ggpo, timeout);
                break;

            case 2:
                result = GGPO.DllSetDisconnectTimeout(ggpo, timeout);
                break;

            case 3:
                result = GGPO.DllSynchronizeInput(ggpo, inputs, MAX_PLAYERS, out int disconnect_flags);
                Debug.Log($"DllSynchronizeInput{disconnect_flags} {inputs[0]} {inputs[1]}");
                break;

            case 4:
                result = GGPO.DllAddLocalInput(ggpo, local_player_handle, input);
                break;

            case 5:
                result = GGPO.DllCloseSession(ggpo);
                break;

            case 6:
                result = GGPO.DllIdle(ggpo, time);
                break;

            case 7:
                result = GGPO.DllAddPlayer(ggpo, player_type, player_num, player_ip_address, player_port, out phandle);
                break;

            case 8:
                result = GGPO.DllDisconnectPlayer(ggpo, phandle);
                break;

            case 9:
                result = GGPO.DllSetFrameDelay(ggpo, phandle, frame_delay);
                break;

            case 10:
                result = GGPO.DllAdvanceFrame(ggpo);
                break;

            case 11:
                result = GGPO.DllGetNetworkStats(ggpo, phandle, out int send_queue_len, out int recv_queue_len, out int ping, out int kbps_sent, out int local_frames_behind, out int remote_frames_behind);
                Debug.Log($"DllSynchronizeInput{send_queue_len}, {recv_queue_len}, {ping}, {kbps_sent}, " +
                    $"{ local_frames_behind}, {remote_frames_behind}");
                break;

            case 12:
                unsafe {
                    result = GGPO.DllStartSession(BeginGame,
                        AdvanceFrame,
                        LoadGameState,
                        LogGameState,
                        SaveGameState,
                        FreeBuffer,
                        OnEventConnectedToPeer,
                        OnEventSynchronizingWithPeer,
                        OnEventSynchronizedWithPeer,
                        OnEventRunning,
                        OnEventConnectionInterrupted,
                        OnEventConnectionResumed,
                        OnEventDisconnectedFromPeer,
                        OnEventTimesync,
                        "Game", num_players, local_port);
                }
                break;

            case 13:
                unsafe {
                    result = GGPO.DllStartSpectating(BeginGame,
                        AdvanceFrame,
                        LoadGameState,
                        LogGameState,
                        SaveGameState,
                        FreeBuffer,
                        OnEventConnectedToPeer,
                        OnEventSynchronizingWithPeer,
                        OnEventSynchronizedWithPeer,
                        OnEventRunning,
                        OnEventConnectionInterrupted,
                        OnEventConnectionResumed,
                        OnEventDisconnectedFromPeer,
                        OnEventTimesync,
                        "Game", num_players, local_port, host_ip, host_port);
                }
                break;

            case 14:
                GGPO.DllLog(ggpo, logText);
                break;

            case 15:
                unsafe {
                    GGPO.DllTestGameStateDelegates(SaveGameState, LogGameState, LoadGameState, FreeBuffer);
                }
                break;
        }
        Debug.Log($"Result={result}");
    }
}
