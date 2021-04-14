using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace SharedGame {

    public abstract class GameManager : MonoBehaviour {
        private static GameManager _instance;

        public static GameManager Instance {
            get {
                if (_instance == null) {
                    _instance = FindObjectOfType<GameManager>();
                }
                return _instance;
            }
        }

        private float next;

        public event Action<string> OnStatus;

        public event Action<string> OnChecksum;

        public event Action<bool> OnRunningChanged;

        public Stopwatch updateWatch = new Stopwatch();

        public bool IsRunning { get; private set; }

        public IGame Game { get; private set; }

        public void DisconnectPlayer(int player) {
            if (Game != null) {
                Game.DisconnectPlayer(player);
            }
        }

        public void Shutdown() {
            if (Game != null) {
                Game.Shutdown();
                Game = null;
            }
        }

        private void OnDestroy() {
            Shutdown();
            _instance = null;
        }

        private void Update() {
            if (IsRunning != (Game != null)) {
                IsRunning = Game != null;
                OnRunningChanged?.Invoke(IsRunning);
            }
            if (Game != null) {
                updateWatch.Start();
                var now = Time.time;
                var extraMs = Mathf.Max(0, (int)((next - now) * 1000f) - 1);
                Game.Idle(extraMs);

                if (now >= next) {
                    Game.RunFrame();
                    next = now + 1f / 60f;
                }
                updateWatch.Stop();

                string status = Game.GetStatus(updateWatch);
                OnStatus?.Invoke(status);
                OnChecksum?.Invoke(RenderChecksum(Game.GameInfo.periodic) + RenderChecksum(Game.GameInfo.now));
            }
        }

        private string RenderChecksum(GameInfo.ChecksumInfo info) {
            return string.Format("f:{0} c:{1}", info.framenumber, info.checksum); // %04d  %08x
        }

        public void StartLocalGame() {
            Game = CreateLocalGame();
        }

        public void StartGGPOGame(IPerfUpdate perfPanel, IList<Connections> connections, int playerIndex) {
            Game = CreateGGPOGame(perfPanel, connections, playerIndex);
        }

        protected abstract IGame CreateLocalGame();

        protected abstract IGame CreateGGPOGame(IPerfUpdate perfPanel, IList<Connections> connections, int playerIndex);
    }
}