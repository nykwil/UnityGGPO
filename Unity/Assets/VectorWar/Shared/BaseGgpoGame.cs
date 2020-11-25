using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using UnityEngine;

namespace SharedGame {

    [Serializable]
    public class Connections {
        public ushort port;
        public string ip;
        public bool spectator;
    }

    public abstract class BaseGgpoGame : IGame {
        public List<Connections> connections;

        public int PlayerIndex { get; set; }

        public const int MAX_PLAYERS = 2;
        private const int FRAME_DELAY = 2;

        public IGameState gs { get; private set; }
        public GameInfo ngs { get; private set; }

        public GGPOPerformance perf = null;

        public static event Action<string> OnLog;

        public Stopwatch frameWatch = new Stopwatch();
        public Stopwatch idleWatch = new Stopwatch();

        /*
         * The begin game callback.  We don't need to do anything special here,
         * so just return true.
         */

        private bool OnBeginGameCallback(string name) {
            Log($"OnBeginGameCallback");
            return true;
        }

        /*
         * Notification from GGPO that something has happened.  Update the status
         * text at the bottom of the screen to notify the user.
         */

        private bool OnEventConnectedToPeerDelegate(int connected_player) {
            ngs.SetConnectState(connected_player, PlayerConnectState.Synchronizing);
            return true;
        }

        public bool OnEventSynchronizingWithPeerDelegate(int synchronizing_player, int synchronizing_count, int synchronizing_total) {
            var progress = 100 * synchronizing_count / synchronizing_total;
            ngs.UpdateConnectProgress(synchronizing_player, progress);
            return true;
        }

        public bool OnEventSynchronizedWithPeerDelegate(int synchronized_player) {
            ngs.UpdateConnectProgress(synchronized_player, 100);
            return true;
        }

        public bool OnEventRunningDelegate() {
            ngs.SetConnectState(PlayerConnectState.Running);
            SetStatusText("");
            return true;
        }

        public bool OnEventConnectionInterruptedDelegate(int connection_interrupted_player, int connection_interrupted_disconnect_timeout) {
            ngs.SetDisconnectTimeout(connection_interrupted_player,
                                     Helper.TimeGetTime(),
                                     connection_interrupted_disconnect_timeout);
            return true;
        }

        public bool OnEventConnectionResumedDelegate(int connection_resumed_player) {
            ngs.SetConnectState(connection_resumed_player, PlayerConnectState.Running);
            return true;
        }

        public bool OnEventDisconnectedFromPeerDelegate(int disconnected_player) {
            ngs.SetConnectState(disconnected_player, PlayerConnectState.Disconnected);
            return true;
        }

        public bool OnEventEventcodeTimesyncDelegate(int timesync_frames_ahead) {
            Helper.Sleep(1000 * timesync_frames_ahead / 60);
            return true;
        }

        /*
         * Notification from GGPO we should step foward exactly 1 frame
         * during a rollback.
         */

        private bool OnAdvanceFrameCallback(int flags) {
            Log($"OnAdvanceFrameCallback {flags}");

            // Make sure we fetch new inputs from GGPO and use those to update the game state
            // instead of reading from the keyboard.
            ulong[] inputs = new ulong[MAX_PLAYERS];
            CheckAndReport(GGPO.Session.SynchronizeInput(inputs, MAX_PLAYERS, out var disconnect_flags));

            AdvanceFrame(inputs, disconnect_flags);
            return true;
        }

        /*
         * Makes our current state match the state passed in by GGPO.
         */

        private bool OnLoadGameStateCallback(NativeArray<byte> data) {
            Log($"OnLoadGameStateCallback {data.Length}");
            gs.FromBytes(data);
            return true;
        }

        /*
         * Save the current state to a buffer and return it to GGPO via the
         * buffer and len parameters.
         */

        private bool OnSaveGameStateCallback(out NativeArray<byte> data, out int checksum, int frame) {
            Log($"OnSaveGameStateCallback {frame}");
            data = gs.ToBytes();
            checksum = Helper.CalcFletcher32(data);
            return true;
        }

        /*
         * Log the gamestate.  Used by the synctest debugging tool.
         */

        private bool OnLogGameState(string filename, NativeArray<byte> data) {
            Log($"OnLogGameState {filename}");

            var gamestate = this.CreateGameState();
            gamestate.FromBytes(data);
            gamestate.LogInfo(filename);
            return true;
        }

        protected abstract IGameState CreateGameState();

        private void OnFreeBufferCallback(NativeArray<byte> data) {
            Log($"OnFreeBufferCallback");
            if (data.IsCreated) {
                data.Dispose();
            }
        }

        /*
         * Initialize the game.  This initializes the game state and
         * the video renderer and creates a new network session.
         */

        protected void Init(int localport, int num_players, IList<GGPOPlayer> players, int num_spectators) {
            // Initialize the game state
            gs.Init(num_players);

#if SYNC_TEST
            var result = ggpo_start_synctest(cb, GetName(), num_players, 1);
#else
            var result = GGPO.Session.StartSession(OnBeginGameCallback,
                    OnAdvanceFrameCallback,
                    OnLoadGameStateCallback,
                    OnLogGameState,
                    OnSaveGameStateCallback,
                    OnFreeBufferCallback,
                    OnEventConnectedToPeerDelegate,
                    OnEventSynchronizingWithPeerDelegate,
                    OnEventSynchronizedWithPeerDelegate,
                    OnEventRunningDelegate,
                    OnEventConnectionInterruptedDelegate,
                    OnEventConnectionResumedDelegate,
                    OnEventDisconnectedFromPeerDelegate,
                    OnEventEventcodeTimesyncDelegate,
                    GetName(), num_players, localport);

#endif
            CheckAndReport(result);

            // automatically disconnect clients after 3000 ms and start our count-down timer for
            // disconnects after 1000 ms. To completely disable disconnects, simply use a value of 0
            // for ggpo_set_disconnect_timeout.
            CheckAndReport(GGPO.Session.SetDisconnectTimeout(3000));
            CheckAndReport(GGPO.Session.SetDisconnectNotifyStart(1000));

            int controllerId = 0;
            int playerIndex = 0;
            ngs.players = new PlayerConnectionInfo[num_players];
            for (int i = 0; i < players.Count; i++) {
                CheckAndReport(GGPO.Session.AddPlayer(players[i], out int handle));

                if (players[i].type == GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL) {
                    var playerInfo = new PlayerConnectionInfo();
                    playerInfo.handle = handle;
                    playerInfo.type = players[i].type;
                    playerInfo.connect_progress = 100;
                    playerInfo.controllerId = controllerId++;
                    ngs.players[playerIndex++] = playerInfo;
                    ngs.SetConnectState(handle, PlayerConnectState.Connecting);
                    CheckAndReport(GGPO.Session.SetFrameDelay(handle, FRAME_DELAY));
                }
                else if (players[i].type == GGPOPlayerType.GGPO_PLAYERTYPE_REMOTE) {
                    var playerInfo = new PlayerConnectionInfo();
                    playerInfo.handle = handle;
                    playerInfo.type = players[i].type;
                    playerInfo.connect_progress = 0;
                    ngs.players[playerIndex++] = playerInfo;
                }
            }

            perf?.ggpoutil_perfmon_init();
            SetStatusText("Connecting to peers.");
        }

        public abstract string GetName();

        /*
         * Create a new spectator session
         */

        public void InitSpectator(int localport, int num_players, string host_ip, int host_port) {
            // Initialize the game state
            gs.Init(num_players);
            ngs.players = Array.Empty<PlayerConnectionInfo>();

            // Fill in a ggpo callbacks structure to pass to start_session.
            var result = GGPO.Session.StartSpectating(
                    OnBeginGameCallback,
                    OnAdvanceFrameCallback,
                    OnLoadGameStateCallback,
                    OnLogGameState,
                    OnSaveGameStateCallback,
                    OnFreeBufferCallback,
                    OnEventConnectedToPeerDelegate,
                    OnEventSynchronizingWithPeerDelegate,
                    OnEventSynchronizedWithPeerDelegate,
                    OnEventRunningDelegate,
                    OnEventConnectionInterruptedDelegate,
                    OnEventConnectionResumedDelegate,
                    OnEventDisconnectedFromPeerDelegate,
                    OnEventEventcodeTimesyncDelegate,
                    GetName(), num_players, localport, host_ip, host_port);

            CheckAndReport(result);

            perf?.ggpoutil_perfmon_init();

            SetStatusText("Starting new spectator session");
        }

        /*
         * Disconnects a player from this session.
         */

        public void DisconnectPlayer(int playerIndex) {
            if (playerIndex < ngs.players.Length) {
                string logbuf;
                var result = GGPO.Session.DisconnectPlayer(ngs.players[playerIndex].handle);
                if (GGPO.SUCCEEDED(result)) {
                    logbuf = $"Disconnected player {playerIndex}.";
                }
                else {
                    logbuf = $"Error while disconnecting player (err:{result}).";
                }
                SetStatusText(logbuf);
            }
        }

        /*
         * Advances the game state by exactly 1 frame using the inputs specified
         * for player 1 and player 2.
         */

        private void AdvanceFrame(ulong[] inputs, int disconnect_flags) {
            gs.Update(inputs, disconnect_flags);

            // update the checksums to display in the top of the window. this helps to detect desyncs.
            ngs.now.framenumber = gs._framenumber;
            // var buffer = GameState.ToBytes(gs);
            ngs.now.checksum = 0; // naive_fletcher32_per_byte(buffer);
            if ((gs._framenumber % 90) == 0) {
                ngs.periodic = ngs.now;
            }

            // Notify ggpo that we've moved forward exactly 1 frame.
            CheckAndReport(GGPO.Session.AdvanceFrame());

            // Update the performance monitor display.
            int[] handles = new int[MAX_PLAYERS];
            int count = 0;
            for (int i = 0; i < ngs.players.Length; i++) {
                if (ngs.players[i].type == GGPOPlayerType.GGPO_PLAYERTYPE_REMOTE) {
                    handles[count++] = ngs.players[i].handle;
                }
            }
            perf?.ggpoutil_perfmon_update(GGPO.Session.ggpo, handles, count);
        }

        /*
        * Run a single frame of the game.
        */

        public void RunFrame() {
            var result = GGPO.OK;

            for (int i = 0; i < ngs.players.Length; ++i) {
                var player = ngs.players[i];
                if (player.type == GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL) {
                    ulong input = gs.ReadInputs(player.controllerId);
#if SYNC_TEST
     input = rand(); // test: use random inputs to demonstrate sync testing
#endif
                    result = GGPO.Session.AddLocalInput(player.handle, input);
                }
            }

            // synchronize these inputs with ggpo. If we have enough input to proceed ggpo will
            // modify the input list with the correct inputs to use and return 1.
            if (GGPO.SUCCEEDED(result)) {
                frameWatch.Start();
                ulong[] inputs = new ulong[MAX_PLAYERS];
                result = GGPO.Session.SynchronizeInput(inputs, MAX_PLAYERS, out var disconnect_flags);
                if (GGPO.SUCCEEDED(result)) {
                    // inputs[0] and inputs[1] contain the inputs for p1 and p2. Advance the game by
                    // 1 frame using those inputs.
                    AdvanceFrame(inputs, disconnect_flags);
                }
                else {
                    OnLog?.Invoke("Error inputsync");
                }
                frameWatch.Stop();
            }
        }

        /*
         * Spend our idle time in ggpo so it can use whatever time we have left over
         * for its internal bookkeeping.
         */

        public void Idle(int time) {
            idleWatch.Start();
            CheckAndReport(GGPO.Session.Idle(time));
            idleWatch.Stop();
        }

        public void Exit() {
            if (GGPO.Session.IsStarted()) {
                CheckAndReport(GGPO.Session.CloseSession());
            }
        }

        private void SetStatusText(string status) {
            ngs.status = status;
        }

        public void Init(IGameState _gs, GameInfo _ngs, GGPOPerformance _perf) {
            gs = _gs;
            ngs = _ngs;
            perf = _perf;
        }

        private void CheckAndReport(int result) {
            if (!GGPO.SUCCEEDED(result)) {
                OnLog?.Invoke(GGPO.GetErrorCodeMessage(result));
            }
        }

        public string GetStatus(Stopwatch updateWatch) {
            var idlePerc = (float)idleWatch.ElapsedMilliseconds / (float)updateWatch.ElapsedMilliseconds;
            var updatePerc = (float)frameWatch.ElapsedMilliseconds / (float)updateWatch.ElapsedMilliseconds;
            var otherPerc = 1f - (idlePerc + updatePerc);
            return string.Format("idle:{0:.00} update{1:.00} other{2:.00}", idlePerc, updatePerc, otherPerc);
        }

        public void Shutdown() {
            Exit();
            GGPO.SetLogDelegate(null);
        }

        public static void Log(string value) {
            OnLog?.Invoke(value);
        }

        public void Init() {
            GGPO.SetLogDelegate(GameRunner.LogCallback);
            Init(CreateGameState(), new GameInfo(), GameObject.FindObjectOfType<GGPOPerformance>());

            var remote_index = -1;
            var num_spectators = 0;
            var num_players = 0;

            for (int i = 0; i < connections.Count; ++i) {
                if (i != PlayerIndex && remote_index == -1) {
                    remote_index = i;
                }

                if (connections[i].spectator) {
                    ++num_spectators;
                }
                else {
                    ++num_players;
                }
            }
            if (connections[PlayerIndex].spectator) {
                InitSpectator(connections[PlayerIndex].port, num_players, connections[remote_index].ip, connections[remote_index].port);
            }
            else {
                var players = new List<GGPOPlayer>();
                for (int i = 0; i < connections.Count; ++i) {
                    var player = new GGPOPlayer {
                        player_num = players.Count + 1,
                    };
                    if (PlayerIndex == i) {
                        player.type = GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL;
                        player.ip_address = "";
                        player.port = 0;
                    }
                    else if (connections[i].spectator) {
                        player.type = GGPOPlayerType.GGPO_PLAYERTYPE_SPECTATOR;
                        player.ip_address = connections[remote_index].ip;
                        player.port = connections[remote_index].port;
                    }
                    else {
                        player.type = GGPOPlayerType.GGPO_PLAYERTYPE_REMOTE;
                        player.ip_address = connections[remote_index].ip;
                        player.port = connections[remote_index].port;
                    }
                    players.Add(player);
                }
                Init(connections[PlayerIndex].port, num_players, players, num_spectators);
            }
        }
    }
}