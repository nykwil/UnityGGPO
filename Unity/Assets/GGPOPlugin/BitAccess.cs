public class BitAccess {

    public struct Section {
        public int pos;
        public int length;
        public ulong mask;

        public Section(int pos, int length) {
            this.pos = pos;
            this.length = length;
            this.mask = GetMask(pos, length);
        }
    }

    public static Section GetSection(ulong maxValue) {
        return new Section(GetSize(maxValue), 0);
    }

    public static Section GetSection(ulong maxValue, Section prev) {
        return new Section(GetSize(maxValue), prev.pos + prev.length);
    }

    static public int GetSize(ulong value) {
        var i = 1;
        while ((1ul << i) <= value) {
            ++i;
        }
        return i;
    }

    static ulong GetMask(int start, int length) {
        return ((1ul << length) - 1) << start;
    }

    public ulong Get(int start, int length) {
        var mask = GetMask(start, length);
        var d = data & mask;
        return d >> start;
    }

    public ulong Get(Section s) {
        return Get(s.pos, s.length);
    }

    public void Set(int start, int length, int value) {
        var nmask = ~GetMask(start, length);
        data = (data & nmask) | ((ulong)value << start);
    }

    public void Set(Section s, int value) {
        Set(value, s.pos, s.length);
    }

    public ulong data;
}

/*extern "C" const char UNITY_INTERFACE_EXPORT * UNITY_INTERFACE_API GetPluginVersion();
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetPluginBuildNumber();
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetLogDelegate(LogDelegate callback);
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllTestLogGameStateDelegate(LogGameStateDelegate callback);
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllTestFreeGameStateDelegate(LogGameStateDelegate callback);
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllTestSaveGameStateDelegate(SaveGameStateDelegate callback);
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
	const char* game, int num_players, int input_size, int localport);
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
	const char* game, int num_players, int input_size, int localport, const char* host_ip, int host_port);
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllSetDisconnectNotifyStart(int ggpo, int timeout) {
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllSetDisconnectTimeout(int ggpo, int timeout) {
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllSynchronizeInput(int ggpo, unsigned long long* inputs, int length, int& disconnect_flags) {
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllAddLocalInput(int ggpo, int local_player_handle, unsigned long long input, int length) {
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllCloseSession(int ggpo) {
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllIdle(int ggpo, int timeout) {
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllAddPlayer(int ggpo,
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllDisconnectPlayer(int ggpo, int phandle) {
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllSetFrameDelay(int ggpo, int phandle, int frame_delay) {
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllAdvanceFrame(int ggpo) {
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllLog(int ggpo, const char* v) {
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API DllGetNetworkStats(int ggpo, int phandle,
*/
