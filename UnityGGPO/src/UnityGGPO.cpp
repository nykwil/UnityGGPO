#include <stdio.h>
#include <vector>
#include <string>
#include "ggponet.h"
#include "UnityGGPO.h"
#include <memory>
#include <iostream>
#include <string>
#include <cstdio>

constexpr auto PLUGIN_VERSION = "1.0.0.0";
constexpr auto PLUGIN_BUILD_NUMBER = 1;
int logLevel = 1; // 0 == nothing // 1 == important things // 2 == verbose // 3 == everything

const int LOG_TESTS = 0;
const int LOG_INFO = 1;
const int LOG_DETAILS = 2;
const int LOG_VERBOSE = 3;

LogDelegate uggLogCallback = nullptr;

void UggCallLog(int level, const char* text)
{
    if (logLevel >= level && uggLogCallback) {
        uggLogCallback(text);
    }
}

template<typename ... Args>
void UggCallLogv(int level, const char* format, Args ... args)
{
    if (logLevel >= level && uggLogCallback) {
        size_t size = snprintf(nullptr, 0, format, args ...) + 1; // Extra space for '\0'
        std::unique_ptr<char[]> buf(new char[size]);
        snprintf(buf.get(), size, format, args ...);
        UggCallLog(level, buf.get());
    }
}

PLUGINEX(const char*) UggPluginVersion()
{
    return PLUGIN_VERSION;
}

PLUGINEX(int) UggPluginBuildNumber()
{
    return PLUGIN_BUILD_NUMBER;
}

PLUGINEX(void) UggSetLogDelegate(LogDelegate callback)
{
    uggLogCallback = callback;
}

void TestOnEventDelegate(OnEventDelegate realOnEventCallback)
{
    UggCallLogv(LOG_INFO, "UggTestOnEventDelegate");

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

void TestAllDelegates(const GGPOSessionCallbacks& cb) {
    unsigned char* data;
    int length;
    int checksum;

    cb.advance_frame(1);
    cb.begin_game("Test");
    cb.save_game_state(&data, &length, &checksum, 1);
    cb.load_game_state(data, length);
    cb.log_game_state("", data, length);
    cb.free_buffer(data);
    TestOnEventDelegate(cb.on_event);
}

PLUGINEX(int) UggTestStartSession(GGPOPtr& sessionRef,
    BeginGameDelegate beginGame,
    AdvanceFrameDelegate advanceFrame,
    LoadGameStateDelegate loadGameState,
    LogGameStateDelegate logGameState,
    SaveGameStateDelegate saveGameState,
    FreeBufferDelegate freeBuffer,
    OnEventDelegate onEvent,
    const char* game, int num_players, int localport)
{
    UggCallLogv(LOG_TESTS, "UggTestStartSession - %s %i %i", game, num_players, localport);
    GGPOSessionCallbacks cb;
    cb.advance_frame = advanceFrame;
    cb.load_game_state = loadGameState;
    cb.begin_game = beginGame;
    cb.save_game_state = saveGameState;
    cb.load_game_state = loadGameState;
    cb.log_game_state = logGameState;
    cb.free_buffer = freeBuffer;
    cb.on_event = onEvent;

    TestAllDelegates(cb);
    GGPOSession* ggpo;
    auto ret = ggpo_start_session(&ggpo, &cb, game, num_players, sizeof(uint64_t), localport);
    sessionRef = (GGPOPtr)ggpo;
    return ret;
}

PLUGINEX(int) UggStartSession(GGPOPtr& sessionRef,
    BeginGameDelegate beginGame,
    AdvanceFrameDelegate advanceFrame,
    LoadGameStateDelegate loadGameState,
    LogGameStateDelegate logGameState,
    SaveGameStateDelegate saveGameState,
    FreeBufferDelegate freeBuffer,
    OnEventDelegate onEvent,
    const char* game, int num_players, int localport)
{
    UggCallLogv(LOG_INFO, "UggStartSession - %s %i %i", game, num_players, localport);
    GGPOSessionCallbacks cb;
    cb.advance_frame = advanceFrame;
    cb.load_game_state = loadGameState;
    cb.begin_game = beginGame;
    cb.save_game_state = saveGameState;
    cb.load_game_state = loadGameState;
    cb.log_game_state = logGameState;
    cb.free_buffer = freeBuffer;
    cb.on_event = onEvent;

    GGPOSession* ggpo;
    auto ret = ggpo_start_session(&ggpo, &cb, game, num_players, sizeof(uint64_t), localport);
    sessionRef = (GGPOPtr)ggpo;
    return ret;
}

PLUGINEX(int) UggStartSpectating(GGPOPtr& sessionRef,
    BeginGameDelegate beginGame,
    AdvanceFrameDelegate advanceFrame,
    LoadGameStateDelegate loadGameState,
    LogGameStateDelegate logGameState,
    SaveGameStateDelegate saveGameState,
    FreeBufferDelegate freeBuffer,
    OnEventDelegate onEvent,
    const char* game, int num_players, int localport, char* host_ip, int host_port)
{
    UggCallLogv(LOG_INFO, "UggStartSpectating - %s %i %i %s %i", game, num_players, localport, host_ip, host_port);
    GGPOSessionCallbacks cb;
    cb.advance_frame = advanceFrame;
    cb.load_game_state = loadGameState;
    cb.begin_game = beginGame;
    cb.save_game_state = saveGameState;
    cb.load_game_state = loadGameState;
    cb.log_game_state = logGameState;
    cb.free_buffer = freeBuffer;
    cb.on_event = onEvent;

    GGPOSession* ggpo;
    auto ret = ggpo_start_spectating(&ggpo, &cb, game, num_players, sizeof(uint64_t), localport, host_ip, host_port);
    return ret;
}

PLUGINEX(int) UggSetDisconnectNotifyStart(GGPOPtr ggpo, int timeout)
{
    UggCallLog(LOG_INFO, "UggSetDisconnectNotifyStart");
    return ggpo_set_disconnect_notify_start((GGPOSession*)ggpo, timeout);
}

PLUGINEX(int) UggSetDisconnectTimeout(GGPOPtr ggpo, int timeout)
{
    UggCallLog(LOG_INFO, "UggSetDisconnectTimeout");
    return ggpo_set_disconnect_timeout((GGPOSession*)ggpo, timeout);
}

PLUGINEX(int) UggSynchronizeInput(GGPOPtr ggpo, uint64_t* inputs, int length, int& disconnect_flags)
{
    UggCallLog(LOG_VERBOSE, "UggSynchronizeInput");
    return ggpo_synchronize_input((GGPOSession*)ggpo, inputs, sizeof(uint64_t) * length, &disconnect_flags);
}

PLUGINEX(int) UggAddLocalInput(GGPOPtr ggpo, int local_player_handle, uint64_t input)
{
    UggCallLog(LOG_VERBOSE, "UggAddLocalInput");
    return ggpo_add_local_input((GGPOSession*)ggpo, local_player_handle, &input, sizeof(uint64_t));
}

PLUGINEX(int) UggCloseSession(GGPOPtr ggpo)
{
    UggCallLog(LOG_INFO, "UggCloseSession");
    return ggpo_close_session((GGPOSession*)ggpo);
}

PLUGINEX(int) UggIdle(GGPOPtr ggpo, int timeout)
{
    UggCallLog(LOG_VERBOSE, "UggIdle");
    return ggpo_idle((GGPOSession*)ggpo, timeout);
}

PLUGINEX(int) UggAddPlayer(GGPOPtr ggpo,
    int player_type,
    int player_num,
    const char* player_ip_address,
    unsigned short player_port,
    int& phandle)
{
    UggCallLogv(LOG_INFO, "UggAddPlayer %d %d %s %d", player_type, player_num, player_ip_address, player_port);
    GGPOPlayer player;
    player.size = sizeof(GGPOPlayer);
    player.type = (GGPOPlayerType)player_type;
    player.player_num = player_num;
    strcpy_s(player.u.remote.ip_address, player_ip_address);
    player.u.remote.port = player_port;
    return ggpo_add_player((GGPOSession*)ggpo, &player, &phandle);
}

PLUGINEX(int) UggDisconnectPlayer(GGPOPtr ggpo, int phandle)
{
    UggCallLog(LOG_INFO, "UggDisconnectPlayer");
    return ggpo_disconnect_player((GGPOSession*)ggpo, phandle);
}

PLUGINEX(int) UggSetFrameDelay(GGPOPtr ggpo, int phandle, int frame_delay)
{
    UggCallLog(LOG_INFO, "UggSetFrameDelay");
    return ggpo_set_frame_delay((GGPOSession*)ggpo, phandle, frame_delay);
}

PLUGINEX(int) UggAdvanceFrame(GGPOPtr ggpo)
{
    UggCallLog(LOG_VERBOSE, "UggAdvanceFrame");
    return ggpo_advance_frame((GGPOSession*)ggpo);
}

PLUGINEX(void) UggLog(GGPOPtr ggpo, const char* text)
{
    UggCallLogv(LOG_INFO, "UggLog %s", text);
    ggpo_log((GGPOSession*)ggpo, text);
}

PLUGINEX(int) UggGetNetworkStats(GGPOPtr ggpo, int phandle,
    int& send_queue_len,
    int& recv_queue_len,
    int& ping,
    int& kbps_sent,
    int& local_frames_behind,
    int& remote_frames_behind)
{
    UggCallLogv(LOG_VERBOSE, "UggGetNetworkStats %i", phandle);
    GGPONetworkStats stats;
    auto result = ggpo_get_network_stats((GGPOSession*)ggpo, phandle, &stats);
    send_queue_len = stats.network.send_queue_len;
    recv_queue_len = stats.network.recv_queue_len;
    ping = stats.network.ping;
    kbps_sent = stats.network.kbps_sent;
    local_frames_behind = stats.timesync.local_frames_behind;
    remote_frames_behind = stats.timesync.remote_frames_behind;
    return result;
}