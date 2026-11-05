#include "gekkonet.h"
#include <cstddef>
#include <iostream>
#define SIZE(T,N) static_assert(sizeof(T)==N, #T " size")
#define OFFSET(T,F,N) static_assert(offsetof(T,F)==N, #T "." #F)
SIZE(bool,1); SIZE(GekkoSessionType,4); SIZE(GekkoPlayerType,4); SIZE(GekkoGameEventType,4); SIZE(GekkoSessionEventType,4);
SIZE(GekkoConfig,24);
OFFSET(GekkoConfig,num_players,0); OFFSET(GekkoConfig,max_spectators,1); OFFSET(GekkoConfig,max_input_prediction_window,2);
OFFSET(GekkoConfig,spectator_delay,4); OFFSET(GekkoConfig,input_size,8); OFFSET(GekkoConfig,state_size,12);
OFFSET(GekkoConfig,limited_saving,16); OFFSET(GekkoConfig,desync_detection,17); OFFSET(GekkoConfig,check_distance,20);
SIZE(GekkoNetAddress,16); OFFSET(GekkoNetAddress,data,0); OFFSET(GekkoNetAddress,size,8);
SIZE(GekkoNetResult,32); OFFSET(GekkoNetResult,addr,0); OFFSET(GekkoNetResult,data_len,16); OFFSET(GekkoNetResult,data,24);
SIZE(GekkoNetAdapter,24); OFFSET(GekkoNetAdapter,send_data,0); OFFSET(GekkoNetAdapter,receive_data,8); OFFSET(GekkoNetAdapter,free_data,16);
SIZE(GekkoGameEvent,40); OFFSET(GekkoGameEvent,type,0); OFFSET(GekkoGameEvent,data,8);
using GameData = decltype(GekkoGameEvent::data);
SIZE(GameData,32);
using Advance = decltype(GameData::adv); using Save = decltype(GameData::save); using Load = decltype(GameData::load);
SIZE(Advance,24); OFFSET(Advance,frame,0); OFFSET(Advance,input_len,4); OFFSET(Advance,inputs,8);
OFFSET(Advance,rolling_back,16); OFFSET(Advance,running_ahead,17);
SIZE(Save,32); OFFSET(Save,frame,0); OFFSET(Save,portable,4); OFFSET(Save,checksum,8); OFFSET(Save,state_len,16); OFFSET(Save,state,24);
SIZE(Load,16); OFFSET(Load,frame,0); OFFSET(Load,state_len,4); OFFSET(Load,state,8);
SIZE(GekkoSessionEvent,20); OFFSET(GekkoSessionEvent,type,0); OFFSET(GekkoSessionEvent,data,4);
using SessionData = decltype(GekkoSessionEvent::data);
SIZE(SessionData,16);
SIZE(GekkoNetworkStats,20); OFFSET(GekkoNetworkStats,kb_sent,0); OFFSET(GekkoNetworkStats,kb_received,4);
OFFSET(GekkoNetworkStats,last_ping,8); OFFSET(GekkoNetworkStats,avg_ping,12); OFFSET(GekkoNetworkStats,jitter,16);
SIZE(GekkoRollbackStats,20); OFFSET(GekkoRollbackStats,total_count,0); OFFSET(GekkoRollbackStats,last_frames,4);
OFFSET(GekkoRollbackStats,avg_frames,8); OFFSET(GekkoRollbackStats,max_frames,12); OFFSET(GekkoRollbackStats,total_stalls,16);
static_assert(GekkoGameSession==0 && GekkoStressSession==1 && GekkoSpectateSession==2 && GekkoReplaySession==3);
static_assert(GekkoLocalPlayer==0 && GekkoRemotePlayer==1 && GekkoSpectator==2);
static_assert(GekkoEmptyGameEvent==-1 && GekkoAdvanceEvent==0 && GekkoSaveEvent==1 && GekkoLoadEvent==2);
static_assert(GekkoEmptySessionEvent==-1 && GekkoPlayerSyncing==0 && GekkoPlayerConnected==1 && GekkoPlayerDisconnected==2 &&
    GekkoSessionStarted==3 && GekkoSpectatorPaused==4 && GekkoSpectatorUnpaused==5 && GekkoDesyncDetected==6 &&
    GekkoReplayFinished==7 && GekkoReplayDesync==8);
int main() { std::cout << "PASS pinned Windows x64 C ABI sizes, offsets, unions, enums and bools\n"; }
