#include <stdio.h>
#include <vector>
#include <IUnityInterface.h>
#include <string>
#include "ggponet.h"
#include "Plugin.h"
#include <memory>
#include <iostream>
#include <string>
#include <cstdio>

typedef void (*LogDelegate)(const char* string);
typedef bool (*BeginGameDelegate)(const char* string);
typedef bool (*AdvanceFrameDelegate)(int flags);
typedef bool (*LoadGameStateDelegate)(unsigned char* buffer, int length);
typedef bool (*LogGameStateDelegate)(char* string, unsigned char* buffer, int length);
typedef void* (*SaveGameStateDelegate)(int& length, int& checksum, int frame);
typedef void (*FreeBufferDelegate)(unsigned char* buffer, int length);
typedef bool (*OnEventConnectedToPeerDelegate)(int connected_player);
typedef bool (*OnEventSynchronizingWithPeerDelegate)(int synchronizing_player, int synchronizing_count, int synchronizing_total);
typedef bool (*OnEventSynchronizedWithPeerDelegate)(int synchronizing_player);
typedef bool (*OnEventRunningDelegate)();
typedef bool (*OnEventConnectionInterruptedDelegate)(int connection_interrupted_player, int connection_interrupted_disconnect_timeout);
typedef bool (*OnEventConnectionResumedDelegate)(int connection_resumed_player);
typedef bool (*OnEventDisconnectedFromPeerDelegate)(int disconnected_player);
typedef bool (*OnEventTimesyncDelegate)(int timesync_frames_ahead);

class GGPOInstance {
public:
	GGPOSession* session;
	GGPOSessionCallbacks cb;

	LogDelegate logCb;
	BeginGameDelegate beginGameCb;
	AdvanceFrameDelegate advanceFrameCb;
	LoadGameStateDelegate loadGameStateCb;
	LogGameStateDelegate logGameStateCb;
	SaveGameStateDelegate saveGameStateCb;
	FreeBufferDelegate freeBufferCb;
	OnEventConnectedToPeerDelegate onEventConnectedToPeerCb;
	OnEventSynchronizingWithPeerDelegate onEventSynchronizingWithPeerCb;
	OnEventSynchronizedWithPeerDelegate onEventSynchronizedWithPeerCb;
	OnEventRunningDelegate onEventRunningCb;
	OnEventConnectionInterruptedDelegate onEventConnectionInterruptedCb;
	OnEventConnectionResumedDelegate onEventConnectionResumedCb;
	OnEventDisconnectedFromPeerDelegate onEventDisconnectedFromPeerCb;
	OnEventTimesyncDelegate onEventTimesyncCb;

	static GGPOPlayer* CreatePlayer(int player_size,
		int player_type,
		int player_num,
		const char* player_ip_address,
		short player_port);

	virtual ~GGPOInstance() {
		for (auto player : players) {
			delete player;
		}
	}

	void AddPlayer(int handle, GGPOPlayer* player);

	std::vector<GGPOPlayer*> players;
};

GGPOInstance inst0;
LogDelegate logCallback;

void Log(const std::string& text) {
	logCallback(text.c_str());
}

void Log(const char* text) {
	logCallback(text);
}

template<typename ... Args>
void LogV(const char* format, Args ... args) {
	size_t size = snprintf(nullptr, 0, format, args ...) + 1; // Extra space for '\0'
	std::unique_ptr<char[]> buf(new char[size]);
	snprintf(buf.get(), size, format, args ...);
	Log(buf.get());
}

void TestSession0() {
	unsigned char* data;
	int length;
	int checksum;

	inst0.cb.advance_frame(1);
	inst0.cb.begin_game("Test");

	inst0.cb.save_game_state(&data, &length, &checksum, 1);
	inst0.cb.load_game_state(data, length);
	inst0.cb.log_game_state("", data, length);
	inst0.cb.free_buffer(data, length);

	GGPOEvent event;
	event.code = GGPO_EVENTCODE_CONNECTED_TO_PEER;
	inst0.cb.on_event(&event);
	event.code = GGPO_EVENTCODE_SYNCHRONIZING_WITH_PEER;
	inst0.cb.on_event(&event);
	event.code = GGPO_EVENTCODE_SYNCHRONIZED_WITH_PEER;
	inst0.cb.on_event(&event);
	event.code = GGPO_EVENTCODE_RUNNING;
	inst0.cb.on_event(&event);
	event.code = GGPO_EVENTCODE_CONNECTION_INTERRUPTED;
	inst0.cb.on_event(&event);
	event.code = GGPO_EVENTCODE_CONNECTION_RESUMED;
	inst0.cb.on_event(&event);
	event.code = GGPO_EVENTCODE_DISCONNECTED_FROM_PEER;
	inst0.cb.on_event(&event);
	event.code = GGPO_EVENTCODE_TIMESYNC;
	inst0.cb.on_event(&event);
}

bool __cdecl vw_begin_game_callback0(const char* name)
{
	return inst0.beginGameCb(name);
}

bool __cdecl vw_on_event_callback0(GGPOEvent* info)
{
	switch (info->code) {
	case GGPO_EVENTCODE_CONNECTED_TO_PEER:
		return inst0.onEventConnectedToPeerCb(info->u.connected.player);
	case GGPO_EVENTCODE_SYNCHRONIZING_WITH_PEER:
		return inst0.onEventSynchronizingWithPeerCb(info->u.synchronizing.player, info->u.synchronizing.count, info->u.synchronizing.total);
	case GGPO_EVENTCODE_SYNCHRONIZED_WITH_PEER:
		return inst0.onEventSynchronizedWithPeerCb(info->u.synchronized.player);
	case GGPO_EVENTCODE_RUNNING:
		return inst0.onEventRunningCb();
	case GGPO_EVENTCODE_CONNECTION_INTERRUPTED:
		return inst0.onEventConnectionInterruptedCb(info->u.connection_interrupted.player, info->u.connection_interrupted.disconnect_timeout);
	case GGPO_EVENTCODE_CONNECTION_RESUMED:
		return inst0.onEventConnectionResumedCb(info->u.connection_resumed.player);
	case GGPO_EVENTCODE_DISCONNECTED_FROM_PEER:
		return inst0.onEventDisconnectedFromPeerCb(info->u.disconnected.player);
	case GGPO_EVENTCODE_TIMESYNC:
		return inst0.onEventTimesyncCb(info->u.timesync.frames_ahead);
	}
	return false;
}

bool __cdecl vw_save_game_state_callback0(unsigned char** buffer, int* len, int* checksum, int frame)
{
	*buffer = (unsigned char*)inst0.saveGameStateCb(*len, *checksum, frame);
	return true;
}

GGPOPlayer* GGPOInstance::CreatePlayer(int player_size,
	int player_type,
	int player_num,
	const char* player_ip_address,
	short player_port)
{
	GGPOPlayer* player = new GGPOPlayer();
	player->size = player_size;
	player->type = (GGPOPlayerType)player_type;
	player->player_num = player_num;
	strcpy_s(player->u.remote.ip_address, player_ip_address);
	player->u.remote.port = player_port;
	return player;
}

void GGPOInstance::AddPlayer(int handle, GGPOPlayer* player) {
}

extern "C" const char UNITY_INTERFACE_EXPORT * UNITY_INTERFACE_API DllPluginVersion() {
	//This is defined in CMAKE and passed to the source.
	return PLUGIN_VERSION;
}
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllPluginBuildNumber() {
	//This is defined in CMAKE and passed to the source.
	return PLUGIN_BUILD_NUMBER;
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllSetLogDelegate(LogDelegate callback) {
	logCallback = callback;
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllStartSession(
	BeginGameDelegate beginGame,
	AdvanceFrameDelegate advanceFrame,
	LoadGameStateDelegate loadGameState,
	LogGameStateDelegate logGameState,
	SaveGameStateDelegate saveGameState,
	FreeBufferDelegate freeBuffer,
	OnEventConnectedToPeerDelegate onEventConnectedToPeer,
	OnEventSynchronizingWithPeerDelegate on_event_synchronizing_with_peer,
	OnEventSynchronizedWithPeerDelegate on_event_synchronized_with_peer,
	OnEventRunningDelegate on_event_running,
	OnEventConnectionInterruptedDelegate on_event_connection_interrupted,
	OnEventConnectionResumedDelegate onEventConnectionResumedDelegate,
	OnEventDisconnectedFromPeerDelegate onEventDisconnectedFromPeerDelegate,
	OnEventTimesyncDelegate onEventEventcodeTimesyncDelegate,
	const char* game, int num_players, int input_size, int localport) {
	LogV("DllStartSession - %s %i %i %i", game, num_players, input_size, localport);

	inst0.cb.advance_frame = advanceFrame;
	inst0.cb.load_game_state = loadGameState;
	inst0.cb.begin_game = vw_begin_game_callback0;
	inst0.beginGameCb = beginGame;
	inst0.cb.save_game_state = vw_save_game_state_callback0;
	inst0.saveGameStateCb = saveGameState;
	inst0.cb.load_game_state = loadGameState;
	inst0.cb.log_game_state = logGameState;
	inst0.cb.free_buffer = freeBuffer;
	inst0.cb.on_event = vw_on_event_callback0;
	inst0.onEventConnectedToPeerCb = onEventConnectedToPeer;
	inst0.onEventSynchronizingWithPeerCb = on_event_synchronizing_with_peer;
	inst0.onEventSynchronizedWithPeerCb = on_event_synchronized_with_peer;
	inst0.onEventRunningCb = on_event_running;
	inst0.onEventConnectionInterruptedCb = on_event_connection_interrupted;
	inst0.onEventConnectionResumedCb = onEventConnectionResumedDelegate;
	inst0.onEventDisconnectedFromPeerCb = onEventDisconnectedFromPeerDelegate;
	inst0.onEventTimesyncCb = onEventEventcodeTimesyncDelegate;

	TestSession0();

	//	ggpo_start_session(&session1.session, &session1.cb, game, num_players, input_size, localport);

	return -1;
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllStartSpectating(BeginGameDelegate begin_game,
	AdvanceFrameDelegate advance_frame,
	LoadGameStateDelegate load_game_state,
	LogGameStateDelegate log_game_state,
	SaveGameStateDelegate save_game_state,
	FreeBufferDelegate free_buffer,
	OnEventConnectedToPeerDelegate on_even_connected_to_peer,
	OnEventSynchronizingWithPeerDelegate on_event_synchronizing_with_peer,
	OnEventSynchronizedWithPeerDelegate on_event_synchronized_withpeer,
	OnEventRunningDelegate onEventRunningDelegate,
	OnEventConnectionInterruptedDelegate onEventConnectionInterruptedDelegate,
	OnEventConnectionResumedDelegate onEventConnectionResumedDelegate,
	OnEventDisconnectedFromPeerDelegate onEventDisconnectedFromPeerDelegate,
	OnEventTimesyncDelegate onEventEventcodeTimesyncDelegate,
	const char* game, int num_players, int input_size, int localport, const char* host_ip, int host_port) {
	LogV("DllStartSpectating - %s %i %i %i %i %s %i", game, num_players, input_size, localport, host_ip, host_port);
	return -1;
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllSetDisconnectNotifyStart(int ggpo, int timeout) {
	if (ggpo == 0) {
		//return ggpo_set_disconnect_notify_start(session1.session, timeout);
	}
	return -1;
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllSetDisconnectTimeout(int ggpo, int timeout)
{
	if (ggpo == 0) {
		//return ggpo_set_disconnect_timeout(session1.session, timeout);
	}
	return -1;
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllSynchronizeInput(int ggpo, unsigned long long* inputs, int length, int& disconnect_flags) {
	Log("DllSynchronizeInput");
	if (ggpo == 0) {
		// return ggpo_synchronize_input(session1.session, inputs, length, &disconnect_flags);
	}
	inputs[0] = 1;
	inputs[1] = 2;
	disconnect_flags = 2;
	return -1;
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllAddLocalInput(int ggpo, int local_player_handle, unsigned long long input, int length) {
	if (ggpo == 0) {
		// return ggpo_add_local_input(session1.session, local_player_handle, &input, length);
	}
	return 0;
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllCloseSession(int ggpo) {
	if (ggpo == 0) {
		// return ggpo_close_session(session1.session);
	}
	return -1;
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllIdle(int ggpo, int timeout) {
	if (ggpo == 0) {
		//		return ggpo_idle(session1.session, timeout);
	}
	return -1;
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllAddPlayer(int ggpo,
	int player_size,
	int player_type,
	int player_num,
	const char* player_ip_address,
	short player_port,
	int& phandle) {
	if (ggpo == 0) {
		auto player = inst0.CreatePlayer(player_size, player_type, player_num, player_ip_address, player_port);
		//		auto result = ggpo_add_player(session1.session, player, &handle);
		inst0.AddPlayer(phandle, player);
		//		return result;
	}
	return -1;
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllDisconnectPlayer(int ggpo, int phandle) {
	if (ggpo == 0) {
		//		return ggpo_disconnect_player(session1.session, handle);
	}
	return -1;
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllSetFrameDelay(int ggpo, int phandle, int frame_delay)
{
	if (ggpo == 0) {
		//		return ggpo_set_frame_delay(session1.session, handle, frame_delay);
	}
	return -1;
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllAdvanceFrame(int ggpo)
{
	if (ggpo == 0) {
		//		return ggpo_advance_frame(session1.session);
	}
	return -1;
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllLog(int ggpo, const char* v) {
	if (ggpo == 0) {
		//		return ggpo_log(session1.session, v);
	}
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllGetNetworkStats(int ggpo, int phandle,
	int& send_queue_len,
	int& recv_queue_len,
	int& ping,
	int& kbps_sent,
	int& local_frames_behind,
	int& remote_frames_behind
)
{
	if (ggpo == 0) {
		//GGPONetworkStats stats;
		//auto result = ggpo_get_network_stats(session1.session, phandle, &stats);
		//send_queue_len = stats.network.send_queue_len;
		//recv_queue_len = stats.network.recv_queue_len;
		//ping = stats.network.ping;
		//kbps_sent = stats.network.kbps_sent;
		//local_frames_behind = stats.timesync.local_frames_behind;
		//remote_frames_behind = stats.timesync.remote_frames_behind;
		//return result;
	}

	send_queue_len = 1;
	recv_queue_len = 2;
	ping = 3;
	kbps_sent = 4;
	local_frames_behind = 5;
	remote_frames_behind = 6;
	return -1;
}
