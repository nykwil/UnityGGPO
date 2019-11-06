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

	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggSetLogDelegate(LogDelegate callback);
	GGPOPtr UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggStartSession(
		BeginGameDelegate beginGame,
		AdvanceFrameDelegate advanceFrame,
		LoadGameStateDelegate loadGameState,
		LogGameStateDelegate logGameState,
		SaveGameStateDelegate saveGameState,
		FreeBufferDelegate freeBuffer,
		OnEventDelegate onEvent,
		const char* game, int num_players, int localport);
	GGPOPtr UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggStartSpectating(
		BeginGameDelegate beginGame,
		AdvanceFrameDelegate advanceFrame,
		LoadGameStateDelegate loadGameState,
		LogGameStateDelegate logGameState,
		SaveGameStateDelegate saveGameState,
		FreeBufferDelegate freeBuffer,
		OnEventDelegate onEvent,
		const char* game, int num_players, int localport, char* host_ip, int host_port);
	int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggSetDisconnectNotifyStart(GGPOPtr ggpo, int timeout);
	int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggSetDisconnectTimeout(GGPOPtr ggpo, int timeout);
	int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggSynchronizeInput(GGPOPtr ggpo, uint64_t* inputs, int length, int& disconnect_flags);
	int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggAddLocalInput(GGPOPtr ggpo, int local_player_handle, uint64_t input);
	int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggCloseSession(GGPOPtr ggpo);
	int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggIdle(GGPOPtr ggpo, int timeout);
	int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggAddPlayer(GGPOPtr ggpo,
		int player_type,
		int player_num,
		const char* player_ip_address,
		unsigned short player_port,
		int& phandle);
	int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggDisconnectPlayer(GGPOPtr ggpo, int phandle);
	int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggSetFrameDelay(GGPOPtr ggpo, int phandle, int frame_delay);
	int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggAdvanceFrame(GGPOPtr ggpo);
	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggLog(GGPOPtr ggpo, const char* v);
	int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggGetNetworkStats(GGPOPtr ggpo, int phandle,
		int& send_queue_len,
		int& recv_queue_len,
		int& ping,
		int& kbps_sent,
		int& local_frames_behind,
		int& remote_frames_behind);
}
