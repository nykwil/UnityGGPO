using System;
using System.Collections.Generic;
using UnityEngine;

namespace VectorWar {

    [Serializable]
    public class Connections {
        public ushort port;
        public string ip;
        public bool spectator;
    }

    public class VectorWarRunner : MonoBehaviour {
        public List<Connections> connections;

        public GameState gs;
        public NonGameState ngs;
        public GGPOPerformance perf;

        public ShipView shipPrefab;
        public Transform bulletPrefab;
        public int editorPlayerIndex = 0;
        public int otherPlayerIndex = 1;
        public bool localMode = true;

        ShipView[] shipViews = Array.Empty<ShipView>();
        Transform[][] bulletLists;
        float next;

        public bool Running { get; set; }

        public static event Action<string> OnStatus = (string s) => { };
        public static event Action<string> OnPeriodicChecksum = (string s) => { };
        public static event Action<string> OnNowChecksum = (string s) => { };
        public static event Action<string> OnLog = (string s) => { };

        public static void LogCallback(string text) {
            OnLog(text);
        }

        public void Startup() {
            gs = new GameState();
            ngs = new NonGameState();
            if (localMode) {
                InitLocal();
            }
            else {
                InitRemote();
            }

            Running = true;
        }

        void InitRemote() {
            GGPO.UggSetLogDelegate(LogCallback);
            VectorWar.Init(gs, ngs, perf);

            var remote_index = -1;
            var num_spectators = 0;
            var num_players = 0;

            var player_index = Application.isEditor ? editorPlayerIndex : otherPlayerIndex;
            OnLog("Player Index: " + player_index);
            for (int i = 0; i < connections.Count; ++i) {
                if (i != player_index && remote_index == -1) {
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
                    };
                    if (player_index == i) {
                        player.type = GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL;
                        player.ip_address = "";
                        player.port = 0;
                    }
                    else if (connections[i].spectator) {
                        player.type = GGPOPlayerType.GGPO_PLAYERTYPE_SPECTATOR;
                        player.ip_address = connections[remote_index].ip;
                        player.port = connections[remote_index].port;
                    }
                    else {
                        player.type = GGPOPlayerType.GGPO_PLAYERTYPE_REMOTE;
                        player.ip_address = connections[remote_index].ip;
                        player.port = connections[remote_index].port;
                    }
                    players.Add(player);
                }
                VectorWar.Init(connections[player_index].port, num_players, players, num_spectators);
            }
        }

        void InitLocal() {
            int handle = 1;
            int controllerId = 0;
            ngs.players = new PlayerConnectionInfo[2];
            ngs.players[0] = new PlayerConnectionInfo {
                handle = handle,
                type = GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL,
                connect_progress = 100,
                controllerId = controllerId
            };
            ngs.SetConnectState(handle, PlayerConnectState.Connecting);
            ++handle;
            ++controllerId;
            ngs.players[1] = new PlayerConnectionInfo {
                handle = handle,
                type = GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL,
                connect_progress = 100,
                controllerId = controllerId++
            };
            ngs.SetConnectState(handle, PlayerConnectState.Connecting);
            gs.Init(ngs.players.Length);
        }

        public void DisconnectPlayer(int player) {
            if (Running) {
                if (!localMode) {
                    VectorWar.DisconnectPlayer(player);
                }
            }
        }

        public void Shutdown() {
            if (Running) {
                if (!localMode) {
                    VectorWar.Exit();
                    GGPO.UggSetLogDelegate(null);
                }
                Running = false;
            }
        }

        void OnDestroy() {
            VectorWar.Exit();
        }

        void Update() {
            if (Running) {
                var now = Time.time;
                if (!localMode) {
                    VectorWar.Idle(Mathf.Max(0, (int)((next - now) * 1000f) - 1));
                }

                if (now >= next) {
                    if (localMode) {
                        var inputs = new ulong[ngs.players.Length];
                        for (int i = 0; i < inputs.Length; ++i) {
                            inputs[i] = VectorWar.ReadInputs(ngs.players[i].controllerId);
                        }
                        gs.Update(inputs, 0);
                    }
                    else {
                        VectorWar.RunFrame();
                    }
                    next = now + 1f / 60f;
                }
                UpdateGameView();
            }
        }

        void ResetView() {
            var shipGss = gs._ships;
            shipViews = new ShipView[shipGss.Length];
            bulletLists = new Transform[shipGss.Length][];

            for (int i = 0; i < shipGss.Length; ++i) {
                shipViews[i] = Instantiate(shipPrefab, transform);
                bulletLists[i] = new Transform[shipGss[i].bullets.Length];
                for (int j = 0; j < bulletLists[i].Length; ++j) {
                    bulletLists[i][j] = Instantiate(bulletPrefab, transform);
                }
            }
        }

        void UpdateGameView() {
            OnStatus(ngs.status);
            OnPeriodicChecksum(RenderChecksum(ngs.periodic));
            OnNowChecksum(RenderChecksum(ngs.now));

            var shipsGss = gs._ships;
            if (shipViews.Length != shipsGss.Length) {
                ResetView();
            }
            for (int i = 0; i < shipsGss.Length; ++i) {
                shipViews[i].Populate(shipsGss[i], ngs.players[i]);
                UpdateBullets(shipsGss[i].bullets, bulletLists[i]);
            }
        }

        void UpdateBullets(Bullet[] bullets, Transform[] bulletList) {
            for (int j = 0; j < bulletList.Length; ++j) {
                bulletList[j].position = bullets[j].position;
                bulletList[j].gameObject.SetActive(bullets[j].active);
            }
        }

        string RenderChecksum(NonGameState.ChecksumInfo info) {
            return string.Format("f:{0} c:{1}", info.framenumber, info.checksum); // %04d  %08x
        }
    }
}
