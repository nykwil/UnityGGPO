using System;
using System.IO;
using SharedGame;
using SharedGame.Validation;
using UnityEngine;
using Nykwil.GekkoNet.Session;

namespace VectorWar {
    // Command-line validation of the built example; inert during normal play.
    public sealed class VwPlayerValidation : SessionValidationHarness {
        public VwGameManager manager;
        [Serializable] public sealed class Config {
            public int Players = 2, LocalPlayer, BasePort = 32000, TargetFrame = 600;
            public bool Spectator, Stress;
            public int ProxyPort, LeavePlayer = -1;
            public int RestartCount;
            public string Output;
        }
        [Serializable] public sealed class VwResult : Result {
            public long AllocatedMemory;
            public long[] RestartMemory;
        }
        Config config;
        SessionOptions sessionOptions;
        long[] restartMemory;
        int cycle, restartAt = -1;
        protected override GameManager Manager => manager;
        protected override string CommandLineFlag => "--session-validation";
        protected override string OutputPath => config?.Output;
        protected override SessionOptions Configure(string path) {
            config = JsonUtility.FromJson<Config>(File.ReadAllText(path));
            if (config.TargetFrame < 300 || config.TargetFrame > 3000 || string.IsNullOrEmpty(config.Output))
                throw new ArgumentException("Invalid validation target/output.");
            if (config.RestartCount < 0 || config.RestartCount > 50 || config.RestartCount > 0 && !config.Stress)
                throw new ArgumentException("Resource validation uses 1–50 stress restarts.");
            restartMemory = new long[config.RestartCount + 1];
            return sessionOptions = new SessionOptions {
                Mode = config.Stress ? SessionMode.Stress : config.Spectator ? SessionMode.Spectator : SessionMode.Peer,
                Players = SessionEndpoint.LoopbackRoster(config.Players, config.LocalPlayer, config.BasePort, config.ProxyPort),
                LocalPlayerIndex = Math.Max(0, config.LocalPlayer),
                SpectatorListenPort = config.BasePort + 10,
                Spectators = new[] { new SessionEndpoint { Port = config.BasePort + 10 } }
            };
        }
        public override long ReadInput(int controller, int frame) =>
            frame >= config.TargetFrame - 200 ? 0 : (frame * 17 + config.LocalPlayer * 11) & 63;
        protected override void Update() {
            if (!Done && restartAt >= 0 && Time.frameCount >= restartAt) {
                restartAt = -1;
                GC.Collect();
                restartMemory[cycle++] = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
                ResetDeadline();
                if (cycle == restartMemory.Length) Finish(null);
                else manager.StartSession(sessionOptions);
            }
            base.Update();
        }
        protected override void Observe(IGame game) {
            if (config.RestartCount > 0 && game.FrameNumber >= 120) {
                manager.StopSession(); restartAt = Time.frameCount + 2; return;
            }
            if (config.LeavePlayer == config.LocalPlayer && game.FrameNumber >= 120) {
                manager.DisconnectPlayer(config.LocalPlayer); Finish(null); return;
            }
            RecordChecksum(game, config.TargetFrame, config.TargetFrame + 120);
            // Hosts must outlive a spectator's buffered playback; all peers stopping
            // together at the comparison boundary can strand it one frame short.
            if (game.FrameNumber >= config.TargetFrame + (config.Spectator ? 120 : 240)) Finish(null);
        }
        protected override Result CreateResult() => new VwResult {
            AllocatedMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),
            RestartMemory = restartMemory
        };
    }
}
