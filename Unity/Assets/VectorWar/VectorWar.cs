using System;
using Unity.Collections;
using static Constants;
using static GGPO;

public class VectorWar {

    //#define SYNC_TEST    // test: turn on synctest
    const int FRAME_DELAY = 2;

    GameState gs = null;
    NonGameState ngs = null;
    VectorWarRenderer renderer = null;
    public GGPOPerformance perf = null;
    int ggpo = -1;

    public VectorWar(GameState gs, NonGameState ngs, VectorWarRenderer renderer, GGPOPerformance perf) {
    }

    /*
     * Simple checksum function stolen from wikipedia:
     *
     *   http://en.wikipedia.org/wiki/Fletcher%27s_checksum
     */

    int fletcher32_checksum(byte[] data, int len) {
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

    bool vw_begin_game_callback() {
        return true;
    }

    /*
     * vw_on_event_callback --
     *
     * Notification from GGPO that something has happened.  Update the status
     * text at the bottom of the screen to notify the user.
     */

    bool vw_on_event_callback(int code, int player, int synchronizingcount, int synchronizingtotal, int disconnect_timeout, int frames_ahead) {
        int progress;
        switch (code) {
            case GGPO_EVENTCODE_CONNECTED_TO_PEER:
                ngs.SetConnectState(player, PlayerConnectState.Synchronizing);
                break;

            case GGPO_EVENTCODE_SYNCHRONIZING_WITH_PEER:
                progress = 100 * synchronizingcount / synchronizingtotal;
                ngs.UpdateConnectProgress(player, progress);
                break;

            case GGPO_EVENTCODE_SYNCHRONIZED_WITH_PEER:
                ngs.UpdateConnectProgress(player, 100);
                break;

            case GGPO_EVENTCODE_RUNNING:
                ngs.SetConnectState(PlayerConnectState.Running);
                renderer.SetStatusText("");
                break;

            case GGPO_EVENTCODE_CONNECTION_INTERRUPTED:
                ngs.SetDisconnectTimeout(player, (int)(UnityEngine.Time.time * 1000f), disconnect_timeout);
                break;

            case GGPO_EVENTCODE_CONNECTION_RESUMED:
                ngs.SetConnectState(player, PlayerConnectState.Running);
                break;

            case GGPO_EVENTCODE_DISCONNECTED_FROM_PEER:
                ngs.SetConnectState(player, PlayerConnectState.Disconnected);
                break;

            case GGPO_EVENTCODE_TIMESYNC:
                Sleep(1000 * frames_ahead / 60);
                break;
        }
        return true;
    }

    void Sleep(int v) {
        throw new NotImplementedException();
    }

    /*
     * vw_advance_frame_callback --
     *
     * Notification from GGPO we should step foward exactly 1 frame
     * during a rollback.
     */

    bool vw_advance_frame_callback(int flags) {
        // Make sure we fetch new inputs from GGPO and use those to update the game state instead of
        // reading from the keyboard.
        GGPO.ggpo_synchronize_input(ggpo, out var buffer, out var disconnect_flags);

        int[] inputs = ConvertInputBuffer(buffer);

        VectorWar_AdvanceFrame(inputs, disconnect_flags);
        return true;
    }

    static int[] ConvertInputBuffer(byte[] buffer) {
        int[] inputs = new int[MAX_SHIPS];
        for (int i = 0; i < MAX_SHIPS; ++i) {
            inputs[i] = BitConverter.ToInt32(buffer, sizeof(int) * i);
        }

        return inputs;
    }

    /*
     * vw_load_game_state_callback --
     *
     * Makes our current state match the state passed in by GGPO.
     */

    bool vw_load_game_state_callback(NativeArray<byte> data) {
        gs = GameState.FromBytes(data);
        return true;
    }

    /*
     * vw_save_game_state_callback --
     *
     * Save the current state to a buffer and return it to GGPO via the
     * buffer and len parameters.
     */

    NativeArray<byte> vw_save_game_state_callback(out int checksum, int frame) {
        var buffer = GameState.ToBytes(gs);
        checksum = 0;
        return buffer;
    }

    /*
     * vw_log_game_state --
     *
     * Log the gamestate.  Used by the synctest debugging tool.
     */

    bool vw_log_game_state(NativeArray<byte> data) {
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
                for (int j = 0; j < MAX_BULLETS; j++) {
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

    /*
     * VectorWar_Init --
     *
     * Initialize the vector war game.  This initializes the game state and
     * the video renderer and creates a new network session.
     */

    public void VectorWar_Init(int localport, int num_players, GGPOPlayer[] players, int num_spectators) {
        // Initialize the game state
        gs.Init(num_players);
        ngs.num_players = num_players;

        // Fill in a ggpo callbacks structure to pass to start_session.
        var cb = new GGPOSessionCallbacks();
/*
        cb.begin_game = vw_begin_game_callback;
        cb.advance_frame = vw_advance_frame_callback;
        cb.load_game_state = vw_load_game_state_callback;
        cb.save_game_state = vw_save_game_state_callback;
        // cb.on_event = vw_on_event_callback;
        cb.log_game_state = vw_log_game_state;
        */
#if SYNC_TEST
   result = ggpo_start_synctest(&ggpo, &cb, "vectorwar", num_players, sizeof(int), 1);
#else
        ggpo = GGPO.ggpo_start_session(cb, "vectorwar", num_players, sizeof(int), localport);
#endif

        gs.PostInit(ggpo);
        // automatically disconnect clients after 3000 ms and start our count-down timer for
        // disconnects after 1000 ms. To completely disable disconnects, simply use a value of 0 for ggpo_set_disconnect_timeout.
        GGPO.ggpo_set_disconnect_timeout(ggpo, 3000);
        GGPO.ggpo_set_disconnect_notify_start(ggpo, 1000);

        int i;
        for (i = 0; i < num_players + num_spectators; i++) {
            int handle;
            int result = GGPO.ggpo_add_player(ggpo, players[i], out handle);
            ngs.players[i].handle = handle;
            ngs.players[i].type = players[i].type;
            if (players[i].type == GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL) {
                ngs.players[i].connect_progress = 100;
                ngs.local_player_handle = handle;
                ngs.SetConnectState(handle, PlayerConnectState.Connecting);
                GGPO.ggpo_set_frame_delay(ggpo, handle, FRAME_DELAY);
            }
            else {
                ngs.players[i].connect_progress = 0;
            }
        }

        perf.ggpoutil_perfmon_init();
        renderer.SetStatusText("Connecting to peers.");
    }

    /*
     * VectorWar_InitSpectator --
     *
     * Create a new spectator session
     */

    public void VectorWar_InitSpectator(int localport, int num_players, string host_ip, int host_port) {
        renderer = new VectorWarRenderer();

        // Initialize the game state
        gs.Init(num_players);
        ngs.num_players = num_players;

        // Fill in a ggpo callbacks structure to pass to start_session.
        var cb = new GGPOSession();
/*
        cb.begin_game = vw_begin_game_callback;
        cb.advance_frame = vw_advance_frame_callback;
        cb.load_game_state = vw_load_game_state_callback;
        cb.save_game_state = vw_save_game_state_callback;
        // cb.on_event = vw_on_event_callback;
        cb.log_game_state = vw_log_game_state;
        */

        ggpo = GGPO.ggpo_start_spectating(
               public delegate void LogDelegate(string text);

    public delegate bool BeginGameDelegate();

    public delegate bool AdvanceFrameDelegate(int flags);

    public delegate bool SafeLoadGameStateDelegate(NativeArray<byte> data);

    public delegate bool SafeLogGameStateDelegate(NativeArray<byte> data);

    public delegate NativeArray<byte> SafeSaveGameStateDelegate(out int checksum, int frame);

    unsafe public delegate bool LoadGameStateDelegate([MarshalAs(UnmanagedType.LPArray)] void* buffer, int length);

    unsafe public delegate bool LogGameStateDelegate([MarshalAs(UnmanagedType.LPArray)] void* buffer, int length);

    [return: MarshalAs(UnmanagedType.LPArray)]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void* SaveGameStateDelegate(out int length, out int checksum, int frame);

    unsafe public delegate void FreeBufferDelegate(void* buffer, int length);

    public delegate bool OnEventConnectedToPeerDelegate(int connected_player);

    public delegate bool OnEventSynchronizingWithPeerDelegate(int synchronizing_player, int synchronizing_count, int synchronizing_total);

    public delegate bool OnEventSynchronizedWithPeerDelegate(int synchronizing_player);

    public delegate bool OnEventRunningDelegate();

    public delegate bool OnEventConnectionInterruptedDelegate(int connection_interrupted_player, int connection_interrupted_disconnect_timeout);

    public delegate bool OnEventConnectionResumedDelegate(int connection_resumed_player);

    public delegate bool OnEventDisconnectedFromPeerDelegate(int disconnected_player);

    public delegate bool OnEventEventcodeTimesyncDelegate(int timesync_frames_ahead);


    cb, "vectorwar", num_players, sizeof(int), localport, host_ip, host_port);

        perf.ggpoutil_perfmon_init();

        renderer.SetStatusText("Starting new spectator session");
    }

    /*
     * VectorWar_DisconnectPlayer --
     *
     * Disconnects a player from this session.
     */

    public void VectorWar_DisconnectPlayer(int player) {
        if (player < ngs.num_players) {
            string logbuf;
            var result = GGPO.ggpo_disconnect_player(ggpo, ngs.players[player].handle);
            if (GGPO.GGPO_SUCCEEDED(result)) {
                logbuf = $"Disconnected player {player}.\n";
            }
            else {
                logbuf = $"Error while disconnecting player (err:{result}).\n";
            }
            renderer.SetStatusText(logbuf);
        }
    }

    /*
     * VectorWar_DrawCurrentFrame --
     *
     * Draws the current frame without modifying the game state.
     */

    public void VectorWar_DrawCurrentFrame() {
        renderer.Draw(gs, ngs);
    }

    /*
     * VectorWar_AdvanceFrame --
     *
     * Advances the game state by exactly 1 frame using the inputs specified
     * for player 1 and player 2.
     */

    public void VectorWar_AdvanceFrame(int[] inputs, int disconnect_flags) {
        gs.Update(inputs, disconnect_flags);

        // update the checksums to display in the top of the window. this helps to detect desyncs.
        ngs.now.framenumber = gs._framenumber;
        var buffer = GameState.ToBytes(gs);
        ngs.now.checksum = fletcher32_checksum(buffer, buffer.Length / 2);
        if ((gs._framenumber % 90) == 0) {
            ngs.periodic = ngs.now;
        }

        // Notify ggpo that we've moved forward exactly 1 frame.
        GGPO.ggpo_advance_frame(ggpo);

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

    // @TODO
    int ReadInputs() {
        InputInfo[] inputtable = {
            new InputInfo( VK_UP,       INPUT_THRUST ),
            new InputInfo( VK_DOWN,     INPUT_BREAK ),
            new InputInfo( VK_LEFT,     INPUT_ROTATE_LEFT ),
            new InputInfo( VK_RIGHT,    INPUT_ROTATE_RIGHT ),
            new InputInfo( 'D',         INPUT_FIRE ),
            new InputInfo('S', INPUT_BOMB ),
        };
        int i, inputs = 0;

        //if (GetForegroundWindow() == hwnd) {
        //    for (i = 0; i < sizeof(inputtable) / sizeof(inputtable[0]); i++) {
        //        if (GetAsyncKeyState(inputtable[i].key)) {
        //            inputs |= inputtable[i].input;
        //        }
        //    }
        //}

        return inputs;
    }

    /*
     * VectorWar_RunFrame --
     *
     * Run a single frame of the game.
     */

    public void VectorWar_RunFrame() {
        int result = GGPO.GGPO_OK;

        if (ngs.local_player_handle != GGPO.GGPO_INVALID_HANDLE) {
            int input = ReadInputs();
#if SYNC_TEST
     input = rand(); // test: use random inputs to demonstrate sync testing
#endif
            byte[] localInputBuffer = BitConverter.GetBytes(input);
            result = GGPO.ggpo_add_local_input(ggpo, ngs.local_player_handle, localInputBuffer);
        }

        // synchronize these inputs with ggpo. If we have enough input to proceed ggpo will modify
        // the input list with the correct inputs to use and return 1.
        if (GGPO.GGPO_SUCCEEDED(result)) {
            result = GGPO.ggpo_synchronize_input(ggpo, out var buffer, out var disconnect_flags);
            if (GGPO.GGPO_SUCCEEDED(result)) {
                // inputs[0] and inputs[1] contain the inputs for p1 and p2. Advance the game by 1
                // frame using those inputs.
                var inputs = ConvertInputBuffer(buffer);
                VectorWar_AdvanceFrame(inputs, disconnect_flags);
            }
        }
        VectorWar_DrawCurrentFrame();
    }

    /*
     * VectorWar_Idle --
     *
     * Spend our idle time in ggpo so it can use whatever time we have left over
     * for its internal bookkeeping.
     */

    public void VectorWar_Idle(int time) {
        GGPO.ggpo_idle(ggpo, time);
    }

    public void VectorWar_Exit() {
        if (ggpo >= 0) {
            GGPO.ggpo_close_session(ggpo);
            ggpo = -1;
        }
    }
}
