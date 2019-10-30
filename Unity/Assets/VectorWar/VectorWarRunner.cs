using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VectorWar {

    [Serializable]
    public class Connections {
        public short port;
        public string ip;
        public bool spectator;
        public bool local;
    }

    public class VectorWarRunner : MonoBehaviour {
        public List<Connections> connections;

        public GameState gs;
        public NonGameState ngs;
        public GGPOPerformance perf;

        float next;
        VectorWar VectorWar;

        UGGPO.LogDelegate logDelegate;

        public bool Running { get; set; }

        public void LogCallback(string text) {
            Debug.Log("Log: " + text);
        }

        [Button]
        public void Startup() {
            gs = new GameState();
            ngs = new NonGameState();
            logDelegate = new UGGPO.LogDelegate(LogCallback);
            UGGPO.UggSetLogDelegate(logDelegate);
            VectorWar = new VectorWar(gs, ngs, perf);
            var remote_index = -1;
            var num_spectators = 0;
            var num_players = 0;
            var player_index = -1;
            for (int i = 0; i < connections.Count; ++i) {
                if (connections[i].local) {
                    player_index = i;
                }
                else if (remote_index == -1) {
                    remote_index = i;
                }

                if (connections[i].spectator) {
                    ++num_spectators;
                }
                else {
                    ++num_players;
                }
            }
            if (connections[player_index].spectator) {
                VectorWar.InitSpectator(connections[player_index].port, num_players, connections[remote_index].ip, connections[remote_index].port);
            }
            else {
                var players = new List<GGPOPlayer>();
                for (int i = 0; i < connections.Count; ++i) {
                    var player = new GGPOPlayer {
                        player_num = players.Count + 1,
                        ip_address = connections[remote_index].ip,
                        port = connections[remote_index].port
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
            Running = true;
        }

        [Button]
        public void DisconnectPlayer(int player) {
            if (Running) {
                VectorWar.DisconnectPlayer(player);
            }
        }

        [Button]
        public void Close() {
            if (Running) {
                UGGPO.UggSetLogDelegate(null);
                VectorWar.Exit();
                Running = false;
            }
        }

        private void OnDestroy() {
        }

        void Update() {
            if (Running) {
                var now = Time.time;
                VectorWar.Idle(Mathf.Max(0, (int)((next - now) * 1000f) - 1));

                if (now >= next) {
                    VectorWar.RunFrame();
                    next = now + 1f / 60f;
                }
            }
        }
    }
}
