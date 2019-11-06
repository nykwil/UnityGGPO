using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public static class Helper {

    public static int CalcFletcher32(NativeArray<byte> data) {
        uint sum1 = 0;
        uint sum2 = 0;

        int index;
        for (index = 0; index < data.Length; ++index) {
            sum1 = (sum1 + data[index]) % 0xffff;
            sum2 = (sum2 + sum1) % 0xffff;
        }
        return unchecked((int)((sum2 << 16) | sum1));
    }

    public static string GetString(IntPtr ptrStr) {
        return ptrStr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptrStr) : "";
    }

    unsafe public static void* ToPtr(NativeArray<byte> data) {
        unsafe {
            return NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(data);
        }
    }

    unsafe public static NativeArray<byte> ToArray(void* dataPointer, int length) {
        unsafe {
            return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(dataPointer, length, Allocator.Persistent);
        }
    }

    public static int TimeGetTime() {
        return (int)(UnityEngine.Time.time * 1000);
    }

    public static void Sleep(int ms) {
        Thread.Sleep(ms);
    }
}

public class SessionTests : MonoBehaviour {
    public int testId;
    public bool runTest;

    const int MAX_PLAYERS = 2;

    readonly static StringBuilder console = new StringBuilder();

    public string gameName = "SessionTest";
    public int localPort = 7000;
    public int numPlayers = 2;

    void Start() {
        Log(string.Format("Plugin Version: {0} build {1}", GGPO.Version, GGPO.BuildNumber));
        GGPO.Session.Init(Log);
    }

    void Update() {
        if (runTest) {
            runTest = false;
            RunTest(testId);
        }
    }

    bool OnBeginGame(string name) {
        Debug.Log($"OnBeginGame({name})");
        return true;
    }

    bool OnAdvanceFrame(int flags) {
        Debug.Log($"OnAdvanceFrame({flags})");
        return true;
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

    NativeArray<byte> OnSaveGameState(out int checksum, int frame) {
        Debug.Log($"OnSaveGameState({frame})");
        var data = new NativeArray<byte>(12, Allocator.Persistent);
        for (int i = 0; i < data.Length; ++i) {
            data[i] = (byte)i;
        }
        checksum = Helper.CalcFletcher32(data);
        Debug.Log($"SafeSaveGameState({frame})");
        return data;
    }

    bool OnLogGameState(string filename, NativeArray<byte> data) {
        // var list = string.Join(",", Array.ConvertAll(data.ToArray(), x => x.ToString()));
        Debug.Log($"OnLogGameState({filename},{data.Length})");
        return true;
    }

    bool OnLoadGameState(NativeArray<byte> data) {
        // var list = string.Join(",", Array.ConvertAll(data.ToArray(), x => x.ToString()));
        Debug.Log($"OnLoadGameState({data.Length})");
        return true;
    }

    void OnFreeBuffer(NativeArray<byte> data) {
        // var list = string.Join(",", Array.ConvertAll(data.ToArray(), x => x.ToString()));
        Debug.Log($"OnFreeBuffer({data.Length})");
        data.Dispose();
    }

    public static void Log(string obj) {
        Debug.Log(obj);
        console.Append(obj + "\n");
    }

    void OnGUI() {
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), console.ToString());
    }

    void RunTest(int testId) {
        int timeout = 1;
        ulong[] inputs = new ulong[] { 3, 4 };
        int local_player_handle = 0;
        ulong input = 0;
        int time = 0;
        int phandle = 0;
        int frame_delay = 10;
        string logText = "";
        var player = new GGPOPlayer();
        player.type = GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL;
        player.player_num = 1;
        player.ip_address = "127.0.0.1";
        player.port = 9000;

        int hostPort = 7000;
        string hostIp = "127.0.0.1";

        switch (testId) {
            case 0:
                GGPO.Session.StartSession(OnBeginGame, OnAdvanceFrame, OnLoadGameState, OnLogGameState, OnSaveGameState, OnFreeBuffer,
                    OnEventConnectedToPeer, OnEventSynchronizingWithPeer, OnEventSynchronizedWithPeer, OnEventRunning, OnEventConnectionInterrupted,
                    OnEventConnectionResumed, OnEventDisconnectedFromPeer, OnEventTimesync, "Game", numPlayers, localPort);
                break;

            case 1:
                GGPO.Session.StartSpectating(OnBeginGame, OnAdvanceFrame, OnLoadGameState, OnLogGameState, OnSaveGameState, OnFreeBuffer,
                    OnEventConnectedToPeer, OnEventSynchronizingWithPeer, OnEventSynchronizedWithPeer, OnEventRunning, OnEventConnectionInterrupted,
                    OnEventConnectionResumed, OnEventDisconnectedFromPeer, OnEventTimesync, "Game", numPlayers, localPort, hostIp, hostPort);
                break;

            case 2:
                GGPO.Session.SetDisconnectTimeout(timeout);
                break;

            case 3:
                GGPO.Session.SynchronizeInput(inputs, MAX_PLAYERS, out int disconnect_flags);
                Debug.Log($"DllSynchronizeInput{disconnect_flags} {inputs[0]} {inputs[1]}");
                break;

            case 4:
                GGPO.Session.AddLocalInput(local_player_handle, input);
                break;

            case 5:
                GGPO.Session.CloseSession();
                break;

            case 6:
                GGPO.Session.Idle(time);
                break;

            case 7:
                GGPO.Session.AddPlayer(player, out phandle);
                break;

            case 8:
                GGPO.Session.DisconnectPlayer(phandle);
                break;

            case 9:
                GGPO.Session.SetFrameDelay(phandle, frame_delay);
                break;

            case 10:
                GGPO.Session.AdvanceFrame();
                break;

            case 11:
                GGPO.Session.GetNetworkStats(phandle, out var stats);
                Debug.Log($"DllSynchronizeInput{stats.send_queue_len}, {stats.recv_queue_len}, {stats.ping}, {stats.kbps_sent}, " +
                    $"{stats.local_frames_behind}, {stats.remote_frames_behind}");
                break;

            case 12:
                GGPO.Session.SetDisconnectNotifyStart(timeout);
                break;

            case 13:
                GGPO.Session.Log(logText);
                break;
        }
    }
}
