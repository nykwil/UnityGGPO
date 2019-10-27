using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using UnityEngine;

public class GGPOPluginExample : MonoBehaviour {
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

        unsafe {
            // GGPO.DllTestStart(BeginGame, AdvanceFrame, LoadGameState, "GAME", 1, 1, 1);
            GGPO.DllStartSession(BeginGame, AdvanceFrame, LoadGameState, LogGameState, SaveGameState, FreeBuffer, OnEv1, OnEv2, OnEv3, OnEv4, OnEv5, OnEv6, OnEv7, OnEv8, "Game", 2, sizeof(ulong), 9000);
        }
        //session = new GGPOSession();
        //session.logDelegate = Log;
        //session.StartSession(BeginGame, AdvanceFrame, LoadGameState, LogGameState, SaveGameState, OnEv1, OnEv2, OnEv3, OnEv4, OnEv5, OnEv6, OnEv7, OnEv8, "Game", 2, sizeof(ulong), 9000);
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

    bool OnEv8(int timesync_frames_ahead) {
        Debug.Log($"OnEv8({timesync_frames_ahead})");
        return true;
    }

    bool OnEv7(int disconnected_player) {
        Debug.Log($"OnEv7({disconnected_player})");
        return true;
    }

    bool OnEv6(int connection_resumed_player) {
        Debug.Log($"OnEv6({connection_resumed_player})");
        return true;
    }

    bool OnEv5(int connection_interrupted_player, int connection_interrupted_disconnect_timeout) {
        Debug.Log($"OnEv5({connection_interrupted_player},{connection_interrupted_disconnect_timeout})");
        return true;
    }

    bool OnEv4() {
        Debug.Log($"OnEv4()");
        return true;
    }

    bool OnEv3(int synchronizing_player) {
        Debug.Log($"OnEv3({synchronizing_player})");
        return true;
    }

    bool OnEv2(int synchronizing_player, int synchronizing_count, int synchronizing_total) {
        Debug.Log($"OnEv2({synchronizing_player}, {synchronizing_count}, {synchronizing_total})");
        return true;
    }

    bool OnEv1(int connected_player) {
        Debug.Log($"OnEv1({connected_player})");
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

    void Tests() {
        GGPO.DllSetLogDelegate(Log);

        unsafe {
            // GGPO.DllTestGameStateDelegates(SaveGameState, LogGameState, LoadGameState, FreeBuffer);
        }
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
        int frame_delay = 10;
        string logText = "";

        result = GGPO.DllSetDisconnectNotifyStart(ggpo, timeout);

        result = GGPO.DllSetDisconnectTimeout(ggpo, timeout);

        result = GGPO.DllSynchronizeInput(ggpo, inputs, out int disconnect_flags);

        result = GGPO.DllAddLocalInput(ggpo, local_player_handle, input);

        result = GGPO.DllCloseSession(ggpo);

        result = GGPO.DllIdle(ggpo, time);

        result = GGPO.DllAddPlayer(ggpo,
            player_size,
            player_type,
            player_num,
            player_ip_address,
            player_port,
            out int phandle);

        result = GGPO.DllDisconnectPlayer(ggpo, phandle);

        result = GGPO.DllSetFrameDelay(ggpo, phandle, frame_delay);
        result = GGPO.DllAdvanceFrame(ggpo);

        GGPO.DllLog(ggpo, logText);

        result = GGPO.DllGetNetworkStats(ggpo, phandle,
            out int send_queue_len,
            out int recv_queue_len,
            out int ping,
            out int kbps_sent,
            out int local_frames_behind,
            out int remote_frames_behind
        );
    }
}
