using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using UnityEngine;

namespace VectorWar {

    public static class LocalVW {
        public static GameState gs;
        public static NonGameState ngs;

        public static void Init(GameState _gs, NonGameState _ngs) {
            gs = _gs;
            ngs = _ngs;
        }

        public static void Idle(int ms) {
        }

        public static void RunFrame() {
            var inputs = new ulong[ngs.players.Length];
            for (int i = 0; i < inputs.Length; ++i) {
                inputs[i] = VectorWar.ReadInputs(ngs.players[i].controllerId);
            }
            gs.Update(inputs, 0);
        }
    }

    [Serializable]
    public class Connections {
        public ushort port;
        public string ip;
        public bool spectator;
    }

    public class VectorWarRunner : MonoBehaviour {
        public List<Connections> connections;

        public GGPOPerformance perf;

        public ShipView shipPrefab;
        public Transform bulletPrefab;
        public int editorPlayerIndex = 0;
        public int otherPlayerIndex = 1;
        public bool localMode = true;

        ShipView[] shipViews = Array.Empty<ShipView>();
        Transform[][] bulletLists;
        float next;
        NativeArray<byte> buffer;

        public bool Running { get; set; }

        public int PlayerIndex {
            get => Application.isEditor ? editorPlayerIndex : otherPlayerIndex;
            set {
                if (Application.isEditor) {
                    editorPlayerIndex = value;
                }
                else {
                    otherPlayerIndex = value;
                }
            }
        }

        public static event Action<string> OnStatus = (string s) => { };
        public static event Action<string> OnChecksum = (string s) => { };
        public static event Action<string> OnLog = (string s) => { };

        public static Stopwatch updateWatch = new Stopwatch();

        public static void LogCallback(string text) {
            OnLog(text);
        }

        [Button]
        public void TestSave() {
            if (localMode) {
                if (buffer.IsCreated) {
                    buffer.Dispose();
                }
                buffer = GameState.ToBytes(LocalVW.gs);
            }
        }

        [Button]
        public void TestLoad() {
            if (localMode) {
                GameState.FromBytes(LocalVW.gs, buffer);
            }
        }

        public void Startup() {
            if (localMode) {
                InitLocal();
            }
            else {
                InitRemote();
            }

            Running = true;
        }

        void InitRemote() {
            // GGPO.SetLogDelegate(LogCallback);
            VectorWar.Init(new GameState(), new NonGameState(), perf);

            var remote_index = -1;
            var num_spectators = 0;
            var num_players = 0;

            OnLog("Player Index: " + PlayerIndex);
            for (int i = 0; i < connections.Count; ++i) {
                if (i != PlayerIndex && remote_index == -1) {
                    remote_index = i;
                }

                if (connections[i].spectator) {
                    ++num_spectators;
                }
                else {
                    ++num_players;
                }
            }
            if (connections[PlayerIndex].spectator) {
                VectorWar.InitSpectator(connections[PlayerIndex].port, num_players, connections[remote_index].ip, connections[remote_index].port);
            }
            else {
                var players = new List<GGPOPlayer>();
                for (int i = 0; i < connections.Count; ++i) {
                    var player = new GGPOPlayer {
                        player_num = players.Count + 1,
                    };
                    if (PlayerIndex == i) {
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
                VectorWar.Init(connections[PlayerIndex].port, num_players, players, num_spectators);
            }
        }

        void InitLocal() {
            LocalVW.Init(new GameState(), new NonGameState());
            int handle = 1;
            int controllerId = 0;
            LocalVW.ngs.players = new PlayerConnectionInfo[2];
            LocalVW.ngs.players[0] = new PlayerConnectionInfo {
                handle = handle,
                type = GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL,
                connect_progress = 100,
                controllerId = controllerId
            };
            LocalVW.ngs.SetConnectState(handle, PlayerConnectState.Connecting);
            ++handle;
            ++controllerId;
            LocalVW.ngs.players[1] = new PlayerConnectionInfo {
                handle = handle,
                type = GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL,
                connect_progress = 100,
                controllerId = controllerId++
            };
            LocalVW.ngs.SetConnectState(handle, PlayerConnectState.Connecting);
            LocalVW.gs.Init(LocalVW.ngs.players.Length);
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
                    GGPO.SetLogDelegate(null);
                }
                Running = false;
            }
        }

        void OnDestroy() {
            if (!localMode) {
                VectorWar.Exit();
            }
            if (buffer.IsCreated) {
                buffer.Dispose();
            }
        }

        int updateCount;

        void Update() {
            if (Running) {
                ++updateCount;
                updateWatch.Start();
                var now = Time.time;
                var extraMs = Mathf.Max(0, (int)((next - now) * 1000f) - 1);
                if (localMode) {
                    LocalVW.Idle(extraMs);
                }
                else {
                    VectorWar.Idle(extraMs);
                }

                if (now >= next) {
                    if (localMode) {
                        LocalVW.RunFrame();
                    }
                    else {
                        VectorWar.RunFrame();
                    }
                    next = now + 1f / 60f;
                }
                updateWatch.Stop();

                if (localMode) {
                    OnStatus?.Invoke(string.Format("time{0:.00}", (float)updateWatch.ElapsedMilliseconds / updateCount));
                    UpdateGameView(LocalVW.gs, LocalVW.ngs);
                }
                else {
                    var idlePerc = (float)VectorWar.idleWatch.ElapsedMilliseconds / (float)updateWatch.ElapsedMilliseconds;
                    var updatePerc = (float)VectorWar.frameWatch.ElapsedMilliseconds / (float)updateWatch.ElapsedMilliseconds;
                    var otherPerc = 1f - (idlePerc + updatePerc);
                    OnStatus?.Invoke(string.Format("idle:{0:.00} update{1:.00} other{2:.00}", idlePerc, updatePerc, otherPerc));
                    UpdateGameView(VectorWar.gs, VectorWar.ngs);
                }
            }
        }

        void ResetView(GameState gs) {
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

        void UpdateGameView(GameState gs, NonGameState ngs) {
            // OnStatus(ngs.status);
            OnChecksum(RenderChecksum(ngs.periodic) + RenderChecksum(ngs.now));

            var shipsGss = gs._ships;
            if (shipViews.Length != shipsGss.Length) {
                ResetView(gs);
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
