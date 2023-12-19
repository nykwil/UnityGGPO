using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public event Action<StatusInfo> OnStatus;

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

        private void Start() {
            var t = Time.realtimeSinceStartup;
            var t2 = t + 1f / 60f;
            System.Threading.Thread.Sleep(SToMs(t2 - t));
            UnityEngine.Debug.Log($"{t} {t2} {Time.realtimeSinceStartup}");
        }

        private int SToMs(float time) {
            return (int)(time * 1000f);
        }

        // Inputs need to be polled in Update (or you can use something like Rewired's FixedUpdate setting) so we don't miss inputs
        private void FixedUpdate() {
            if (IsRunning != (Runner != null)) {
                IsRunning = Runner != null;
                OnRunningChanged?.Invoke(IsRunning);
                if (IsRunning) {
                    OnInit?.Invoke();
                }
            }
            if (IsRunning) {
                updateWatch.Start();
                
                Runner.Idle(0);
                OnPreRunFrame();
                Runner.RunFrame();
                OnStateChanged?.Invoke();

                updateWatch.Stop();
                OnStatus?.Invoke(Runner.GetStatus(updateWatch));
            }
        }

        public void StartGame(IGameRunner runner) {
            Runner = runner;
        }

        public abstract void StartLocalGame();

        public abstract void StartGGPOGame(IPerfUpdate perfPanel, IList<Connections> connections, int playerIndex);
    }
}