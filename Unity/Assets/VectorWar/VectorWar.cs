using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

namespace VectorWar {
    using static VWConstants;

    public static class VectorWar {

        //#define SYNC_TEST    // test: turn on synctest
        const int FRAME_DELAY = 2;

        public static GameState gs = null;
        public static NonGameState ngs = null;
        public static GGPOPerformance perf = null;
        static readonly Dictionary<long, NativeArray<byte>> cache = new Dictionary<long, NativeArray<byte>>();
        static IntPtr ggpo = IntPtr.Zero;

        static IntPtr vw_begin_game_callback;

        static IntPtr vw_advance_frame_callback;
        static IntPtr vw_load_game_state_callback;
        static IntPtr vw_log_game_state;
        static IntPtr vw_save_game_state_callback;
        static IntPtr vw_free_buffer_callback;
        static IntPtr vw_on_event_callback;

        public static event Action<string> OnLog = (string s) => { };

        /*
         * vw_begin_game_callback --
         *
         * The begin game callback.  We don't need to do anything special here,
         * so just return true.
         */

        static bool Vw_begin_game_callback(string name) {
            OnLog($"vw_begin_game_callback");
            return true;
        }

        /*
         * vw_on_event_callback --
         *
         * Notification from GGPO that something has happened.  Update the status
         * text at the bottom of the screen to notify the user.
         */

        static bool Vw_on_event_callback(IntPtr evtPtr) {
            Debug.Assert(gs != null && ngs != null);
            int[] data = new int[4];
            Marshal.Copy(evtPtr, data, 0, 4);
            int info_code = data[0];
            int connected_player = data[1];
            int synchronizing_player = data[1];
            int synchronizing_count = data[2];
            int synchronizing_total = data[3];
            int synchronized_player = data[1];
            int disconnected_player = data[1];
            int timesync_frames_ahead = data[1];
            int connection_interrupted_player = data[1];
            int connection_interrupted_disconnect_timeout = data[2];
            int connection_resumed_player = data[1];

            OnLog($"vw_on_event_callback {data[0]} {data[1]} {data[2]} {data[3]}");

            int progress;
            switch (info_code) {
                case GGPO.EVENTCODE_CONNECTED_TO_PEER:
                    ngs.SetConnectState(connected_player, PlayerConnectState.Synchronizing);
                    break;

                case GGPO.EVENTCODE_SYNCHRONIZING_WITH_PEER:
                    progress = 100 * synchronizing_count / synchronizing_total;
                    ngs.UpdateConnectProgress(synchronizing_player, progress);
                    break;

                case GGPO.EVENTCODE_SYNCHRONIZED_WITH_PEER:
                    ngs.UpdateConnectProgress(synchronized_player, 100);
                    break;

                case GGPO.EVENTCODE_RUNNING:
                    ngs.SetConnectState(PlayerConnectState.Running);
                    SetStatusText("");
                    break;

                case GGPO.EVENTCODE_CONNECTION_INTERRUPTED:
                    ngs.SetDisconnectTimeout(connection_interrupted_player,
                                             Helper.TimeGetTime(),
                                             connection_interrupted_disconnect_timeout);
                    break;

                case GGPO.EVENTCODE_CONNECTION_RESUMED:
                    ngs.SetConnectState(connection_resumed_player, PlayerConnectState.Running);
                    break;

                case GGPO.EVENTCODE_DISCONNECTED_FROM_PEER:
                    ngs.SetConnectState(disconnected_player, PlayerConnectState.Disconnected);
                    break;

                case GGPO.EVENTCODE_TIMESYNC:
                    Helper.Sleep(1000 * timesync_frames_ahead / 60);
                    break;
            }
            return true;
        }

        /*
         * vw_advance_frame_callback --
         *
         * Notification from GGPO we should step foward exactly 1 frame
         * during a rollback.
         */

        static bool Vw_advance_frame_callback(int flags) {
            OnLog($"vw_begin_game_callback {flags}");

            // Make sure we fetch new inputs from GGPO and use those to update the game state
            // instead of reading from the keyboard.
            ulong[] inputs = new ulong[MAX_SHIPS];
            ReportFailure(GGPO.SynchronizeInput(ggpo, inputs, MAX_SHIPS, out var disconnect_flags));

            AdvanceFrame(inputs, disconnect_flags);
            return true;
        }

        /*
         * vw_load_game_state_callback --
         *
         * Makes our current state match the state passed in by GGPO.
         */

        static unsafe bool Vw_load_game_state_callback(void* dataPtr, int length) {
            OnLog($"vw_load_game_state_callback {length}");
            gs = GameState.FromBytes(Helper.ToArray(dataPtr, length));
            return true;
        }

        /*
         * vw_save_game_state_callback --
         *
         * Save the current state to a buffer and return it to GGPO via the
         * buffer and len parameters.
         */

        static private unsafe bool Vw_save_game_state_callback(void** buffer, int* length, int* checksum, int frame) {
            OnLog($"vw_save_game_state_callback {frame}");
            Debug.Assert(gs != null);
            var bytes = GameState.ToBytes(gs);
            *checksum = Helper.CalcFletcher32(bytes);
            *length = bytes.Length;
            var ptr = Helper.ToPtr(bytes);
            *buffer = ptr;
            cache[(long)ptr] = bytes;
            return true;
        }

        /*
         * vw_log_game_state --
         *
         * Log the gamestate.  Used by the synctest debugging tool.
         */

        static unsafe bool Vw_log_game_state(string text, void* buffer, int length) {
            OnLog($"vw_log_game_state {text}");

            var gamestate = GameState.FromBytes(Helper.ToArray(buffer, length));
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
                fp += string.Format("  ship {0} speed:     %d.\n", i, ship.speed);
                fp += string.Format("  ship {0} cooldown:  %d.\n", i, ship.cooldown);
                fp += string.Format("  ship {0} score:     {1}.\n", i, ship.score);
                for (int j = 0; j < ship.bullets.Length; j++) {
                    fp += string.Format("  ship {0} bullet {1}: {2} {3} -> {4} {5}.\n", i, j,
                            ship.bullets[j].position.x, ship.bullets[j].position.y,
                            ship.bullets[j].velocity.x, ship.bullets[j].velocity.y);
                }
            }
            return true;
        }

        static unsafe void Vw_free_buffer_callback(void* dataPtr) {
            OnLog($"vw_free_buffer_callback");

            if (cache.TryGetValue((long)dataPtr, out var data)) {
                data.Dispose();
                cache.Remove((long)dataPtr);
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
            SetFuncPointers();

            var result = GGPO.StartSession(out ggpo,
                                vw_begin_game_callback,
                                vw_advance_frame_callback,
                                vw_load_game_state_callback,
                                vw_log_game_state,
                                vw_save_game_state_callback,
                                vw_free_buffer_callback,
                                vw_on_event_callback,
                                "vectorwar", num_players, localport);

            if (ggpo == IntPtr.Zero) {
                OnLog("Session Error");
            }

#endif
            ReportFailure(result);

            // automatically disconnect clients after 3000 ms and start our count-down timer for
            // disconnects after 1000 ms. To completely disable disconnects, simply use a value of 0
            // for ggpo_set_disconnect_timeout.
            ReportFailure(GGPO.SetDisconnectTimeout(ggpo, 3000));
            ReportFailure(GGPO.SetDisconnectNotifyStart(ggpo, 1000));

            int controllerId = 0;
            int playerIndex = 0;
            ngs.players = new PlayerConnectionInfo[num_players];
            for (int i = 0; i < players.Count; i++) {
                ReportFailure(GGPO.AddPlayer(ggpo,
                    (int)players[i].type,
                    players[i].player_num,
                    players[i].ip_address,
                    players[i].port,
                    out int handle));

                if (players[i].type == GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL) {
                    var playerInfo = new PlayerConnectionInfo();
                    playerInfo.handle = handle;
                    playerInfo.type = players[i].type;
                    playerInfo.connect_progress = 100;
                    playerInfo.controllerId = controllerId++;
                    ngs.players[playerIndex++] = playerInfo;
                    ngs.SetConnectState(handle, PlayerConnectState.Connecting);
                    ReportFailure(GGPO.SetFrameDelay(ggpo, handle, FRAME_DELAY));
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

        static unsafe void SetFuncPointers() {
            vw_begin_game_callback = Marshal.GetFunctionPointerForDelegate<GGPO.BeginGameDelegate>(Vw_begin_game_callback);
            vw_advance_frame_callback = Marshal.GetFunctionPointerForDelegate<GGPO.AdvanceFrameDelegate>(Vw_advance_frame_callback);
            vw_load_game_state_callback = Marshal.GetFunctionPointerForDelegate<GGPO.LoadGameStateDelegate>(Vw_load_game_state_callback);
            vw_log_game_state = Marshal.GetFunctionPointerForDelegate<GGPO.LogGameStateDelegate>(Vw_log_game_state);
            vw_save_game_state_callback = Marshal.GetFunctionPointerForDelegate<GGPO.SaveGameStateDelegate>(Vw_save_game_state_callback);
            vw_free_buffer_callback = Marshal.GetFunctionPointerForDelegate<GGPO.FreeBufferDelegate>(Vw_free_buffer_callback);
            vw_on_event_callback = Marshal.GetFunctionPointerForDelegate<GGPO.OnEventDelegate>(Vw_on_event_callback);
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
            SetFuncPointers();
            var result = GGPO.StartSpectating(out ggpo,
                    vw_begin_game_callback,
                    vw_advance_frame_callback,
                    vw_load_game_state_callback,
                    vw_log_game_state,
                    vw_save_game_state_callback,
                    vw_free_buffer_callback,
                    vw_on_event_callback,
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
                var result = GGPO.DisconnectPlayer(ggpo, ngs.players[playerIndex].handle);
                if (GGPO.SUCCEEDED(result)) {
                    logbuf = $"Disconnected player {playerIndex}.\n";
                }
                else {
                    logbuf = $"Error while disconnecting player (err:{result}).\n";
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
            ReportFailure(GGPO.AdvanceFrame(ggpo));

            // Update the performance monitor display.
            int[] handles = new int[MAX_PLAYERS];
            int count = 0;
            for (int i = 0; i < ngs.players.Length; i++) {
                if (ngs.players[i].type == GGPOPlayerType.GGPO_PLAYERTYPE_REMOTE) {
                    handles[count++] = ngs.players[i].handle;
                }
            }
            perf.ggpoutil_perfmon_update(ggpo, handles, count);
        }

        /*
         * ReadInputs --
         *
         * Read the inputs for player 1 from the keyboard.  We never have to
         * worry about player 2.  GGPO will handle remapping his inputs
         * transparently.
         */

        static public ulong ReadInputs(int id) {
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
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.D)) {
                    input |= INPUT_FIRE;
                }
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.S)) {
                    input |= INPUT_BOMB;
                }
            }
            else if (id == 1) {
                return INPUT_THRUST | INPUT_ROTATE_LEFT | INPUT_FIRE;
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

                    result = GGPO.AddLocalInput(ggpo, player.handle, input);
                }
            }

            // synchronize these inputs with ggpo. If we have enough input to proceed ggpo will
            // modify the input list with the correct inputs to use and return 1.
            if (GGPO.SUCCEEDED(result)) {
                ulong[] inputs = new ulong[MAX_SHIPS];
                result = GGPO.SynchronizeInput(ggpo, inputs, MAX_SHIPS, out var disconnect_flags);
                if (GGPO.SUCCEEDED(result)) {
                    // inputs[0] and inputs[1] contain the inputs for p1 and p2. Advance the game by
                    // 1 frame using those inputs.
                    AdvanceFrame(inputs, disconnect_flags);
                }
                else {
                    OnLog("Error inputsync");
                }
            }
        }

        /*
         * Idle --
         *
         * Spend our idle time in ggpo so it can use whatever time we have left over
         * for its internal bookkeeping.
         */

        public static void Idle(int time) {
            ReportFailure(GGPO.Idle(ggpo, time));
        }

        public static void Exit() {
            if (ggpo != IntPtr.Zero) {
                ReportFailure(GGPO.CloseSession(ggpo));
                ggpo = IntPtr.Zero;
            }

            foreach (var c in cache.Values) {
                if (c.IsCreated) {
                    c.Dispose();
                }
            }
            cache.Clear();
        }

        static void SetStatusText(string status) {
            ngs.status = status;
        }

        public static void Init(GameState gs, NonGameState ngs, GGPOPerformance perf) {
            VectorWar.gs = gs;
            VectorWar.ngs = ngs;
            VectorWar.perf = perf;
        }

        static void ReportFailure(int result) {
            if (!GGPO.SUCCEEDED(result)) {
                OnLog(GGPO.GetErrorCodeMessage(result));
            }
        }
    }
}
