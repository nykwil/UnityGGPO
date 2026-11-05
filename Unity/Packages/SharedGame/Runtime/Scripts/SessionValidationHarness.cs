using System;
using System.IO;
using UnityEngine;
using Nykwil.GekkoNet.Session;
namespace SharedGame.Validation {
    /// <summary>
    /// Command-line validation driver for built players. Inert unless <see cref="CommandLineFlag"/>
    /// is passed with a JSON config path; then it starts a session with scripted input, records
    /// checksums over a frame window and writes a JSON result before quitting.
    /// </summary>
    public abstract class SessionValidationHarness : MonoBehaviour, ILocalInputSource {
        [Serializable] public struct FrameChecksum { public int Frame; public uint Checksum; }
        [Serializable] public class Result {
            public string UnityVersion, Backend, Error;
            public int FinalFrame, Desyncs;
            public uint Rollbacks, MaxDepth, Stalls;
            public FrameChecksum[] Checksums;
        }
        public static string Backend =>
#if ENABLE_IL2CPP
            "IL2CPP";
#elif ENABLE_CORECLR
            "CoreCLR";
#else
            "Mono";
#endif
        readonly FrameChecksum[] history = new FrameChecksum[128];
        int count;
        byte[] snapshot;
        double deadline = double.PositiveInfinity;
        protected bool Done { get; private set; }
        protected abstract GameManager Manager { get; }
        protected abstract string CommandLineFlag { get; }
        protected virtual double TimeoutSeconds => 90;
        /// <summary>Path the result is written to; null skips writing.</summary>
        protected abstract string OutputPath { get; }
        /// <summary>Reads the config at <paramref name="path"/> and returns the session to start.</summary>
        protected abstract SessionOptions Configure(string path);
        public abstract long ReadInput(int controllerId, int frame);
        /// <summary>Called after each simulated update while validation runs.</summary>
        protected abstract void Observe(IGame game);
        protected virtual Result CreateResult() => new Result();

        protected virtual void Start() {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, CommandLineFlag);
            if (index < 0 || index + 1 >= args.Length) { enabled = false; return; }
            try {
                var options = Configure(args[index + 1]);
                Manager.InputOverride = this;
                Manager.OnStateChanged += OnStateChanged;
                Manager.OnError += Fail;
                Application.runInBackground = true;
                Application.targetFrameRate = 120;
                QualitySettings.vSyncCount = 0;
                ResetDeadline();
                Manager.StartSession(options);
            } catch (Exception error) { Fail(error.ToString()); }
        }
        protected void ResetDeadline() => deadline = Time.realtimeSinceStartupAsDouble + TimeoutSeconds;
        protected virtual void Update() {
            if (!Done && Time.realtimeSinceStartupAsDouble > deadline) Fail("Validation timed out.");
        }
        void OnStateChanged() {
            if (!Done && Manager.Runner != null) Observe(Manager.Runner.Game);
        }
        /// <summary>Records one checksum per frame for frames in [from, to).</summary>
        protected void RecordChecksum(IGame game, int from, int to) {
            if (game.FrameNumber < from || game.FrameNumber >= to || count == history.Length) return;
            if (count > 0 && history[count - 1].Frame == game.FrameNumber) return;
            if (snapshot == null || snapshot.Length != game.SnapshotSize) snapshot = new byte[game.SnapshotSize];
            history[count++] = new FrameChecksum { Frame = game.FrameNumber, Checksum = SnapshotChecksum.Compute(game, snapshot) };
        }
        protected void Fail(string error) { if (!Done) Finish(error); }
        protected void Finish(string error) {
            Done = true;
            var result = CreateResult();
            result.UnityVersion = Application.unityVersion; result.Backend = Backend; result.Error = error;
            result.Checksums = new FrameChecksum[count];
            Array.Copy(history, result.Checksums, count);
            var runner = Manager.Runner;
            if (runner != null) {
                runner.SampleDiagnostics();
                var diagnostics = runner.Diagnostics;
                result.FinalFrame = runner.Game.FrameNumber; result.Desyncs = diagnostics.DesyncCount;
                result.Rollbacks = diagnostics.Current.Rollback.TotalCount;
                result.MaxDepth = diagnostics.Current.Rollback.MaxFrames;
                result.Stalls = diagnostics.Current.Rollback.TotalStalls;
            }
            string output = OutputPath;
            if (!string.IsNullOrEmpty(output)) {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
                File.WriteAllText(output, JsonUtility.ToJson(result, true));
            }
            Manager.StopSession();
            Application.Quit(error == null ? 0 : 1);
        }
        protected virtual void OnDestroy() {
            if (Manager != null) { Manager.OnStateChanged -= OnStateChanged; Manager.OnError -= Fail; }
        }
    }
}
