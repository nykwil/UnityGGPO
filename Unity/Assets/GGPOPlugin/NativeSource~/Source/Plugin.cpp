#include <stdio.h>
#include <IUnityInterface.h>
#include <sstream>

extern "C" const char UNITY_INTERFACE_EXPORT * UNITY_INTERFACE_API GetPluginVersion() {
	//This is defined in CMAKE and passed to the source.
	return PLUGIN_VERSION;
}
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetPluginBuildNumber() {
	//This is defined in CMAKE and passed to the source.
	return PLUGIN_BUILD_NUMBER;
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetTwo() {
	return 2;
}

typedef void (*CALLBACK)(int result);
extern "C" bool UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API PassCallback(CALLBACK callback) {
	if (!callback) {
		return false;
	}
	callback(5);
	return true;
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API FillWithOnes(int* array, int length) {
	for (int i = 0; i < length; i++) {
		array[i] = 1;
	}
}

typedef void (*LogDelegate)(const char* string);

typedef bool (*BeginGameDelegate)();
typedef bool (*AdvanceFrameDelegate)(int flags);
typedef bool (*LoadGameStateDelegate)(unsigned char* buffer, int length);
typedef bool (*LogGameStateDelegate)(unsigned char* buffer, int length);
typedef void* (*SaveGameStateDelegate)(int& length, int& checksum, int frame);
typedef bool (*OnEventConnectedToPeerDelegate)(int connected_player);
typedef bool (*OnEventSynchronizingWithPeerDelegate)(int synchronizing_player, int synchronizing_count, int synchronizing_total);
typedef bool (*OnEventSynchronizedWithPeerDelegate)(int synchronizing_player);
typedef bool (*OnEventRunningDelegate)();
typedef bool (*OnEventConnectionInterruptedDelegate)(int connection_interrupted_player, int connection_interrupted_disconnect_timeout);
typedef bool (*OnEventConnectionResumedDelegate)(int connection_resumed_player);
typedef bool (*OnEventDisconnectedFromPeerDelegate)(int disconnected_player);
typedef bool (*OnEventEventcodeTimesyncDelegate)(int timesync_frames_ahead);

LogDelegate logCallback;

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetLogDelegate(LogDelegate callback) {
	logCallback = callback;
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API TestLogGameStateDelegate(LogGameStateDelegate callback) {
	unsigned char* buffer = new unsigned char[5];
	buffer[0] = 1;
	buffer[1] = 2;
	buffer[2] = 3;
	buffer[3] = 4;
	buffer[4] = 5;
	callback(buffer, 5);
	logCallback("Callback complete");
	delete[] buffer;
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API TestSaveGameStateDelegate(SaveGameStateDelegate callback) {
	int length;
	int checksum;
	unsigned char* buffer = (unsigned char*)callback(length, checksum, 1);
	std::stringstream ss;
	ss << "Frame: " << length << " " << checksum;
	for (int i = 0; i < length; ++i) {
		ss << buffer[i] << ",";
	}
	logCallback(ss.str().c_str());
}
