//using System;

//[Serializable]
//public struct GGPOPlayer {
//    public int size;
//    public GGPOPlayerType type;
//    public int player_num;
//    public string ip_address;
//    public short port;
//}

//public class GGPOSessionCallbacks {
//    public GGPO.BeginGameDelegate begin_game;

//    public GGPO.AdvanceFrameDelegate advance_frame;

//    public GGPO.LoadGameStateDelegate load_game_state;

//    public GGPO.FreeBufferDelegate free_buffer;

//    public GGPO.OnEventDelegate on_event;

//    public GGPO.LogGameStateDelegate log_game_state;

//    public GGPO.SaveGameStateDelegate save_game_state;
//}

//public class GGPONetworkStats {
//    public int send_queue_len;
//    public int recv_queue_len;
//    public int ping;
//    public int kbps_sent;
//    public int local_frames_behind;
//    public int remote_frames_behind;
//}

//public static class GGPO {
//    public const int GGPO_OK = 0;
//    public const int GGPO_INVALID_HANDLE = -1;

//    public const int GGPO_ERRORCODE_SUCCESS = 0;
//    public const int GGPO_ERRORCODE_GENERAL_FAILURE = -1;
//    public const int GGPO_ERRORCODE_INVALID_SESSION = 1;
//    public const int GGPO_ERRORCODE_INVALID_PLAYER_HANDLE = 2;
//    public const int GGPO_ERRORCODE_PLAYER_OUT_OF_RANGE = 3;
//    public const int GGPO_ERRORCODE_PREDICTION_THRESHOLD = 4;
//    public const int GGPO_ERRORCODE_UNSUPPORTED = 5;
//    public const int GGPO_ERRORCODE_NOT_SYNCHRONIZED = 6;
//    public const int GGPO_ERRORCODE_IN_ROLLBACK = 7;
//    public const int GGPO_ERRORCODE_INPUT_DROPPED = 8;
//    public const int GGPO_ERRORCODE_PLAYER_DISCONNECTED = 9;
//    public const int GGPO_ERRORCODE_TOO_MANY_SPECTATORS = 10;
//    public const int GGPO_ERRORCODE_INVALID_REQUEST = 11;

//    public const int GGPO_EVENTCODE_CONNECTED_TO_PEER = 1000;
//    public const int GGPO_EVENTCODE_SYNCHRONIZING_WITH_PEER = 1001;
//    public const int GGPO_EVENTCODE_SYNCHRONIZED_WITH_PEER = 1002;
//    public const int GGPO_EVENTCODE_RUNNING = 1003;
//    public const int GGPO_EVENTCODE_DISCONNECTED_FROM_PEER = 1004;
//    public const int GGPO_EVENTCODE_TIMESYNC = 1005;
//    public const int GGPO_EVENTCODE_CONNECTION_INTERRUPTED = 1006;
//    public const int GGPO_EVENTCODE_CONNECTION_RESUMED = 1007;

//    public delegate bool BeginGameDelegate(string name);

//    public delegate bool AdvanceFrameDelegate(int flags);

//    public delegate bool LoadGameStateDelegate(byte[] buffer);

//    public delegate void FreeBufferDelegate(byte[] buffer);

//    public delegate bool OnEventDelegate(int code, int player, int synchronizingcount, int synchronizingtotal, int disconnect_timeout, int frames_ahead);

//    public delegate bool LogGameStateDelegate(string filename, byte[] buffer);

//    public delegate bool SaveGameStateDelegate(ref byte[] buffer, ref int len, ref int checksum, int frame);

//    // DLL
//    public static int ggpo_start_session(BeginGameDelegate begin_game,
//            AdvanceFrameDelegate advance_frame,
//            LoadGameStateDelegate load_game_state,
//            FreeBufferDelegate free_buffer,
//            OnEventDelegate on_event,
//            LogGameStateDelegate log_game_state,
//            SaveGameStateDelegate save_game_state,
//            string game, int num_players, int input_size, int localport) {
//        return 0;
//    }

//    public static int ggpo_start_session(GGPOSessionCallbacks cb, string game, int num_players, int input_size, int localport) {
//        return ggpo_start_session(cb.begin_game,
//            cb.advance_frame,
//            cb.load_game_state,
//            cb.free_buffer,
//            cb.on_event,
//            cb.log_game_state,
//            cb.save_game_state, game, num_players, input_size, localport);
//    }

//    public static int ggpo_start_spectating(BeginGameDelegate begin_game,
//            AdvanceFrameDelegate advance_frame,
//            LoadGameStateDelegate load_game_state,
//            FreeBufferDelegate free_buffer,
//            OnEventDelegate on_event,
//            LogGameStateDelegate log_game_state,
//            SaveGameStateDelegate save_game_state,
//            string game, int num_players, int input_size, int localport, string host_ip, int host_port) {
//        return 0;
//    }

//    public static int ggpo_start_spectating(GGPOSessionCallbacks cb,
//            string game,
//            int num_players,
//            int input_size,
//            int local_port,
//            string host_ip,
//            int host_port) {
//        return ggpo_start_spectating(cb.begin_game,
//            cb.advance_frame,
//            cb.load_game_state,
//            cb.free_buffer,
//            cb.on_event,
//            cb.log_game_state,
//            cb.save_game_state, game, num_players, input_size, local_port, host_ip, host_port);
//    }

//    public static void ggpo_set_disconnect_notify_start(int ggpo, int v) {
//        throw new NotImplementedException();
//    }

//    public static void ggpo_set_disconnect_timeout(int ggpo, int v) {
//        throw new NotImplementedException();
//    }

//    public static int ggpo_synchronize_input(int ggpo, out byte[] inputs, out int disconnect_flags) {
//        throw new NotImplementedException();
//    }

//    internal static int ggpo_add_local_input(int ggpo, int local_player_handle, byte[] inputs) {
//        throw new NotImplementedException();
//    }

//    internal static void ggpo_close_session(int ggpo) {
//        throw new NotImplementedException();
//    }

//    internal static void ggpo_idle(int ggpo, int time) {
//        throw new NotImplementedException();
//    }

//    internal static int ggpo_add_player(int ggpo, GGPOPlayer player, out int handle) {
//        throw new NotImplementedException();
//    }

//    internal static int ggpo_disconnect_player(int ggpo, int handle) {
//        throw new NotImplementedException();
//    }

//    internal static void ggpo_set_frame_delay(int ggpo, int handle, int frame_delay) {
//        throw new NotImplementedException();
//    }

//    internal static void ggpo_advance_frame(int ggpo) {
//        throw new NotImplementedException();
//    }

//    internal static void ggpo_log(int ggpo, string v) {
//        throw new NotImplementedException();
//    }

//    public static bool GGPO_SUCCEEDED(int result) {
//        return result == GGPO_OK;
//    }

//    public static void ggpo_get_network_stats(int ggpo, int p, out GGPONetworkStats stats) {
//        stats = new GGPONetworkStats();
//        ggpo_get_network_stats(ggpo, p,
//            out stats.send_queue_len,
//            out stats.recv_queue_len,
//            out stats.ping,
//            out stats.kbps_sent,
//            out stats.local_frames_behind,
//            out stats.remote_frames_behind
//        );
//    }

//    public static void ggpo_get_network_stats(int ggpo, int p,
//        out int send_queue_len,
//        out int recv_queue_len,
//        out int ping,
//        out int kbps_sent,
//        out int local_frames_behind,
//        out int remote_frames_behind
//    ) {
//        throw new NotImplementedException();
//    }
//}
