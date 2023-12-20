using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityGGPO;

namespace SharedGame {

    public abstract class GameManager : MonoBehaviour {

        public enum UpdateType {
            VectorWar,
            Always,
            Fixed
        }

        public UpdateType updateType = UpdateType.Fixed;

        private static GameManager _instance;

        public static GameManager Instance {
            get {
                if (_instance == null) {
                    _instance = FindObjectOfType<GameManager>();
                }
                return _instance;
            }
        }

        public event Action<StatusInfo> OnStatus;

        public event Action<bool> OnRunningChanged;

        public event Action OnInit;

        public event Action OnStateChanged;

        public Stopwatch updateWatch = new Stopwatch();

        public bool IsRunning { get; private set; }

        public IGameRunner Runner { get; private set; }

        private int next;

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
                    next = Utils.TimeGetTime() + (int)(1000f / 60f);
                }
            }
            if (IsRunning) {
                updateWatch.Start();
                if (updateType == UpdateType.VectorWar) {
                    OriginalUpdate();
                }
                else if (updateType == UpdateType.Always) {
                    AlwaysUpdate();
                }
                else {
                    NewUpdate();
                }

                updateWatch.Stop();

                OnStatus?.Invoke(Runner.GetStatus(updateWatch));
            }
        }

        private void OriginalUpdate() {
            var now = Utils.TimeGetTime();
            var extraMs = Mathf.Max(0, next - now - 1);
            Runner.Idle(extraMs);
            if (now >= next) {
                OnPreRunFrame();
                Runner.RunFrame();
                next = now + (int)(1000f / 60f);
                OnStateChanged?.Invoke();
            }
        }

        private void NewUpdate() {
            var now = Utils.TimeGetTime();
            var extraMs = Mathf.Max(0, next - now - 1);
            Runner.Idle(extraMs);
            if (now >= next) {
                OnPreRunFrame();
                Runner.RunFrame();
                next += (int)(1000f / 60f);
                OnStateChanged?.Invoke();
            }
        }

        private void AlwaysUpdate() {
            var now = Utils.TimeGetTime();
            var extraMs = Mathf.Max(0, next - now - 1);
            Runner.Idle(extraMs);
            if (now >= next) {
                OnPreRunFrame();
                Runner.RunFrame();
                next += (int)(1000f / 60f);
                OnStateChanged?.Invoke();
            }
        }

        public void StartGame(IGameRunner runner) {
            Runner = runner;
        }

        public abstract void StartLocalGame();

        public abstract void StartGGPOGame(IPerfUpdate perfPanel, IList<Connections> connections, int playerIndex);
    }
}