#include <IUnityInterface.h>

//[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//public delegate bool BeginGameDelegate(string name);
typedef bool (*BeginGameDelegate)(const char* name);

//[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//public delegate bool AdvanceFrameDelegate(int flags);
typedef bool (*AdvanceFrameDelegate)(int flags);

//[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//public delegate bool LoadGameStateDelegate(byte[] buffer);
typedef bool (*LoadGameStateDelegate)(char* buffer, int length);

//[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//public delegate void FreeBufferDelegate(byte[] buffer);
typedef void (*FreeBufferDelegate)(char* buffer, int length);

//[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//public delegate bool OnEventDelegate(int code, int player, int synchronizingcount, int synchronizingtotal, int disconnect_timeout, int frames_ahead);
typedef bool (*OnEventDelegate)(int code, int player, int synchronizingcount, int synchronizingtotal, int disconnect_timeout, int frames_ahead);

//[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//public delegate bool LogGameStateDelegate(string filename, byte[] buffer);
typedef bool (*LogGameStateDelegate)(const char* filename, byte* buffer, int size);

//[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//public delegate bool SaveGameStateDelegate(ref byte[] buffer, ref int len, ref int checksum, int frame);
typedef bool (*SaveGameStateDelegate)(&byte* buffer, int& len, int& cchecksum, int& frame);

vector<GGPOSession> sessions;
/*
 * The GGPOSessionCallbacks structure contains the callback functions that
 * your application must implement.  GGPO.net will periodically call these
 * functions during the game.  All callback functions must be implemented.
 */
typedef struct {
	/*
	 * begin_game callback - This callback has been deprecated.  You must
	 * implement it, but should ignore the 'game' parameter.
	 */
	bool(__cdecl* begin_game)(const char* game);

	/*
	 * save_game_state - The client should allocate a buffer, copy the
	 * entire contents of the current game state into it, and copy the
	 * length into the *len parameter.  Optionally, the client can compute
	 * a checksum of the data and store it in the *checksum argument.
	 */
	bool(__cdecl* save_game_state)(unsigned char** buffer, int* len, int* checksum, int frame);

	/*
	 * load_game_state - GGPO.net will call this function at the beginning
	 * of a rollback.  The buffer and len parameters contain a previously
	 * saved state returned from the save_game_state function.  The client
	 * should make the current game state match the state contained in the
	 * buffer.
	 */
	bool(__cdecl* load_game_state)(unsigned char* buffer, int len);

	/*
	 * log_game_state - Used in diagnostic testing.  The client should use
	 * the ggpo_log function to write the contents of the specified save
	 * state in a human readible form.
	 */
	bool(__cdecl* log_game_state)(char* filename, unsigned char* buffer, int len);

	/*
	 * free_buffer - Frees a game state allocated in save_game_state.  You
	 * should deallocate the memory contained in the buffer.
	 */
	void(__cdecl* free_buffer)(void* buffer);

	/*
	 * advance_frame - Called during a rollback.  You should advance your game
	 * state by exactly one frame.  Before each frame, call ggpo_synchronize_input
	 * to retrieve the inputs you should use for that frame.  After each frame,
	 * you should call ggpo_advance_frame to notify GGPO.net that you're
	 * finished.
	 *
	 * The flags parameter is reserved.  It can safely be ignored at this time.
	 */
	bool(__cdecl* advance_frame)(int flags);

	/*
	 * on_event - Notification that something has happened.  See the GGPOEventCode
	 * structure above for more information.
	 */
	bool(__cdecl* on_event)(GGPOEvent* info);
} GGPOSessionCallbacks;

public static int ggpo_start_session(BeginGameDelegate begin_game,
	AdvanceFrameDelegate advance_frame,
	LoadGameStateDelegate load_game_state,
	FreeBufferDelegate free_buffer,
	OnEventDelegate on_event,
	LogGameStateDelegate log_game_state,
	SaveGameStateDelegate save_game_state,
	string game, int num_players, int input_size, int localport) {

	auto cb = new GGPOSessionCallbacks();
	cb->on_event = on_event

	auto *session = (GGPOSession*)new Peer2PeerBackend(cb,
		game,
		localport,
		num_players,
		input_size);


	return 0;
}
