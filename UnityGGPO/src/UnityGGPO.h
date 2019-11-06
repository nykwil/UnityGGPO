#pragma once
#include <IUnityInterface.h>

extern "C" {
#define PLUGINEX(rtype) UNITY_INTERFACE_EXPORT rtype UNITY_INTERFACE_API

	typedef void (*LogDelegate)(const char* text);
	typedef bool (*BeginGameDelegate)(const char* text);
	typedef bool (*AdvanceFrameDelegate)(int flags);
	typedef bool (*LoadGameStateDelegate)(unsigned char* buffer, int length);
	typedef bool (*LogGameStateDelegate)(char* text, unsigned char* buffer, int length);
	typedef bool (*SaveGameStateDelegate)(unsigned char** buffer, int* len, int* checksum, int frame);
	typedef void (*FreeBufferDelegate)(void* buffer);
	typedef bool (*OnEventDelegate)(GGPOEvent* info);
	typedef intptr_t GGPOPtr;

	PLUGINEX(const char*) UggPluginVersion();
	PLUGINEX(int) UggPluginBuildNumber();
	PLUGINEX(int) UggTestStartSession(GGPOPtr& sessionRef,
		BeginGameDelegate beginGame,
		AdvanceFrameDelegate advanceFrame,
		LoadGameStateDelegate loadGameState,
		LogGameStateDelegate logGameState,
		SaveGameStateDelegate saveGameState,
		FreeBufferDelegate freeBuffer,
		OnEventDelegate onEvent,
		const char* game, int num_players, int localport);
	PLUGINEX(void) UggSetLogDelegate(LogDelegate callback);
	PLUGINEX(int) UggStartSession(GGPOPtr& sessionRef,
		BeginGameDelegate beginGame,
		AdvanceFrameDelegate advanceFrame,
		LoadGameStateDelegate loadGameState,
		LogGameStateDelegate logGameState,
		SaveGameStateDelegate saveGameState,
		FreeBufferDelegate freeBuffer,
		OnEventDelegate onEvent,
		const char* game, int num_players, int localport);
	PLUGINEX(int) UggStartSpectating(GGPOPtr& sessionRef,
		BeginGameDelegate beginGame,
		AdvanceFrameDelegate advanceFrame,
		LoadGameStateDelegate loadGameState,
		LogGameStateDelegate logGameState,
		SaveGameStateDelegate saveGameState,
		FreeBufferDelegate freeBuffer,
		OnEventDelegate onEvent,
		const char* game, int num_players, int localport, char* host_ip, int host_port);
	PLUGINEX(int) UggSetDisconnectNotifyStart(GGPOPtr ggpo, int timeout);
	PLUGINEX(int) UggSetDisconnectTimeout(GGPOPtr ggpo, int timeout);
	PLUGINEX(int) UggSynchronizeInput(GGPOPtr ggpo, uint64_t* inputs, int length, int& disconnect_flags);
	PLUGINEX(int) UggAddLocalInput(GGPOPtr ggpo, int local_player_handle, uint64_t input);
	PLUGINEX(int) UggCloseSession(GGPOPtr ggpo);
	PLUGINEX(int) UggIdle(GGPOPtr ggpo, int timeout);
	PLUGINEX(int) UggAddPlayer(GGPOPtr ggpo,
		int player_type,
		int player_num,
		const char* player_ip_address,
		unsigned short player_port,
		int& phandle);
	PLUGINEX(int) UggDisconnectPlayer(GGPOPtr ggpo, int phandle);
	PLUGINEX(int) UggSetFrameDelay(GGPOPtr ggpo, int phandle, int frame_delay);
	PLUGINEX(int) UggAdvanceFrame(GGPOPtr ggpo);
	PLUGINEX(void) UggLog(GGPOPtr ggpo, const char* text);
	PLUGINEX(int) UggGetNetworkStats(GGPOPtr ggpo, int phandle,
		int& send_queue_len,
		int& recv_queue_len,
		int& ping,
		int& kbps_sent,
		int& local_frames_behind,
		int& remote_frames_behind);
}
