#pragma once
#include <IUnityInterface.h>

typedef void (*LogDelegate)(const char* text);
typedef bool (*BeginGameDelegate)(const char* text);
typedef bool (*AdvanceFrameDelegate)(int flags);
typedef bool (*LoadGameStateDelegate)(unsigned char* buffer, int length);
typedef bool (*LogGameStateDelegate)(char* text, unsigned char* buffer, int length);
typedef bool (*SaveGameStateDelegate)(unsigned char** buffer, int* len, int* checksum, int frame);
typedef void (*FreeBufferDelegate)(void* buffer);
typedef bool (*OnEventDelegate)(GGPOEvent* info);

extern "C" const char UNITY_INTERFACE_EXPORT * UNITY_INTERFACE_API UggPluginVersion();
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggPluginBuildNumber();
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggSetLogDelegate(LogDelegate callback);
extern "C" GGPOSession UNITY_INTERFACE_EXPORT * UNITY_INTERFACE_API UggStartSession(
	BeginGameDelegate beginGame,
	AdvanceFrameDelegate advanceFrame,
	LoadGameStateDelegate loadGameState,
	LogGameStateDelegate logGameState,
	SaveGameStateDelegate saveGameState,
	FreeBufferDelegate freeBuffer,
	OnEventDelegate onEvent,
	const char* game, int num_players, int localport);
	extern "C" GGPOSession UNITY_INTERFACE_EXPORT * UNITY_INTERFACE_API UggStartSpectating(
		BeginGameDelegate beginGame,
		AdvanceFrameDelegate advanceFrame,
		LoadGameStateDelegate loadGameState,
		LogGameStateDelegate logGameState,
		SaveGameStateDelegate saveGameState,
		FreeBufferDelegate freeBuffer,
		OnEventDelegate onEvent,
		const char* game, int num_players, int localport, char* host_ip, int host_port);
	extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggSetDisconnectNotifyStart(GGPOSession * ggpo, int timeout);
	extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggSetDisconnectTimeout(GGPOSession * ggpo, int timeout);
	extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggSynchronizeInput(GGPOSession * ggpo, uint64_t * inputs, int length, int& disconnect_flags);
	extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggAddLocalInput(GGPOSession * ggpo, int local_player_handle, uint64_t input);
	extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggCloseSession(GGPOSession * ggpo);
	extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggIdle(GGPOSession * ggpo, int timeout);
	extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggAddPlayer(GGPOSession * ggpo,
		int player_type,
		int player_num,
		const char* player_ip_address,
		short player_port,
		int& phandle);
	extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggDisconnectPlayer(GGPOSession * ggpo, int phandle);
	extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggSetFrameDelay(GGPOSession * ggpo, int phandle, int frame_delay);
	extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggAdvanceFrame(GGPOSession * ggpo);
	extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggLog(GGPOSession * ggpo, const char* v);
	extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UggGetNetworkStats(GGPOSession * ggpo, int phandle,
		int& send_queue_len,
		int& recv_queue_len,
		int& ping,
		int& kbps_sent,
		int& local_frames_behind,
		int& remote_frames_behind);
