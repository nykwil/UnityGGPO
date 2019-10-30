using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;

namespace VectorWar {
    using static VWConstants;

    public class VectorWar {

        //#define SYNC_TEST    // test: turn on synctest
        const int FRAME_DELAY = 2;

        GameState gs = null;
        NonGameState ngs = null;
        GGPOPerformance perf = null;
        Dictionary<long, NativeArray<byte>> cache = new Dictionary<long, NativeArray<byte>>();
        IntPtr ggpo;

        public VectorWar(GameState gs, NonGameState ngs, GGPOPerformance perf) {
            this.gs = gs;
            this.ngs = ngs;
            this.perf = perf;
        }

        internal void InitSpectator(object localport, int length, object host_ip, object host_port) {
        }

        /*
         * Simple checksum function stolen from wikipedia:
         *
         *   http://en.wikipedia.org/wiki/Fletcher%27s_checksum
         */

        int fletcher32_checksum(NativeArray<byte> data, int len) {
            int sum1 = 0xffff, sum2 = 0xffff;
            int i = 0;

            while (len > 0) {
                int tlen = len > 360 ? 360 : len;
                len -= tlen;
                do {
                    sum1 += data[i++];
                    sum2 += sum1;
                } while (--tlen > 0);
                sum1 = (sum1 & 0xffff) + (sum1 >> 16);
                sum2 = (sum2 & 0xffff) + (sum2 >> 16);
            }

            /* Second reduction step to reduce sums to 16 bits */
            sum1 = (sum1 & 0xffff) + (sum1 >> 16);
            sum2 = (sum2 & 0xffff) + (sum2 >> 16);
            return sum2 << 16 | sum1;
        }

        /*
         * vw_begin_game_callback --
         *
         * The begin game callback.  We don't need to do anything special here,
         * so just return true.
         */

        bool vw_begin_game_callback(string name) {
            return true;
        }

        /*
         * vw_on_event_callback --
         *
         * Notification from GGPO that something has happened.  Update the status
         * text at the bottom of the screen to notify the user.
         */

        bool vw_on_event_callback(IntPtr evtPtr) {
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

            int progress;
            switch (info_code) {
                case GGPOC.GGPO_EVENTCODE_CONNECTED_TO_PEER:
                    ngs.SetConnectState(connected_player, PlayerConnectState.Synchronizing);
                    break;

                case GGPOC.GGPO_EVENTCODE_SYNCHRONIZING_WITH_PEER:
                    progress = 100 * synchronizing_count / synchronizing_total;
                    ngs.UpdateConnectProgress(synchronizing_player, progress);
                    break;

                case GGPOC.GGPO_EVENTCODE_SYNCHRONIZED_WITH_PEER:
                    ngs.UpdateConnectProgress(synchronized_player, 100);
                    break;

                case GGPOC.GGPO_EVENTCODE_RUNNING:
                    ngs.SetConnectState(PlayerConnectState.Running);
                    SetStatusText("");
                    break;

                case GGPOC.GGPO_EVENTCODE_CONNECTION_INTERRUPTED:
                    ngs.SetDisconnectTimeout(connection_interrupted_player,
                                             Helper.TimeGetTime(),
                                             connection_interrupted_disconnect_timeout);
                    break;

                case GGPOC.GGPO_EVENTCODE_CONNECTION_RESUMED:
                    ngs.SetConnectState(connection_resumed_player, PlayerConnectState.Running);
                    break;

                case GGPOC.GGPO_EVENTCODE_DISCONNECTED_FROM_PEER:
                    ngs.SetConnectState(disconnected_player, PlayerConnectState.Disconnected);
                    break;

                case GGPOC.GGPO_EVENTCODE_TIMESYNC:
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

        bool vw_advance_frame_callback(int flags) {
            // Make sure we fetch new inputs from GGPO and use those to update the game state instead
            // of reading from the keyboard.
            ulong[] inputs = new ulong[MAX_SHIPS];
            UGGPO.UggSynchronizeInput(ggpo, inputs, MAX_SHIPS, out var disconnect_flags);

            AdvanceFrame(inputs, disconnect_flags);
            return true;
        }

        /*
         * vw_load_game_state_callback --
         *
         * Makes our current state match the state passed in by GGPO.
         */

        unsafe bool vw_load_game_state_callback(void* dataPtr, int length) {
            gs = GameState.FromBytes(Helper.ToArray(dataPtr, length));
            return true;
        }

        /*
         * vw_save_game_state_callback --
         *
         * Save the current state to a buffer and return it to GGPO via the
         * buffer and len parameters.
         */

        private unsafe bool vw_save_game_state_callback(void** buffer, int* len, int* checksum, int frame) {
            throw new NotImplementedException();
        }

        unsafe void* vw_save_game_state_callbackOld(out int length, out int checksum, int frame) {
            var buffer = GameState.ToBytes(gs);
            checksum = 0;
            length = buffer.Length;
            var ptr = Helper.ToPtr(buffer);
            cache[(long)ptr] = buffer;
            return ptr;
        }

        /*
         * vw_log_game_state --
         *
         * Log the gamestate.  Used by the synctest debugging tool.
         */

        unsafe bool vw_log_game_state(string text, void* buffer, int length) {
            /*
            FILE* fp = fopen(filename, "w");
            if (fp) {
                GameState* gamestate = (GameState*)buffer;
                fprintf(fp, "GameState object.\n");
                fprintf(fp, "  bounds: %d,%d x %d,%d.\n", gamestate->_bounds.left, gamestate->_bounds.top,
                        gamestate->_bounds.right, gamestate->_bounds.bottom);
                fprintf(fp, "  num_ships: %d.\n", gamestate->_num_ships);
                for (int i = 0; i < gamestate->_num_ships; i++) {
                    Ship* ship = gamestate->_ships + i;
                    fprintf(fp, "  ship %d position:  %.4f, %.4f\n", i, ship->position.x, ship->position.y);
                    fprintf(fp, "  ship %d velocity:  %.4f, %.4f\n", i, ship->velocity.dx, ship->velocity.dy);
                    fprintf(fp, "  ship %d radius:    %d.\n", i, ship->radius);
                    fprintf(fp, "  ship %d heading:   %d.\n", i, ship->heading);
                    fprintf(fp, "  ship %d health:    %d.\n", i, ship->health);
                    fprintf(fp, "  ship %d speed:     %d.\n", i, ship->speed);
                    fprintf(fp, "  ship %d cooldown:  %d.\n", i, ship->cooldown);
                    fprintf(fp, "  ship %d score:     %d.\n", i, ship->score);
                    for (int j = 0; j < ship.bullets.Length; j++) {
                        Bullet* bullet = ship->bullets + j;
                        fprintf(fp, "  ship %d bullet %d: %.2f %.2f -> %.2f %.2f.\n", i, j,
                                bullet->position.x, bullet->position.y,
                                bullet->velocity.dx, bullet->velocity.dy);
                    }
                }
                fclose(fp);
            }
            */
            return true;
        }

        unsafe void vw_free_buffer_callback(void* dataPtr) {
            if (cache.TryGetValue((long)dataPtr, out var data)) {
                data.Dispose();
            }
        }

        /*
         * Init --
         *
         * Initialize the vector war game.  This initializes the game state and
         * the video renderer and creates a new network session.
         */

        public void Init(int localport, int num_players, IList<GGPOPlayer> players, int num_spectators) {
            // Initialize the game state
            gs.Init(num_players);
            ngs.num_players = num_players;

            // Fill in a ggpo callbacks structure to pass to start_session. var cb = new GGPOSessionCallbacks();

#if SYNC_TEST
   result = ggpo_start_synctest(

        cb, "vectorwar", num_players, 1);
#else
            unsafe {
                ggpo = UGGPO.UggStartSession(
                    vw_begin_game_callback,
                    vw_advance_frame_callback,
                    vw_load_game_state_callback,
                    vw_log_game_state,
                    vw_save_game_state_callback,
                    vw_free_buffer_callback,
                    vw_on_event_callback,
                    "vectorwar", num_players, localport);
            }
#endif

            gs.PostInit(ggpo);
            // automatically disconnect clients after 3000 ms and start our count-down timer for
            // disconnects after 1000 ms. To completely disable disconnects, simply use a value of 0
            // for ggpo_set_disconnect_timeout.
            UGGPO.UggSetDisconnectTimeout(ggpo, 3000);
            UGGPO.UggSetDisconnectNotifyStart(ggpo, 1000);

            int i;
            for (i = 0; i < num_players + num_spectators; i++) {
                int handle;
                int result = UGGPO.UggAddPlayer(ggpo,
                    (int)players[i].type,
                    players[i].player_num,
                    players[i].ip_address,
                    players[i].port,
                    out handle);
                ngs.players[i].handle = handle;
                ngs.players[i].type = players[i].type;
                if (players[i].type == GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL) {
                    ngs.players[i].connect_progress = 100;
                    ngs.local_player_handle = handle;
                    ngs.SetConnectState(handle, PlayerConnectState.Connecting);
                    UGGPO.UggSetFrameDelay(ggpo, handle, FRAME_DELAY);
                }
                else {
                    ngs.players[i].connect_progress = 0;
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

        public void InitSpectator(int localport, int num_players, string host_ip, int host_port) {
            // Initialize the game state
            gs.Init(num_players);
            ngs.num_players = num_players;

            // Fill in a ggpo callbacks structure to pass to start_session.
            unsafe {
                ggpo = UGGPO.UggStartSpectating(
                        vw_begin_game_callback,
                        vw_advance_frame_callback,
                        vw_load_game_state_callback,
                        vw_log_game_state,
                        vw_save_game_state_callback,
                        vw_free_buffer_callback,
                        vw_on_event_callback,
                        "vectorwar", num_players, localport, host_ip, host_port);
            }

            perf.ggpoutil_perfmon_init();

            SetStatusText("Starting new spectator session");
        }

        /*
         * DisconnectPlayer --
         *
         * Disconnects a player from this session.
         */

        public void DisconnectPlayer(int player) {
            if (player < ngs.num_players) {
                string logbuf;
                var result = UGGPO.UggDisconnectPlayer(ggpo, ngs.players[player].handle);
                if (GGPOC.GGPO_SUCCEEDED(result)) {
                    logbuf = $"Disconnected player {player}.\n";
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

        void AdvanceFrame(ulong[] inputs, int disconnect_flags) {
            gs.Update(inputs, disconnect_flags);

            // update the checksums to display in the top of the window. this helps to detect desyncs.
            ngs.now.framenumber = gs._framenumber;
            var buffer = GameState.ToBytes(gs);
            ngs.now.checksum = fletcher32_checksum(buffer, buffer.Length / 2);
            if ((gs._framenumber % 90) == 0) {
                ngs.periodic = ngs.now;
            }

            // Notify ggpo that we've moved forward exactly 1 frame.
            UGGPO.UggAdvanceFrame(ggpo);

            // Update the performance monitor display.
            int[] handles = new int[MAX_PLAYERS];
            int count = 0;
            for (int i = 0; i < ngs.num_players; i++) {
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

        struct InputInfo {
            public int key;
            public int input;

            public InputInfo(int key, int input) {
                this.key = key;
                this.input = input;
            }
        }

        class InputAccess : BitAccess {
            readonly Section Thrust = new Section(0, 1);
            readonly Section Break = new Section(1, 1);
            readonly Section RotateLeft = new Section(2, 1);
            readonly Section RotateRight = new Section(3, 1);
            readonly Section Fire = new Section(4, 1);
            readonly Section Bomb = new Section(5, 1);

            public bool InputThrust { get => Get(Thrust) != 0; set => Set(Thrust, value ? 1 : 0); }
            public bool InputBreak { get => Get(Break) != 0; set => Set(Break, value ? 1 : 0); }
            public bool InputRotateLeft { get => Get(RotateLeft) != 0; set => Set(RotateLeft, value ? 1 : 0); }
            public bool InputRotateRight { get => Get(RotateRight) != 0; set => Set(RotateRight, value ? 1 : 0); }
            public bool InputFire { get => Get(Fire) != 0; set => Set(Fire, value ? 1 : 0); }
            public bool InputBomb { get => Get(Bomb) != 0; set => Set(Bomb, value ? 1 : 0); }
        }

        ulong ReadInputs() {
            var ia = new InputAccess();
            ia.InputThrust = UnityEngine.Input.GetKey(UnityEngine.KeyCode.UpArrow);
            ia.InputBreak = UnityEngine.Input.GetKey(UnityEngine.KeyCode.DownArrow);
            ia.InputRotateLeft = UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftArrow);
            ia.InputRotateRight = UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightArrow);
            ia.InputFire = UnityEngine.Input.GetKey(UnityEngine.KeyCode.D);
            ia.InputBomb = UnityEngine.Input.GetKey(UnityEngine.KeyCode.S);
            return ia.data;
        }

        /*
         * RunFrame --
         *
         * Run a single frame of the game.
         */

        public void RunFrame() {
            int result = GGPOC.GGPO_OK;

            if (ngs.local_player_handle != GGPOC.GGPO_INVALID_HANDLE) {
                ulong input = ReadInputs();
#if SYNC_TEST
     input = rand(); // test: use random inputs to demonstrate sync testing
#endif

                result = UGGPO.UggAddLocalInput(ggpo, ngs.local_player_handle, input);
            }

            // synchronize these inputs with ggpo. If we have enough input to proceed ggpo will
            // modify the input list with the correct inputs to use and return 1.
            if (GGPOC.GGPO_SUCCEEDED(result)) {
                ulong[] inputs = new ulong[MAX_SHIPS];
                result = UGGPO.UggSynchronizeInput(ggpo, inputs, MAX_SHIPS, out var disconnect_flags);
                if (GGPOC.GGPO_SUCCEEDED(result)) {
                    // inputs[0] and inputs[1] contain the inputs for p1 and p2. Advance the game by
                    // 1 frame using those inputs.
                    AdvanceFrame(inputs, disconnect_flags);
                }
            }
        }

        /*
         * Idle --
         *
         * Spend our idle time in ggpo so it can use whatever time we have left over
         * for its internal bookkeeping.
         */

        public void Idle(int time) {
            UGGPO.UggIdle(ggpo, time);
        }

        public void Exit() {
            UGGPO.UggCloseSession(ggpo);
        }

        void SetStatusText(string status) {
            ngs.status = status;
        }
    }
}
