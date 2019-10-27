using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using UnityEngine;

public class GGPOPluginExample : MonoBehaviour {
    public int testId;
    public bool runTest;

    static StringBuilder console = new StringBuilder();
    Dictionary<long, NativeArray<byte>> cache = new Dictionary<long, NativeArray<byte>>();
    GGPOSession session;

    public static string GetString(IntPtr ptrStr) {
        return ptrStr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptrStr) : "";
    }

    // Use this for initialization
    void Start() {
        Log(string.Format("Plugin Version: {0} build {1}", GGPO.Version, GGPO.BuildNumber));
        GGPO.DllSetLogDelegate(Log);
        runTest = true;
        session = new GGPOSession();
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
        var ptr = GGPO.ToPtr(data);
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

    unsafe void FreeBuffer(void* dataPtr, int length) {
        Debug.Log($"FreeBuffer({(long)dataPtr})");
        if (cache.TryGetValue((long)dataPtr, out var data)) {
            data.Dispose();
        }
    }

    bool OnEventEventcodeTimesync(int timesync_frames_ahead) {
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

    NativeArray<byte> SafeSaveGameState(out int checksum, int frame) {
        var data = new NativeArray<byte>(12, Allocator.Persistent);
        for (int i = 0; i < data.Length; ++i) {
            data[i] = (byte)i;
        }
        checksum = 99;
        Debug.Log($"SafeSaveGameState({frame})");
        return data;
    }

    bool SafeLogGameState(NativeArray<byte> data) {
        // var list = string.Join(",", Array.ConvertAll(data.ToArray(), x => x.ToString()));
        Debug.Log($"SafeLogGameState({data.Length})");
        return true;
    }

    bool SafeLoadGameState(NativeArray<byte> data) {
        // var list = string.Join(",", Array.ConvertAll(data.ToArray(), x => x.ToString()));
        Debug.Log($"SafeLoadGameState({data.Length})");
        return true;
    }

    public static void Log(string obj) {
        Debug.Log(obj);
        console.Append(obj + "\n");
    }

    void OnGUI() {
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), console.ToString());
    }

    int MAX_PLAYERS = 2;

    void Tests() {
        int result;
        int ggpo = 0;
        int timeout = 1;
        int player_size = 2;
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

        switch (testId) {
            case 1:
                result = GGPO.DllSetDisconnectNotifyStart(ggpo, timeout);
                break;

            case 2:
                result = GGPO.DllSetDisconnectTimeout(ggpo, timeout);
                break;

            case 3:
                result = GGPO.DllSynchronizeInput(ggpo, inputs, sizeof(ulong) * MAX_PLAYERS, out int disconnect_flags);
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
                result = GGPO.DllAddPlayer(ggpo, player_size, player_type, player_num, player_ip_address, player_port, out phandle);
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
                    GGPO.DllTestGameStateDelegates(SaveGameState, LogGameState, LoadGameState, FreeBuffer);
                }
                break;

            case 13:
                unsafe {
                    result = GGPO.DllTestStartSession(BeginGame, AdvanceFrame, LoadGameState, LogGameState, SaveGameState, FreeBuffer,
                        OnEventConnectedToPeer, OnEventSynchronizingWithPeer, OnEventSynchronizedWithPeer, OnEventRunning, OnEventConnectionInterrupted,
                        OnEventConnectionResumed, OnEventDisconnectedFromPeer, OnEventEventcodeTimesync, "Game", 2, sizeof(ulong), 9000);
                }
                break;

            case 14:
                GGPO.DllLog(ggpo, logText);
                break;
        }
    }
}
