using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class VectorWarRunner : MonoBehaviour {

    [Serializable]
    public class Connections {
        public short port;
        public string ip;
        public bool spectator;
    }

    public List<Connections> connections;

    public GameState gs;
    public NonGameState ngs;
    public GGPOPerformance perf;

    public int player_index;

    float next;
    VectorWar VectorWar;

    GGPO.LogDelegate logDelegate;

    bool running = false;

    public void LogCallback(string text) {
        Debug.Log("Log: " + text);
    }

    [Button]
    public void Startup() {
        gs = new GameState();
        ngs = new NonGameState();
        logDelegate = new GGPO.LogDelegate(LogCallback);
        GGPO.DllSetLogDelegate(logDelegate);
        VectorWar = new VectorWar(gs, ngs, perf);
        var host_index = -1;
        var num_spectators = 0;
        var num_players = 0;
        for (int i = 0; i < connections.Count; ++i) {
            if (host_index == -1 && i != player_index) {
                host_index = i;
            }
            if (connections[i].spectator) {
                ++num_spectators;
            }
            else {
                ++num_players;
            }
        }
        if (connections[player_index].spectator) {
            VectorWar.InitSpectator(connections[player_index].port, num_players, connections[host_index].ip, connections[host_index].port);
        }
        else {
            var players = new List<GGPOPlayer>();
            for (int i = 0; i < connections.Count; ++i) {
                var player = new GGPOPlayer {
                    player_num = players.Count + 1,
                    ip_address = connections[host_index].ip,
                    port = connections[host_index].port
                };
                if (player_index == i) {
                    player.type = GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL;
                }
                else if (connections[i].spectator) {
                    player.type = GGPOPlayerType.GGPO_PLAYERTYPE_SPECTATOR;
                }
                else {
                    player.type = GGPOPlayerType.GGPO_PLAYERTYPE_REMOTE;
                }
                players.Add(player);
            }
            VectorWar.Init(connections[player_index].port, num_players, players, num_spectators);
        }
        running = true;
    }

    [Button]
    public void DisconnectPlayer(int player) {
        if (running) {
            VectorWar.DisconnectPlayer(player);
        }
    }

    [Button]
    public void Close() {
        if (running) {
            GGPO.DllSetLogDelegate(null);
            VectorWar.Exit();
            running = false;
        }
    }

    private void OnDestroy() {
    }

    void Update() {
        if (running) {
            var now = Time.time;
            VectorWar.Idle(Mathf.Max(0, (int)((next - now) * 1000f) - 1));

            if (now >= next) {
                VectorWar.RunFrame();
                next = now + 1f / 60f;
            }
        }
    }
}
