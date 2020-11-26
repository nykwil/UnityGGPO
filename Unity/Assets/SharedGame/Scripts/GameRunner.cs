using System;
using System.Diagnostics;
using UnityEngine;

namespace SharedGame {

    // @TODO this shouldn't be a Component
    public class GameRunner : MonoBehaviour {
        private float next;

        public event Action<string> OnStatus;

        public event Action<string> OnChecksum;

        public event Action<string> OnLog;

        public event Action<bool> OnRunningChanged;

        public Stopwatch updateWatch = new Stopwatch();

        public bool IsRunning { get; private set; }

        public IGame Game { get; private set; }

        public void LogCallback(string text) {
            OnLog?.Invoke(text);
        }

        public void Startup(IGame game) {
            Game = game;
        }

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
                OnChecksum?.Invoke(RenderChecksum(Game.ngs.periodic) + RenderChecksum(Game.ngs.now));
            }
        }

        private string RenderChecksum(GameInfo.ChecksumInfo info) {
            return string.Format("f:{0} c:{1}", info.framenumber, info.checksum); // %04d  %08x
        }
    }
}