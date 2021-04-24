using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using UnityEngine;
using UnityGGPO;

namespace Tests {

    public class UggTests : MonoBehaviour {
        public int runTestId;
        public bool runTest;
        public int maxPlayers = 2;
        public int result = -1;
        public int timeout = 1;
        public int player_type = 3;
        public int player_num = 4;
        public string player_ip_address = "127.0.0.1";
        public ushort player_port = 9000;
        public long[] inputs = new long[] { 3, 4 };
        public int local_player_handle = 0;
        public long input = 0;
        public int time = 0;
        public int phandle = 0;
        public int frame_delay = 10;
        public string logText = "";
        public string host_ip = "127.0.0.1";
        public int num_players = 2;
        public int host_port = 0;
        public int local_port = 0;

        private IntPtr ggpo;

        private IntPtr ptrBeginGame;
        private IntPtr ptrAdvanceFrame;
        private IntPtr ptrLoadGameState;
        private IntPtr ptrLogGameState;
        private IntPtr ptrSaveGameState;
        private IntPtr ptrFreeBuffer;
        private IntPtr ptrOnEvent;

        private static readonly StringBuilder console = new StringBuilder();
        private readonly Dictionary<long, NativeArray<byte>> cache = new Dictionary<long, NativeArray<byte>>();

        private void Update() {
            if (runTest) {
                RunTest(runTestId);
                runTest = false;
            }
        }

        private bool OnEventCallback(IntPtr info) {
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
                case GGPO.EVENTCODE_CONNECTED_TO_PEER:
                    return OnEventConnectedToPeer(data[1]);

                case GGPO.EVENTCODE_SYNCHRONIZING_WITH_PEER:
                    return OnEventSynchronizingWithPeer(data[1], data[2], data[3]);

                case GGPO.EVENTCODE_SYNCHRONIZED_WITH_PEER:
                    return OnEventSynchronizedWithPeer(data[1]);

                case GGPO.EVENTCODE_RUNNING:
                    return OnEventRunning();

                case GGPO.EVENTCODE_DISCONNECTED_FROM_PEER:
                    return OnEventDisconnectedFromPeer(data[1]);

                case GGPO.EVENTCODE_TIMESYNC:
                    return OnEventTimesync(data[1]);

                case GGPO.EVENTCODE_CONNECTION_INTERRUPTED:
                    return OnEventConnectionInterrupted(data[1], data[2]);

                case GGPO.EVENTCODE_CONNECTION_RESUMED:
                    return OnEventConnectionResumed(data[1]);

                default:
                    break;
            }
            return false;
        }

        private void Start() {
            Log(string.Format("Plugin Version: {0} build {1}", GGPO.Version, GGPO.BuildNumber));
            GGPO.SetLogDelegate(Log);
        }

        private bool OnBeginGame(string name) {
            Debug.Log($"OnBeginGame({name})");
            return true;
        }

        private bool OnAdvanceFrame(int flags) {
            Debug.Log($"OnAdvanceFrame({flags})");
            return true;
        }

        private unsafe bool OnSaveGameState(void** buffer, int* outLen, int* outChecksum, int frame) {
            Debug.Log($"OnSaveGameState({frame})");
            var data = new NativeArray<byte>(12, Allocator.Persistent);
            for (int i = 0; i < data.Length; ++i) {
                data[i] = (byte)i;
            }
            var ptr = Utils.ToPtr(data);
            cache[(long)ptr] = data;

            *buffer = ptr;
            *outLen = data.Length;
            *outChecksum = 99;
            return true;
        }

        private unsafe bool OnLogGameState(string filename, void* dataPtr, int length) {
            // var list = string.Join(",", Array.ConvertAll(data.ToArray(), x => x.ToString()));
            Debug.Log($"OnLogGameState({filename})");
            return true;
        }

        private unsafe bool OnLoadGameState(void* dataPtr, int length) {
            // var list = string.Join(",", Array.ConvertAll(data.ToArray(), x => x.ToString()));
            Debug.Log($"OnLoadGameState()");
            return true;
        }

        private unsafe void OnFreeBuffer(void* dataPtr) {
            if (dataPtr != null) {
                Debug.Log($"OnFreeBuffer({(long)dataPtr})");
                if (cache.TryGetValue((long)dataPtr, out var data)) {
                    data.Dispose();
                    cache.Remove((long)dataPtr);
                }
            }
        }

        private bool OnEventTimesync(int timesync_frames_ahead) {
            Debug.Log($"OnEventEventcodeTimesync({timesync_frames_ahead})");
            return true;
        }

        private bool OnEventDisconnectedFromPeer(int disconnected_player) {
            Debug.Log($"OnEventDisconnectedFromPeer({disconnected_player})");
            return true;
        }

        private bool OnEventConnectionResumed(int connection_resumed_player) {
            Debug.Log($"OnEventConnectionResumed({connection_resumed_player})");
            return true;
        }

        private bool OnEventConnectionInterrupted(int connection_interrupted_player, int connection_interrupted_disconnect_timeout) {
            Debug.Log($"OnEventConnectionInterrupted({connection_interrupted_player},{connection_interrupted_disconnect_timeout})");
            return true;
        }

        private bool OnEventRunning() {
            Debug.Log($"OnEventRunning()");
            return true;
        }

        private bool OnEventSynchronizedWithPeer(int synchronized_player) {
            Debug.Log($"OnEventSynchronizedWithPeer({synchronized_player})");
            return true;
        }

        private bool OnEventSynchronizingWithPeer(int synchronizing_player, int synchronizing_count, int synchronizing_total) {
            Debug.Log($"OnEventSynchronizingWithPeer({synchronizing_player}, {synchronizing_count}, {synchronizing_total})");
            return true;
        }

        private bool OnEventConnectedToPeer(int connected_player) {
            Debug.Log($"OnEventConnectedToPeer({connected_player})");
            return true;
        }

        public static void Log(string obj) {
            Debug.Log(obj);
            console.Append(obj + "\n");
        }

        private void OnGUI() {
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), console.ToString());
        }

        public void RunTest(int testId) {
            unsafe {
                ptrBeginGame = Marshal.GetFunctionPointerForDelegate<GGPO.BeginGameDelegate>(OnBeginGame);
                ptrAdvanceFrame = Marshal.GetFunctionPointerForDelegate<GGPO.AdvanceFrameDelegate>(OnAdvanceFrame);
                ptrLoadGameState = Marshal.GetFunctionPointerForDelegate<GGPO.LoadGameStateDelegate>(OnLoadGameState);
                ptrLogGameState = Marshal.GetFunctionPointerForDelegate<GGPO.LogGameStateDelegate>(OnLogGameState);
                ptrSaveGameState = Marshal.GetFunctionPointerForDelegate<GGPO.SaveGameStateDelegate>(OnSaveGameState);
                ptrFreeBuffer = Marshal.GetFunctionPointerForDelegate<GGPO.FreeBufferDelegate>(OnFreeBuffer);
                ptrOnEvent = Marshal.GetFunctionPointerForDelegate<GGPO.OnEventDelegate>(OnEventCallback);
            }

            switch (testId) {
                case 0:
                    GGPO.StartSession(out ggpo,
                        ptrBeginGame,
                        ptrAdvanceFrame,
                        ptrLoadGameState,
                        ptrLogGameState,
                        ptrSaveGameState,
                        ptrFreeBuffer,
                        ptrOnEvent,
                        "Tests", num_players, local_port);

                    Debug.Assert(ggpo != IntPtr.Zero);
                    break;

                case 1:
                    GGPO.StartSpectating(out ggpo,
                        ptrBeginGame,
                        ptrAdvanceFrame,
                        ptrLoadGameState,
                        ptrLogGameState,
                        ptrSaveGameState,
                        ptrFreeBuffer,
                        ptrOnEvent,
                        "Tests", num_players, local_port, host_ip, host_port);

                    Debug.Assert(ggpo != IntPtr.Zero);
                    break;

                case 2:
                    result = GGPO.UggTestStartSession(out ggpo,
                        ptrBeginGame,
                        ptrAdvanceFrame,
                        ptrLoadGameState,
                        ptrLogGameState,
                        ptrSaveGameState,
                        ptrFreeBuffer,
                        ptrOnEvent,
                        "Tests", num_players, local_port);

                    Debug.Assert(ggpo != IntPtr.Zero);
                    break;

                case 3:
                    inputs = GGPO.SynchronizeInput(ggpo, maxPlayers, out int disconnect_flags);
                    Debug.Log($"DllSynchronizeInput{disconnect_flags} {inputs[0]} {inputs[1]}");
                    break;

                case 4:
                    result = GGPO.AddLocalInput(ggpo, local_player_handle, input);
                    break;

                case 5:
                    result = GGPO.CloseSession(ggpo);
                    ggpo = IntPtr.Zero;
                    break;

                case 6:
                    result = GGPO.Idle(ggpo, time);
                    break;

                case 7:
                    result = GGPO.AddPlayer(ggpo, player_type, player_num, player_ip_address, player_port, out phandle);
                    break;

                case 8:
                    result = GGPO.DisconnectPlayer(ggpo, phandle);
                    break;

                case 9:
                    result = GGPO.SetFrameDelay(ggpo, phandle, frame_delay);
                    break;

                case 10:
                    result = GGPO.AdvanceFrame(ggpo);
                    break;

                case 11:
                    result = GGPO.GetNetworkStats(ggpo, phandle, out int send_queue_len, out int recv_queue_len, out int ping, out int kbps_sent, out int local_frames_behind, out int remote_frames_behind);
                    Debug.Log($"DllSynchronizeInput{send_queue_len}, {recv_queue_len}, {ping}, {kbps_sent}, " +
                        $"{ local_frames_behind}, {remote_frames_behind}");
                    break;

                case 12:
                    GGPO.Log(ggpo, logText);
                    result = GGPO.OK;
                    break;

                case 13:
                    result = GGPO.SetDisconnectNotifyStart(ggpo, timeout);
                    break;

                case 14:
                    result = GGPO.SetDisconnectTimeout(ggpo, timeout);
                    break;
            }
            ReportFailure(result);
        }

        public static void ReportFailure(int result) {
            if (result != GGPO.ERRORCODE_SUCCESS) {
                Debug.LogWarning(GGPO.GetErrorCodeMessage(result));
            }
        }

        private void OnDestroy() {
            foreach (var c in cache.Values) {
                if (c.IsCreated) {
                    c.Dispose();
                }
            }
            cache.Clear();
        }
    }
}