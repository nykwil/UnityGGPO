using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Collections;

namespace VectorWar {
    using static VWConstants;

    public static class VectorWar {
        const int FRAME_DELAY = 2;

        public static GameState gs = null;
        public static NonGameState ngs = null;
        public static GGPOPerformance perf = null;

        public static event Action<string> OnLog;

        public static Stopwatch frameWatch = new Stopwatch();
        public static Stopwatch idleWatch = new Stopwatch();

        /*
         * vw_begin_game_callback --
         *
         * The begin game callback.  We don't need to do anything special here,
         * so just return true.
         */

        static bool Vw_begin_game_callback(string name) {
            OnLog?.Invoke($"vw_begin_game_callback");
            return true;
        }

        /*
         * vw_on_event_callback --
         *
         * Notification from GGPO that something has happened.  Update the status
         * text at the bottom of the screen to notify the user.
         */

        static bool OnEventConnectedToPeerDelegate(int connected_player)
        {
            ngs.SetConnectState(connected_player, PlayerConnectState.Synchronizing);
            return true;
        }

        static public bool OnEventSynchronizingWithPeerDelegate(int synchronizing_player, int synchronizing_count, int synchronizing_total)
        {
            var progress = 100 * synchronizing_count / synchronizing_total;
            ngs.UpdateConnectProgress(synchronizing_player, progress);
            return true;
        }

        static public bool OnEventSynchronizedWithPeerDelegate(int synchronized_player)
        {
            ngs.UpdateConnectProgress(synchronized_player, 100);
            return true;
        }

        static public bool OnEventRunningDelegate()
        {
            ngs.SetConnectState(PlayerConnectState.Running);
            SetStatusText("");
            return true;
        }

        static public bool OnEventConnectionInterruptedDelegate(int connection_interrupted_player, int connection_interrupted_disconnect_timeout)
        {
            ngs.SetDisconnectTimeout(connection_interrupted_player,
                                     Helper.TimeGetTime(),
                                     connection_interrupted_disconnect_timeout);
            return true;
        }


        static public bool OnEventConnectionResumedDelegate(int connection_resumed_player)
        {
            ngs.SetConnectState(connection_resumed_player, PlayerConnectState.Running);
            return true;
        }


        static public bool OnEventDisconnectedFromPeerDelegate(int disconnected_player)
        {
            ngs.SetConnectState(disconnected_player, PlayerConnectState.Disconnected);
            return true;
        }


        static public bool OnEventEventcodeTimesyncDelegate(int timesync_frames_ahead)
        {
            Helper.Sleep(1000 * timesync_frames_ahead / 60);
            return true;
        }

        /*
         * vw_advance_frame_callback --
         *
         * Notification from GGPO we should step foward exactly 1 frame
         * during a rollback.
         */

        static bool Vw_advance_frame_callback(int flags) {
            OnLog?.Invoke($"vw_begin_game_callback {flags}");

            // Make sure we fetch new inputs from GGPO and use those to update the game state
            // instead of reading from the keyboard.
            ulong[] inputs = new ulong[MAX_SHIPS];
            ReportFailure(GGPO.Session.SynchronizeInput(inputs, MAX_SHIPS, out var disconnect_flags));

            AdvanceFrame(inputs, disconnect_flags);
            return true;
        }

        /*
         * vw_load_game_state_callback --
         *
         * Makes our current state match the state passed in by GGPO.
         */

        static bool Vw_load_game_state_callback(NativeArray<byte> data) {
            OnLog?.Invoke($"vw_load_game_state_callback {data.Length}");
            GameState.FromBytes(gs, data);
            return true;
        }

        /*
         * vw_save_game_state_callback --
         *
         * Save the current state to a buffer and return it to GGPO via the
         * buffer and len parameters.
         */

        static bool Vw_save_game_state_callback(out NativeArray<byte> data, out int checksum, int frame) {
            OnLog?.Invoke($"vw_save_game_state_callback {frame}");
            Debug.Assert(gs != null);
            data = GameState.ToBytes(gs);
            checksum = Helper.CalcFletcher32(data);
            return true;
        }

        /*
         * vw_log_game_state --
         *
         * Log the gamestate.  Used by the synctest debugging tool.
         */

        static bool Vw_log_game_state(string filename, NativeArray<byte> data) {
            OnLog?.Invoke($"vw_log_game_state {filename}");

            var gamestate = new GameState();
            GameState.FromBytes(gamestate, data);
            string fp = "";
            fp += "GameState object.\n";
            fp += string.Format("  bounds: {0},{1} x {2},{3}.\n", gamestate._bounds.xMin, gamestate._bounds.yMin,
                    gamestate._bounds.xMax, gamestate._bounds.yMax);
            fp += string.Format("  num_ships: {0}.\n", gamestate._ships.Length);
            for (int i = 0; i < gamestate._ships.Length; i++) {
                var ship = gamestate._ships[i];
                fp += string.Format("  ship {0} position:  %.4f, %.4f\n", i, ship.position.x, ship.position.y);
                fp += string.Format("  ship {0} velocity:  %.4f, %.4f\n", i, ship.velocity.x, ship.velocity.y);
                fp += string.Format("  ship {0} radius:    %d.\n", i, ship.radius);
                fp += string.Format("  ship {0} heading:   %d.\n", i, ship.heading);
                fp += string.Format("  ship {0} health:    %d.\n", i, ship.health);
                fp += string.Format("  ship {0} cooldown:  %d.\n", i, ship.cooldown);
                fp += string.Format("  ship {0} score:     {1}.\n", i, ship.score);
                for (int j = 0; j < ship.bullets.Length; j++) {
                    fp += string.Format("  ship {0} bullet {1}: {2} {3} -> {4} {5}.\n", i, j,
                            ship.bullets[j].position.x, ship.bullets[j].position.y,
                            ship.bullets[j].velocity.x, ship.bullets[j].velocity.y);
                }
            }
            File.WriteAllText(filename, fp);
            return true;
        }

        static void Vw_free_buffer_callback(NativeArray<byte> data) {
            OnLog?.Invoke($"vw_free_buffer_callback");
            if (data.IsCreated)
            {
                data.Dispose();
            }
        }

        /*
         * Init --
         *
         * Initialize the vector war game.  This initializes the game state and
         * the video renderer and creates a new network session.
         */

        public static void Init(int localport, int num_players, IList<GGPOPlayer> players, int num_spectators) {
            // Initialize the game state
            gs.Init(num_players);

#if SYNC_TEST
            var result = ggpo_start_synctest(cb, "vectorwar", num_players, 1);
#else
            var result = GGPO.Session.StartSession(Vw_begin_game_callback,
                    Vw_advance_frame_callback,
                    Vw_load_game_state_callback,
                    Vw_log_game_state,
                    Vw_save_game_state_callback,
                    Vw_free_buffer_callback,
                    OnEventConnectedToPeerDelegate,
                    OnEventSynchronizingWithPeerDelegate,
                    OnEventSynchronizedWithPeerDelegate,
                    OnEventRunningDelegate,
                    OnEventConnectionInterruptedDelegate,
                    OnEventConnectionResumedDelegate,
                    OnEventDisconnectedFromPeerDelegate,
                    OnEventEventcodeTimesyncDelegate,
                    "vectorwar", num_players, localport);

#endif
            ReportFailure(result);

            // automatically disconnect clients after 3000 ms and start our count-down timer for
            // disconnects after 1000 ms. To completely disable disconnects, simply use a value of 0
            // for ggpo_set_disconnect_timeout.
            ReportFailure(GGPO.Session.SetDisconnectTimeout(3000));
            ReportFailure(GGPO.Session.SetDisconnectNotifyStart(1000));

            int controllerId = 0;
            int playerIndex = 0;
            ngs.players = new PlayerConnectionInfo[num_players];
            for (int i = 0; i < players.Count; i++) {
                ReportFailure(GGPO.Session.AddPlayer(players[i], out int handle));

                if (players[i].type == GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL) {
                    var playerInfo = new PlayerConnectionInfo();
                    playerInfo.handle = handle;
                    playerInfo.type = players[i].type;
                    playerInfo.connect_progress = 100;
                    playerInfo.controllerId = controllerId++;
                    ngs.players[playerIndex++] = playerInfo;
                    ngs.SetConnectState(handle, PlayerConnectState.Connecting);
                    ReportFailure(GGPO.Session.SetFrameDelay(handle, FRAME_DELAY));
                }
                else if (players[i].type == GGPOPlayerType.GGPO_PLAYERTYPE_REMOTE) {
                    var playerInfo = new PlayerConnectionInfo();
                    playerInfo.handle = handle;
                    playerInfo.type = players[i].type;
                    playerInfo.connect_progress = 0;
                    ngs.players[playerIndex++] = playerInfo;
                }
            }

            perf.ggpoutil_perfmon_init();
            SetStatusText("Connecting to peers.");
        }

        /*
         * InitSpectator --
         *
         * Create a new spectator session
         */

        public static void InitSpectator(int localport, int num_players, string host_ip, int host_port) {
            // Initialize the game state
            gs.Init(num_players);
            ngs.players = Array.Empty<PlayerConnectionInfo>();

            // Fill in a ggpo callbacks structure to pass to start_session.
            var result = GGPO.Session.StartSpectating(
                    Vw_begin_game_callback,
                    Vw_advance_frame_callback,
                    Vw_load_game_state_callback,
                    Vw_log_game_state,
                    Vw_save_game_state_callback,
                    Vw_free_buffer_callback,
                    OnEventConnectedToPeerDelegate,
                    OnEventSynchronizingWithPeerDelegate,
                    OnEventSynchronizedWithPeerDelegate,
                    OnEventRunningDelegate,
                    OnEventConnectionInterruptedDelegate,
                    OnEventConnectionResumedDelegate,
                    OnEventDisconnectedFromPeerDelegate,
                    OnEventEventcodeTimesyncDelegate,
                    "vectorwar", num_players, localport, host_ip, host_port);

            ReportFailure(result);

            perf.ggpoutil_perfmon_init();

            SetStatusText("Starting new spectator session");
        }

        /*
         * DisconnectPlayer --
         *
         * Disconnects a player from this session.
         */

        public static void DisconnectPlayer(int playerIndex) {
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
         * AdvanceFrame --
         *
         * Advances the game state by exactly 1 frame using the inputs specified
         * for player 1 and player 2.
         */

        static void AdvanceFrame(ulong[] inputs, int disconnect_flags) {
            gs.Update(inputs, disconnect_flags);

            // update the checksums to display in the top of the window. this helps to detect desyncs.
            ngs.now.framenumber = gs._framenumber;
            // var buffer = GameState.ToBytes(gs);
            ngs.now.checksum = 0; // naive_fletcher32_per_byte(buffer);
            if ((gs._framenumber % 90) == 0) {
                ngs.periodic = ngs.now;
            }

            // Notify ggpo that we've moved forward exactly 1 frame.
            ReportFailure(GGPO.Session.AdvanceFrame());

            // Update the performance monitor display.
            int[] handles = new int[MAX_PLAYERS];
            int count = 0;
            for (int i = 0; i < ngs.players.Length; i++) {
                if (ngs.players[i].type == GGPOPlayerType.GGPO_PLAYERTYPE_REMOTE) {
                    handles[count++] = ngs.players[i].handle;
                }
            }
            perf.ggpoutil_perfmon_update(GGPO.Session.ggpo, handles, count);
        }

        /*
         * ReadInputs --
         *
         * Read the inputs for player 1 from the keyboard.  We never have to
         * worry about player 2.  GGPO will handle remapping his inputs
         * transparently.
         */

        public static ulong ReadInputs(int id) {
            ulong input = 0;

            if (id == 0) {
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.UpArrow)) {
                    input |= INPUT_THRUST;
                }
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.DownArrow)) {
                    input |= INPUT_BREAK;
                }
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftArrow)) {
                    input |= INPUT_ROTATE_LEFT;
                }
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightArrow)) {
                    input |= INPUT_ROTATE_RIGHT;
                }
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightControl)) {
                    input |= INPUT_FIRE;
                }
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightShift)) {
                    input |= INPUT_BOMB;
                }
            }
            else if (id == 1) {
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.W)) {
                    input |= INPUT_THRUST;
                }
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.S)) {
                    input |= INPUT_BREAK;
                }
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.A)) {
                    input |= INPUT_ROTATE_LEFT;
                }
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.D)) {
                    input |= INPUT_ROTATE_RIGHT;
                }
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.F)) {
                    input |= INPUT_FIRE;
                }
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.G)) {
                    input |= INPUT_BOMB;
                }
            }

            return input;
        }

        /*
         * RunFrame --
         *
         * Run a single frame of the game.
         */

        public static void RunFrame() {
            var result = GGPO.OK;

            for (int i = 0; i < ngs.players.Length; ++i) {
                var player = ngs.players[i];
                if (player.type == GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL) {
                    ulong input = ReadInputs(player.controllerId);
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
                ulong[] inputs = new ulong[MAX_SHIPS];
                result = GGPO.Session.SynchronizeInput(inputs, MAX_SHIPS, out var disconnect_flags);
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
         * Idle --
         *
         * Spend our idle time in ggpo so it can use whatever time we have left over
         * for its internal bookkeeping.
         */

        public static void Idle(int time) {
            idleWatch.Start();
            ReportFailure(GGPO.Session.Idle(time));
            idleWatch.Stop();
        }

        public static void Exit() {
            if (GGPO.Session.IsStarted()) {
                ReportFailure(GGPO.Session.CloseSession());
            }
        }

        static void SetStatusText(string status) {
            ngs.status = status;
        }

        public static void Init(GameState _gs, NonGameState _ngs, GGPOPerformance _perf) {
            gs = _gs;
            ngs = _ngs;
            perf = _perf;
        }

        static void ReportFailure(int result) {
            if (!GGPO.SUCCEEDED(result)) {
                OnLog?.Invoke(GGPO.GetErrorCodeMessage(result));
            }
        }
    }
}
