using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using UnityEngine;

public class UggTests : MonoBehaviour {
    public int testId;
    public bool runTest;

    const int MAX_PLAYERS = 2;
    IntPtr ggpo;

    readonly static StringBuilder console = new StringBuilder();
    readonly Dictionary<long, NativeArray<byte>> cache = new Dictionary<long, NativeArray<byte>>();

    bool OnEventCallback(IntPtr info) {
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

    void Start() {
        Log(string.Format("Plugin Version: {0} build {1}", UGGPO.Version, UGGPO.BuildNumber));
        UGGPO.UggSetLogDelegate(Log);
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

    unsafe bool SaveGameState(void** buffer, int* outLen, int* outChecksum, int frame) {
        var data = new NativeArray<byte>(12, Allocator.Persistent);
        for (int i = 0; i < data.Length; ++i) {
            data[i] = (byte)i;
        }
        var ptr = Helper.ToPtr(data);
        cache[(long)ptr] = data;

        *buffer = ptr;
        *outLen = data.Length;
        *outChecksum = 99;
        return true;
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
            case 0:
                unsafe {
                    ggpo = UGGPO.UggStartSession(BeginGame,
                        AdvanceFrame,
                        LoadGameState,
                        LogGameState,
                        SaveGameState,
                        FreeBuffer,
                        OnEventCallback,
                        "Game", num_players, local_port);
                }
                break;

            case 1:
                unsafe {
                    ggpo = UGGPO.UggStartSpectating(BeginGame,
                        AdvanceFrame,
                        LoadGameState,
                        LogGameState,
                        SaveGameState,
                        FreeBuffer,
                        OnEventCallback,
                        "Game", num_players, local_port, host_ip, host_port);
                }
                break;

            case 2:
                result = UGGPO.UggSetDisconnectTimeout(ggpo, timeout);
                break;

            case 3:
                result = UGGPO.UggSynchronizeInput(ggpo, inputs, MAX_PLAYERS, out int disconnect_flags);
                Debug.Log($"DllSynchronizeInput{disconnect_flags} {inputs[0]} {inputs[1]}");
                break;

            case 4:
                result = UGGPO.UggAddLocalInput(ggpo, local_player_handle, input);
                break;

            case 5:
                result = UGGPO.UggCloseSession(ggpo);
                break;

            case 6:
                result = UGGPO.UggIdle(ggpo, time);
                break;

            case 7:
                result = UGGPO.UggAddPlayer(ggpo, player_type, player_num, player_ip_address, player_port, out phandle);
                break;

            case 8:
                result = UGGPO.UggDisconnectPlayer(ggpo, phandle);
                break;

            case 9:
                result = UGGPO.UggSetFrameDelay(ggpo, phandle, frame_delay);
                break;

            case 10:
                result = UGGPO.UggAdvanceFrame(ggpo);
                break;

            case 11:
                result = UGGPO.UggGetNetworkStats(ggpo, phandle, out int send_queue_len, out int recv_queue_len, out int ping, out int kbps_sent, out int local_frames_behind, out int remote_frames_behind);
                Debug.Log($"DllSynchronizeInput{send_queue_len}, {recv_queue_len}, {ping}, {kbps_sent}, " +
                    $"{ local_frames_behind}, {remote_frames_behind}");
                break;

            case 12:
                UGGPO.UggLog(ggpo, logText);
                break;

            case 13:
                result = UGGPO.UggSetDisconnectNotifyStart(ggpo, timeout);
                break;
        }
        Debug.Log($"Result={result}");
    }
}
