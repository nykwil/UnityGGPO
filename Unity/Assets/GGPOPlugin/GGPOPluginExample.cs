using System;
using System.Text;
using Unity.Collections;
using UnityEngine;

public class GGPOPluginExample : MonoBehaviour {
    static StringBuilder console = new StringBuilder();

    GGPOSession session;
    NativeArray<byte> data;

    // Use this for initialization
    void Start() {
        data = new NativeArray<byte>(new byte[] { 0, 1, 2, 3, 4, 5, 6 }, Allocator.Persistent);
        Log(string.Format("Plugin Version: {0} build {1}", GGPO.Version, GGPO.BuildNumber));
        unsafe {
            GGPO.DllStartSession(BeginGame, AdvanceFrame, LoadGameState, LogGameState, SaveGameState, FreeBuffer, OnEv1, OnEv2, OnEv3, OnEv4, OnEv5, OnEv6, OnEv7, OnEv8, "Game", 2, sizeof(ulong), 9000);
        }
        //session = new GGPOSession();
        //session.logDelegate = Log;
        //session.StartSession(BeginGame, AdvanceFrame, LoadGameState, LogGameState, SaveGameState, OnEv1, OnEv2, OnEv3, OnEv4, OnEv5, OnEv6, OnEv7, OnEv8, "Game", 2, sizeof(ulong), 9000);
    }

    private unsafe void* SaveGameState(out int length, out int checksum, int frame) {
        var data = SafeSaveGameState(out checksum, frame);
        length = data.Length;
        return GGPO.ToPtr(data);
    }

    private unsafe bool LogGameState(void* buffer, int length) {
        return SafeLogGameState(GGPO.ToArray(buffer, length));
    }

    private unsafe bool LoadGameState(void* buffer, int length) {
        return SafeLoadGameState(GGPO.ToArray(buffer, length));
    }

    private unsafe void FreeBuffer(void* buffer, int length) {
        var data = GGPO.ToArray(buffer, length);
        data.Dispose();
    }

    private bool OnEv8(int timesync_frames_ahead) {
        Debug.Log($"OnEv8({timesync_frames_ahead})");
        return true;
    }

    private bool OnEv7(int disconnected_player) {
        Debug.Log($"OnEv7({disconnected_player})");
        return true;
    }

    private bool OnEv6(int connection_resumed_player) {
        Debug.Log($"OnEv6({connection_resumed_player})");
        return true;
    }

    private bool OnEv5(int connection_interrupted_player, int connection_interrupted_disconnect_timeout) {
        Debug.Log($"OnEv5({connection_interrupted_player},{connection_interrupted_disconnect_timeout})");
        return true;
    }

    private bool OnEv4() {
        Debug.Log($"OnEv4()");
        return true;
    }

    private bool OnEv3(int synchronizing_player) {
        Debug.Log($"OnEv3({synchronizing_player})");
        return true;
    }

    private bool OnEv2(int synchronizing_player, int synchronizing_count, int synchronizing_total) {
        Debug.Log($"OnEv2({synchronizing_player}, {synchronizing_count}, {synchronizing_total})");
        return true;
    }

    private bool OnEv1(int connected_player) {
        Debug.Log($"OnEv1({connected_player})");
        return true;
    }

    private NativeArray<byte> SafeSaveGameState(out int checksum, int frame) {
        var data = new NativeArray<byte>(12, Allocator.Persistent);
        for (int i = 0; i < data.Length; ++i) {
            data[i] = (byte)i;
        }
        checksum = 99;
        Debug.Log($"SaveGameState({frame})");
        return data;
    }

    private bool SafeLogGameState(NativeArray<byte> data) {
        var list = string.Join(",", Array.ConvertAll(data.ToArray(), x => x.ToString()));
        Debug.Log($"LoglistGameState({list})");
        return true;
    }

    private bool SafeLoadGameState(NativeArray<byte> data) {
        var list = string.Join(",", Array.ConvertAll(data.ToArray(), x => x.ToString()));
        Debug.Log($"LoadGameState({list})");
        return true;
    }

    private bool AdvanceFrame(int flags) {
        Debug.Log($"AdvanceFrame({flags})");
        return true;
    }

    private bool BeginGame() {
        Debug.Log($"BeginGame()");
        return true;
    }

    private void OnDestroy() {
        if (data.IsCreated) {
            data.Dispose();
        }
    }

    //unsafe void* Callback(out int length, out int checksum, int frame) {
    //    Debug.Log("Callback " + frame);
    //    checksum = 2;
    //    length = data.Length;
    //    return GGPO.ToPtr(data);
    //}

    //unsafe bool LogGameState(void* buffer, int length) {
    //    return true;
    //}

    public static void Log(string obj) {
        Debug.Log(obj);
        console.Append(obj + "\n");
    }

    void OnGUI() {
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), console.ToString());
    }
}
