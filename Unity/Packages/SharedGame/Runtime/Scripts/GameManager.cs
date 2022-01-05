using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using UnityGGPO;

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

        public event Action OnInit;

        public event Action OnStateChanged;

        public Stopwatch updateWatch = new Stopwatch();

        public bool IsRunning { get; private set; }

        public IGameRunner Runner { get; private set; }

        public void DisconnectPlayer(int player) {
            if (Runner != null) {
                Runner.DisconnectPlayer(player);
            }
        }

        public void Shutdown() {
            if (Runner != null) {
                Runner.Shutdown();
                Runner = null;
            }
        }

        private void OnDestroy() {
            Shutdown();
            _instance = null;
        }

        protected virtual void OnPreRunFrame() {
        }

        private void Update() {
            if (IsRunning != (Runner != null)) {
                IsRunning = Runner != null;
                OnRunningChanged?.Invoke(IsRunning);
                if (IsRunning) {
                    OnInit?.Invoke();
                }
            }
            if (Runner != null) {
                updateWatch.Start();
                var now = Time.time;
                var extraMs = Mathf.Max(0, (int)((next - now) * 1000f) - 1);
                Runner.Idle(extraMs);
                Thread.Sleep(extraMs);

                if (now >= next) {
                    OnPreRunFrame();
                    Runner.RunFrame();
                    next = now + 1f / 60f;
                    OnStateChanged?.Invoke();
                }
                updateWatch.Stop();

                string status = Runner.GetStatus(updateWatch);
                OnStatus?.Invoke(status);
                OnChecksum?.Invoke(RenderChecksum(Runner.GameInfo.periodic) + RenderChecksum(Runner.GameInfo.now));
            }
        }

        private string RenderChecksum(GameInfo.ChecksumInfo info) {
            return string.Format("f:{0} c:{1}", info.framenumber, info.checksum); // %04d  %08x
        }

        public void StartGame(IGameRunner runner) {
            Runner = runner;
        }

        public abstract void StartLocalGame();

        public abstract void StartGGPOGame(IPerfUpdate perfPanel, IList<Connections> connections, int playerIndex);
    }
}