using System.Text;
using Unity.Collections;
using UnityEngine;
using UnityGGPO;
using Unity.Burst;

namespace Tests {

    public class SessionTests : MonoBehaviour {
        public int testId;
        public bool runTest;

        private const int MAX_PLAYERS = 2;

        private static readonly StringBuilder console = new StringBuilder();

        public string gameName = "SessionTest";
        public int localPort = 7000;
        public int numPlayers = 2;
        public int timeout = 1;
        public long[] inputs = new long[] { 3, 4 };
        public int local_player_handle = 0;
        public long input = 0;
        public int time = 0;
        public int phandle = 0;
        public int frame_delay = 10;
        public string logText = "";
        public int hostPort = 7000;
        public string hostIp = "127.0.0.1";

        public GGPOPlayer player;

        private void Start() {
            Log(string.Format("Plugin Version: {0} build {1}", GGPO.Version, GGPO.BuildNumber));
            GGPO.Session.Init(Log);
        }

        private void Update() {
            if (runTest) {
                runTest = false;
                RunTest(testId);
            }
        }

        private bool OnBeginGame(string name) {
            Debug.Log($"OnBeginGame({name})");
            return true;
        }

        private bool OnAdvanceFrame(int flags) {
            Debug.Log($"OnAdvanceFrame({flags})");
            return true;
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

        private bool OnSaveGameState(out NativeArray<byte> data, out int checksum, int frame) {
            Debug.Log($"OnSaveGameState({frame})");
            data = new NativeArray<byte>(12, Allocator.Persistent);
            for (int i = 0; i < data.Length; ++i) {
                data[i] = (byte)i;
            }
            checksum = Utils.CalcFletcher32(data);
            Debug.Log($"SafeSaveGameState({frame})");
            return true;
        }

        private bool OnLogGameState(string filename, NativeArray<byte> data) {
            // var list = string.Join(",", Array.ConvertAll(data.ToArray(), x => x.ToString()));
            Debug.Log($"OnLogGameState({filename},{data.Length})");
            return true;
        }

        private bool OnLoadGameState(NativeArray<byte> data) {
            // var list = string.Join(",", Array.ConvertAll(data.ToArray(), x => x.ToString()));
            Debug.Log($"OnLoadGameState({data.Length})");
            return true;
        }

        private void OnFreeBuffer(NativeArray<byte> data) {
            // var list = string.Join(",", Array.ConvertAll(data.ToArray(), x => x.ToString()));
            Debug.Log($"OnFreeBuffer({data.Length})");
            data.Dispose();
        }

        public static void Log(string obj) {
            Debug.Log(obj);
            console.Append(obj + "\n");
        }

        private void OnGUI() {
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), console.ToString());
        }

        private void RunTest(int testId) {
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
                    var inputs = GGPO.Session.SynchronizeInput(MAX_PLAYERS, out int disconnect_flags);
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
}