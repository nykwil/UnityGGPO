using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Logging;
using UnityEngine;
using UnityGGPO;

namespace SharedGame {

    public abstract class GameManager : MonoBehaviour {

        public enum UpdateType {
            VectorWar,
            Always,
            FixedSkip,
            FixedFastForward,
            Smoothed
        }

        public UpdateType updateType = UpdateType.FixedSkip;

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

        private double start;
        private double next;
        private int currentFrame;

        private double MsToFrame(double time) {
            return time / 1000.0 * 60.0;
        }

        private double FrameToMs(double ms) {
            return ms * 1000.0 / 60.0;
        }

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
                    InitGame();
                }
            }
            if (IsRunning) {
                updateWatch.Start();

                if (updateType == UpdateType.VectorWar) {
                    UpdateVectorwar();
                }
                else if (updateType == UpdateType.Always) {
                    UpdateAlways();
                }
                else if (updateType == UpdateType.FixedSkip) {
                    UpdateFixedSkip();
                }
                else if (updateType == UpdateType.FixedFastForward) {
                    UpdateFixedFastForward();
                }
                else if (updateType == UpdateType.Smoothed) {
                    UpdateSmoothed();
                }

                updateWatch.Stop();

                var statusInfo = Runner.GetStatus(updateWatch);

                OnStatus?.Invoke(statusInfo);
            }
        }

        private void InitGame() {
            OnInit?.Invoke();
            start = (double)Utils.TimeGetTime();
            next = start;
            currentFrame = 0;
        }

        private void Tick() {
            OnPreRunFrame();
            Runner.RunFrame();
            currentFrame++;
            OnStateChanged?.Invoke();
        }

        private void UpdateVectorwar() {
            var now = (double)Utils.TimeGetTime();
            if (Runner.FramesAhead > 0) {
                Utils.Sleep((int)FrameToMs(Runner.FramesAhead));
                Runner.FramesAhead = 0;
            }
            var extraMs = Mathf.Max(0, (int)(next - now - 1));
            Runner.Idle(extraMs);
            if (now >= next) {
                Tick();
                next = now + FrameToMs(1);
            }
        }

        private void UpdateFixedSkip() {
            var now = Utils.TimeGetTime();
            if (Runner.FramesAhead > 0) {
                next += FrameToMs(Runner.FramesAhead);
                Runner.FramesAhead = 0;
            }
            var extraMs = Mathf.Max(0, (int)(next - now - 1));
            Runner.Idle(extraMs);
            while (now >= next) {
                Tick();
                next += FrameToMs(1);
            }
        }

        private void UpdateSmoothed() {
            var now = (double)Utils.TimeGetTime();
            var extraMs = Mathf.Max(0, (int)(next - now - 1));
            Runner.Idle(extraMs);
            if (now >= next) {
                if (Runner.FramesAhead > 0) {
                    start += FrameToMs(Runner.FramesAhead - 1);
                    Runner.FramesAhead = 0;
                }
                var targetFrame = MsToFrame(now - start);
                int nearestTarget = Mathf.RoundToInt((float)targetFrame);
                double d = 1.0;
                if (currentFrame != nearestTarget) {
                    d = 1 - ((targetFrame - currentFrame) / 50f);
                    Log.Verbose("Smooth Step adjusted: s:{0} t:{1} c:{2}", d, targetFrame, currentFrame);
                }

                next += FrameToMs(d);

                Tick();
            }
        }

        private void UpdateFixedFastForward() {
            var now = Utils.TimeGetTime();
            var extraMs = Mathf.Max(0, (int)(next - now - 1));
            Runner.Idle(extraMs);
            if (now >= next) {
                Tick();
                next += FrameToMs(1);
                if (Runner.FramesAhead > 0) {
                    next += FrameToMs(1);
                    --Runner.FramesAhead;
                }
            }
        }

        private void UpdateAlways() {
            if (Runner.FramesAhead > 0) {
                Utils.Sleep((int)FrameToMs(Runner.FramesAhead));
                Runner.FramesAhead = 0;
            }
            var now = Utils.TimeGetTime();
            var extraMs = Mathf.Max(0, (int)(next - now - 1));
            Runner.Idle(extraMs);
            Tick();
            next += FrameToMs(1);
        }

        public void StartGame(IGameRunner runner) {
            Runner = runner;
        }

        public abstract void StartLocalGame();

        public abstract void StartGGPOGame(IPerfUpdate perfPanel, IList<Connections> connections, int playerIndex);

        public void ResetTimers() {
            updateWatch.Reset();
            Runner.ResetTimers();
        }
    }
}