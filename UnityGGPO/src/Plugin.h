#pragma once
#include <IUnityInterface.h>

typedef void (*LogDelegate)(const char* string);
typedef bool (*BeginGameDelegate)(const char* string);
typedef bool (*AdvanceFrameDelegate)(int flags);
typedef bool (*LoadGameStateDelegate)(unsigned char* buffer, int length);
typedef bool (*LogGameStateDelegate)(char* string, unsigned char* buffer, int length);
typedef void* (*SaveGameStateDelegate)(int& length, int& checksum, int frame);
typedef bool (*RealSaveGameStateDelegate)(unsigned char** buffer, int* len, int* checksum, int frame);
typedef void (*FreeBufferDelegate)(void* buffer);
typedef bool (*OnEventConnectedToPeerDelegate)(int connected_player);
typedef bool (*OnEventSynchronizingWithPeerDelegate)(int synchronizing_player, int synchronizing_count, int synchronizing_total);
typedef bool (*OnEventSynchronizedWithPeerDelegate)(int synchronizing_player);
typedef bool (*OnEventRunningDelegate)();
typedef bool (*OnEventConnectionInterruptedDelegate)(int connection_interrupted_player, int connection_interrupted_disconnect_timeout);
typedef bool (*OnEventConnectionResumedDelegate)(int connection_resumed_player);
typedef bool (*OnEventDisconnectedFromPeerDelegate)(int disconnected_player);
typedef bool (*OnEventTimesyncDelegate)(int timesync_frames_ahead);
typedef bool (*RealOnEventDelegate)(GGPOEvent* info);

class GGPOInstance {
public:
	GGPOSession* session;
	GGPOSessionCallbacks cb;

//	LoadGameStateDelegate loadGameStateCb;
//	LogGameStateDelegate logGameStateCb;
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
};
extern "C" const char UNITY_INTERFACE_EXPORT * UNITY_INTERFACE_API DllPluginVersion();
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllPluginBuildNumber(); 
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllSetLogDelegate(LogDelegate callback);
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllTestRealSaveGameDelegate(RealSaveGameStateDelegate callback, FreeBufferDelegate freeBufferCallback);
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllTestRealOnEventDelegate(RealOnEventDelegate realOnEventCallback);
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllTestGameStateDelegates(
	SaveGameStateDelegate saveGameState,
	LogGameStateDelegate logGameState,
	LoadGameStateDelegate loadGameState,
	FreeBufferDelegate freeBuffer);
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
	const char* game, int num_players, int localport);
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
	const char* game, int num_players, int localport);
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
	const char* game, int num_players, int localport, char* host_ip, int host_port);
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllSetDisconnectNotifyStart(int ggpo, int timeout);
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllSetDisconnectTimeout(int ggpo, int timeout);
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllSynchronizeInput(int ggpo, unsigned long long* inputs, int length, int& disconnect_flags);
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllAddLocalInput(int ggpo, int local_player_handle, unsigned long long input);
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllCloseSession(int ggpo);
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllIdle(int ggpo, int timeout); 
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllAddPlayer(int ggpo,
	int player_type,
	int player_num,
	const char* player_ip_address,
	short player_port,
	int& phandle);
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllDisconnectPlayer(int ggpo, int phandle);
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllSetFrameDelay(int ggpo, int phandle, int frame_delay);
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllAdvanceFrame(int ggpo);
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllLog(int ggpo, const char* v);
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllGetNetworkStats(int ggpo, int phandle,
	int& send_queue_len,
	int& recv_queue_len,
	int& ping,
	int& kbps_sent,
	int& local_frames_behind,
	int& remote_frames_behind);

