#include <stdio.h>
#include <vector>
#include <string>
#include "ggponet.h"
#include "Plugin.h"
#include <memory>
#include <iostream>
#include <string>
#include <cstdio>

constexpr auto PLUGIN_VERSION = "1.0.0.0";
constexpr auto PLUGIN_BUILD_NUMBER = 102;

GGPOInstance inst1;
LogDelegate logCallback = nullptr;

void CallLog(const char* text)
{
	if (logCallback) {
		logCallback(text);
	}
}

template<typename ... Args>
void CallLogv(const char* format, Args ... args)
{
	size_t size = snprintf(nullptr, 0, format, args ...) + 1; // Extra space for '\0'
	std::unique_ptr<char[]> buf(new char[size]);
	snprintf(buf.get(), size, format, args ...);
	CallLog(buf.get());
}

void TestInst1()
{
	unsigned char* data;
	int length;
	int checksum;

	inst1.cb.advance_frame(1);
	inst1.cb.begin_game("Test");

	inst1.cb.save_game_state(&data, &length, &checksum, 1);
	inst1.cb.load_game_state(data, length);
	inst1.cb.log_game_state("", data, length);
	inst1.cb.free_buffer(data);

	DllTestRealOnEventDelegate(inst1.cb.on_event);

	//GGPOEvent evt;

	//evt.code = GGPO_EVENTCODE_CONNECTED_TO_PEER;
	//evt.u.connected.player = 9;
	//inst1.cb.on_event(&evt);

	//evt.code = GGPO_EVENTCODE_SYNCHRONIZING_WITH_PEER;
	//evt.u.synchronizing.player = 9;
	//evt.u.synchronizing.count = 10;
	//evt.u.synchronizing.total = 11;
	//inst1.cb.on_event(&evt);

	//evt.code = GGPO_EVENTCODE_SYNCHRONIZED_WITH_PEER;
	//evt.u.synchronized.player = 9;
	//inst1.cb.on_event(&evt);

	//evt.code = GGPO_EVENTCODE_RUNNING;
	//inst1.cb.on_event(&evt);

	//evt.code = GGPO_EVENTCODE_CONNECTION_INTERRUPTED;
	//evt.u.connection_interrupted.player = 9;
	//evt.u.connection_interrupted.disconnect_timeout = 10;
	//inst1.cb.on_event(&evt);

	//evt.code = GGPO_EVENTCODE_CONNECTION_RESUMED;
	//evt.u.connection_resumed.player = 9;
	//inst1.cb.on_event(&evt);

	//evt.code = GGPO_EVENTCODE_DISCONNECTED_FROM_PEER;
	//evt.u.disconnected.player = 9;
	//inst1.cb.on_event(&evt);

	//evt.code = GGPO_EVENTCODE_TIMESYNC;
	//evt.u.timesync.frames_ahead = 9;
	//inst1.cb.on_event(&evt);
}

bool __cdecl onEventCallback1(GGPOEvent* info)
{
	switch (info->code)
	{
	case GGPO_EVENTCODE_CONNECTED_TO_PEER:
		return inst1.onEventConnectedToPeerCb(info->u.connected.player);
	case GGPO_EVENTCODE_SYNCHRONIZING_WITH_PEER:
		return inst1.onEventSynchronizingWithPeerCb(info->u.synchronizing.player, info->u.synchronizing.count, info->u.synchronizing.total);
	case GGPO_EVENTCODE_SYNCHRONIZED_WITH_PEER:
		return inst1.onEventSynchronizedWithPeerCb(info->u.synchronized.player);
	case GGPO_EVENTCODE_RUNNING:
		return inst1.onEventRunningCb();
	case GGPO_EVENTCODE_CONNECTION_INTERRUPTED:
		return inst1.onEventConnectionInterruptedCb(info->u.connection_interrupted.player, info->u.connection_interrupted.disconnect_timeout);
	case GGPO_EVENTCODE_CONNECTION_RESUMED:
		return inst1.onEventConnectionResumedCb(info->u.connection_resumed.player);
	case GGPO_EVENTCODE_DISCONNECTED_FROM_PEER:
		return inst1.onEventDisconnectedFromPeerCb(info->u.disconnected.player);
	case GGPO_EVENTCODE_TIMESYNC:
		return inst1.onEventTimesyncCb(info->u.timesync.frames_ahead);
	}
	return false;
}

bool __cdecl saveGameStateCallback1(unsigned char** buffer, int* len, int* checksum, int frame)
{
	*buffer = (unsigned char*)inst1.saveGameStateCb(*len, *checksum, frame);
	return true;
}

extern "C" const char UNITY_INTERFACE_EXPORT * UNITY_INTERFACE_API DllPluginVersion()
{
	return PLUGIN_VERSION;
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllPluginBuildNumber()
{
	return PLUGIN_BUILD_NUMBER;
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllSetLogDelegate(LogDelegate callback)
{
	logCallback = callback;
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllTestRealSaveGameDelegate(RealSaveGameStateDelegate callback, FreeBufferDelegate freeBufferCallback)
{
	CallLog("DllTestRealSaveGameDelegate");
	unsigned char* buffer;
	int length;
	int checksum;
	callback(&buffer, &length, &checksum, 1);
	if (length > 3) {
		CallLogv("Result %d %d %d %d %d", buffer[0], buffer[1], buffer[2], length, checksum);
	}
	freeBufferCallback(buffer);
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllTestRealOnEventDelegate(RealOnEventDelegate realOnEventCallback)
{
	CallLogv("DllTestRealOnEventDelegate");

	GGPOEvent evt;
	evt.code = GGPO_EVENTCODE_CONNECTED_TO_PEER;
	evt.u.connected.player = 9;
	realOnEventCallback(&evt);

	evt.code = GGPO_EVENTCODE_SYNCHRONIZING_WITH_PEER;
	evt.u.synchronizing.player = 9;
	evt.u.synchronizing.count = 10;
	evt.u.synchronizing.total = 11;
	realOnEventCallback(&evt);

	evt.code = GGPO_EVENTCODE_SYNCHRONIZED_WITH_PEER;
	evt.u.synchronized.player = 9;
	realOnEventCallback(&evt);

	evt.code = GGPO_EVENTCODE_RUNNING;
	realOnEventCallback(&evt);

	evt.code = GGPO_EVENTCODE_CONNECTION_INTERRUPTED;
	evt.u.connection_interrupted.player = 9;
	evt.u.connection_interrupted.disconnect_timeout = 10;
	realOnEventCallback(&evt);

	evt.code = GGPO_EVENTCODE_CONNECTION_RESUMED;
	evt.u.connection_resumed.player = 9;
	realOnEventCallback(&evt);

	evt.code = GGPO_EVENTCODE_DISCONNECTED_FROM_PEER;
	evt.u.disconnected.player = 9;
	realOnEventCallback(&evt);

	evt.code = GGPO_EVENTCODE_TIMESYNC;
	evt.u.timesync.frames_ahead = 9;
	realOnEventCallback(&evt);
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllTestGameStateDelegates(
	SaveGameStateDelegate saveGameState,
	LogGameStateDelegate logGameState,
	LoadGameStateDelegate loadGameState,
	FreeBufferDelegate freeBuffer)
{
	CallLogv("DllTestGameStateDelegates");

	int length;
	int checksum;
	unsigned char* buffer = (unsigned char*)saveGameState(length, checksum, 1);
	logGameState("Test", buffer, length);
	loadGameState(buffer, length);
	freeBuffer(buffer);
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllTestStartSession(
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
	const char* game, int num_players, int localport)
{
	CallLogv("DllTestStartSession - %s %i %i", game, num_players, localport);

	inst1.cb.advance_frame = advanceFrame;
	inst1.cb.load_game_state = loadGameState;
	inst1.cb.begin_game = beginGame;
	inst1.cb.save_game_state = saveGameStateCallback1;
	inst1.saveGameStateCb = saveGameState;
	inst1.cb.load_game_state = loadGameState;
	inst1.cb.log_game_state = logGameState;
	inst1.cb.free_buffer = freeBuffer;
	inst1.cb.on_event = onEventCallback1;
	inst1.onEventConnectedToPeerCb = onEventConnectedToPeer;
	inst1.onEventSynchronizingWithPeerCb = on_event_synchronizing_with_peer;
	inst1.onEventSynchronizedWithPeerCb = on_event_synchronized_with_peer;
	inst1.onEventRunningCb = on_event_running;
	inst1.onEventConnectionInterruptedCb = on_event_connection_interrupted;
	inst1.onEventConnectionResumedCb = onEventConnectionResumedDelegate;
	inst1.onEventDisconnectedFromPeerCb = onEventDisconnectedFromPeerDelegate;
	inst1.onEventTimesyncCb = onEventEventcodeTimesyncDelegate;

	TestInst1();

	return 0;
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
	const char* game, int num_players, int localport)
{
	CallLogv("DllStartSession - %s %i %i", game, num_players, localport);

	inst1.cb.advance_frame = advanceFrame;
	inst1.cb.load_game_state = loadGameState;
	inst1.cb.begin_game = beginGame;
	inst1.cb.save_game_state = saveGameStateCallback1;
	inst1.saveGameStateCb = saveGameState;
	inst1.cb.load_game_state = loadGameState;
	inst1.cb.log_game_state = logGameState;
	inst1.cb.free_buffer = freeBuffer;
	inst1.cb.on_event = onEventCallback1;
	inst1.onEventConnectedToPeerCb = onEventConnectedToPeer;
	inst1.onEventSynchronizingWithPeerCb = on_event_synchronizing_with_peer;
	inst1.onEventSynchronizedWithPeerCb = on_event_synchronized_with_peer;
	inst1.onEventRunningCb = on_event_running;
	inst1.onEventConnectionInterruptedCb = on_event_connection_interrupted;
	inst1.onEventConnectionResumedCb = onEventConnectionResumedDelegate;
	inst1.onEventDisconnectedFromPeerCb = onEventDisconnectedFromPeerDelegate;
	inst1.onEventTimesyncCb = onEventEventcodeTimesyncDelegate;

	auto ret = ggpo_start_session(&inst1.session, &inst1.cb, game, num_players, sizeof(uint64_t), localport);

	if (GGPO_SUCCEEDED(ret)) {
		return 1;
	}
	else {
		return 0;
	}
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
	const char* game, int num_players, int localport, char* host_ip, int host_port)
{
	CallLogv("DllStartSpectating - %s %i %i %i %s %i", game, num_players, localport, host_ip, host_port);
	auto ret = ggpo_start_spectating(&inst1.session, &inst1.cb, game, num_players, sizeof(uint64_t), localport, host_ip, host_port);
	if (GGPO_SUCCEEDED(ret)) {
		return 1;
	}
	else {
		return 0;
	}
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllSetDisconnectNotifyStart(int ggpo, int timeout)
{
	CallLog("DllSetDisconnectNotifyStart");
	if (ggpo == 1) {
		return ggpo_set_disconnect_notify_start(inst1.session, timeout);
	}
	else {
		return 0;
	}
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllSetDisconnectTimeout(int ggpo, int timeout)
{
	CallLog("DllSetDisconnectTimeout");
	if (ggpo == 1) {
		return ggpo_set_disconnect_timeout(inst1.session, timeout);
	}
	else {
		return 0;
	}
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllSynchronizeInput(int ggpo, uint64_t* inputs, int length, int& disconnect_flags)
{
	CallLog("DllSynchronizeInput");
	if (ggpo == 1) {
		return ggpo_synchronize_input(inst1.session, inputs, sizeof(uint64_t) * length, &disconnect_flags);
	}
	else {
		inputs[0] = 1;
		inputs[1] = 2;
		disconnect_flags = 2;
		return 0;
	}
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllAddLocalInput(int ggpo, int local_player_handle, uint64_t input)
{
	CallLog("DllAddLocalInput");
	if (ggpo == 1) {
		return ggpo_add_local_input(inst1.session, local_player_handle, &input, sizeof(uint64_t));
	}
	else {
		return 0;
	}
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllCloseSession(int ggpo)
{
	CallLog("DllCloseSession");
	if (ggpo == 1) {
		if (inst1.session) {
			ggpo_close_session(inst1.session);
			inst1.session = nullptr;
		}
		return 0;
	}
	else {
		return 0;
	}
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllIdle(int ggpo, int timeout)
{
	CallLog("DllIdle");
	if (ggpo == 1) {
		return ggpo_idle(inst1.session, timeout);
	}
	else {
		return 0;
	}
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllAddPlayer(int ggpo,
	int player_type,
	int player_num,
	const char* player_ip_address,
	short player_port,
	int& phandle)
{
	CallLog("DllAddPlayer");
	if (ggpo == 1) {

		GGPOPlayer player;
		player.size = sizeof(GGPOPlayer);
		player.type = (GGPOPlayerType)player_type;
		player.player_num = player_num;
		strcpy_s(player.u.remote.ip_address, player_ip_address);
		player.u.remote.port = player_port;

		return ggpo_add_player(inst1.session, &player, &phandle);
	}
	else {
		return 0;
	}
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllDisconnectPlayer(int ggpo, int phandle)
{
	CallLog("DllDisconnectPlayer");
	if (ggpo == 1) {
		return ggpo_disconnect_player(inst1.session, phandle);
	}
	else {
		return 0;
	}
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllSetFrameDelay(int ggpo, int phandle, int frame_delay)
{
	CallLog("DllSetFrameDelay");
	if (ggpo == 1) {
		return ggpo_set_frame_delay(inst1.session, phandle, frame_delay);
	}
	else {
		return 0;
	}
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllAdvanceFrame(int ggpo)
{
	CallLog("DllAdvanceFrame");
	if (ggpo == 1) {
		return ggpo_advance_frame(inst1.session);
	}
	else {
		return 0;
	}
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllLog(int ggpo, const char* v)
{
	CallLogv("DllLog %s", v);
	if (ggpo == 1) {
		return ggpo_log(inst1.session, v);
	}
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllGetNetworkStats(int ggpo, int phandle,
	int& send_queue_len,
	int& recv_queue_len,
	int& ping,
	int& kbps_sent,
	int& local_frames_behind,
	int& remote_frames_behind)
{
	CallLogv("DllGetNetworkStats %i", phandle);
	if (ggpo == 1) {
		GGPONetworkStats stats;
		auto result = ggpo_get_network_stats(inst1.session, phandle, &stats);
		send_queue_len = stats.network.send_queue_len;
		recv_queue_len = stats.network.recv_queue_len;
		ping = stats.network.ping;
		kbps_sent = stats.network.kbps_sent;
		local_frames_behind = stats.timesync.local_frames_behind;
		remote_frames_behind = stats.timesync.remote_frames_behind;
		return result;
	}
	else {
		send_queue_len = 1;
		recv_queue_len = 2;
		ping = 3;
		kbps_sent = 4;
		local_frames_behind = 5;
		remote_frames_behind = 6;
		return 0;
	}
}
