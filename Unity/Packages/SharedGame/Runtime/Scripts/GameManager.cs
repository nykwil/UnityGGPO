using System;
using UnityEngine;
using Nykwil.GekkoNet.Session;
namespace SharedGame {
    /// <summary>
    /// Owns one session at a time and drives it from Unity's Update at a fixed 60 Hz.
    /// Subclass it to supply your game and input source, then call <see cref="StartSession"/>.
    /// </summary>
    public abstract class GameManager : MonoBehaviour {
        /// <summary>Raised when a new game has been created for a session.</summary>
        public event Action OnInit;
        /// <summary>Raised after an update that simulated at least one frame, including rollback correction. Read <see cref="Runner"/>.Game to present.</summary>
        public event Action OnStateChanged;
        /// <summary>Raised with true when a session starts and false when it stops. Clear presentation on false.</summary>
        public event Action<bool> OnRunningChanged;
        /// <summary>Raised every 100 ms after a diagnostics sample is recorded.</summary>
        public event Action<SessionDiagnostics> OnDiagnostics;
        /// <summary>Raised with the message when a session fails to start or fails while running; the session is stopped first.</summary>
        public event Action<string> OnError;
        /// <summary>The active session runner, or null when stopped.</summary>
        public IGameRunner Runner { get; private set; }
        public bool IsRunning => Runner != null;
        /// <summary>Mode of the active session, or null when stopped.</summary>
        public SessionMode? Mode => IsRunning ? lastOptions?.Mode : null;
        /// <summary>Message of the last start or runtime failure; cleared when a session starts.</summary>
        public string LastError { get; private set; }
        SessionOptions lastOptions;
        double nextSample;
        readonly SessionScheduler scheduler = new SessionScheduler();
        /// <summary>Creates a fresh game (frame 0) for <paramref name="playerCount"/> players.</summary>
        protected abstract IGame CreateGame(int playerCount);
        /// <summary>Creates the local input source for a new session.</summary>
        protected abstract ILocalInputSource CreateInputSource();
        /// <summary>Custom network transport for peer and spectator sessions; null uses the built-in UDP adapter.</summary>
        protected virtual IGekkoNetworkTransport CreateTransport(SessionOptions options) => null;
        /// <summary>Chooses the runner: <see cref="LocalRunner"/> for local play, otherwise <see cref="GekkoRunner"/> with <see cref="CreateTransport"/>.</summary>
        protected virtual IGameRunner CreateRunner(IGame game, ILocalInputSource source, SessionOptions options) =>
            options.Mode == SessionMode.Local ? (IGameRunner)new LocalRunner(game, source) : new GekkoRunner(game, source, options, CreateTransport);
        /// <summary>Replaces CreateInputSource for subsequent sessions, e.g. a scripted validation driver.</summary>
        public ILocalInputSource InputOverride { private get; set; }
        /// <summary>
        /// Validates <paramref name="options"/>, stops any running session and starts a new one with a fresh game.
        /// Throws (and raises <see cref="OnError"/>) on invalid options or startup failure; a running session is
        /// left untouched when validation fails.
        /// </summary>
        public void StartSession(SessionOptions options) {
            if (options == null) throw new ArgumentNullException(nameof(options));
            // Validate before stopping a healthy session or opening a socket.
            options.Validate(1);
            var game = CreateGame(options.Players.Length);
            options.ValidateFor(game);
            var source = InputOverride ?? CreateInputSource();
            StopSession();
            try {
                Runner = CreateRunner(game, source, options);
                lastOptions = options.Copy();
                LastError = null;
                nextSample = Time.realtimeSinceStartupAsDouble;
                scheduler.Reset(nextSample);
                OnInit?.Invoke();
                OnRunningChanged?.Invoke(true);
                OnStateChanged?.Invoke();
            } catch (Exception error) {
                LastError = error.Message;
                StopSession();
                OnError?.Invoke(LastError);
                throw;
            }
        }
        /// <summary>Starts a fresh session with a copy of the last options.</summary>
        public void RestartSession() {
            if (lastOptions == null) throw new InvalidOperationException("No previous session.");
            StartSession(lastOptions);
        }
        /// <summary>Disposes the active session, if any. Safe to call repeatedly.</summary>
        public void StopSession() {
            var previous = Runner;
            Runner = null;
            try { previous?.Dispose(); }
            finally { if (previous != null) OnRunningChanged?.Invoke(false); }
        }
        /// <summary>Disconnects player <paramref name="index"/>; stops the session when that ends it locally (e.g. the local peer leaves).
        /// Does nothing when no session is running or the player cannot currently be disconnected.</summary>
        public void DisconnectPlayer(int index) {
            if (Runner == null) return;
            if ((uint)index >= Runner.GameInfo.Players.Length) throw new ArgumentOutOfRangeException(nameof(index));
            if (!Runner.GameInfo.Players[index].CanDisconnect) return;
            if (Runner.DisconnectPlayer(index)) StopSession();
        }
        void Update() {
            if (Runner == null) return;
            try {
                Runner.Poll();
                double now = Time.realtimeSinceStartupAsDouble;
                int ticks = scheduler.TicksDue(now, Runner.FramesAhead);
                for (int i = 0; i < ticks; i++) Runner.Tick();
                Runner.EndFrame();
                if (now >= nextSample) {
                    Runner.SampleDiagnostics();
                    nextSample = now + .1;
                    OnDiagnostics?.Invoke(Runner.Diagnostics);
                }
                if (ticks > 0) OnStateChanged?.Invoke();
            } catch (Exception error) {
                LastError = error.Message;
                if (Debug.isDebugBuild) Debug.LogException(error, this);
                try { StopSession(); }
                finally { OnError?.Invoke(LastError); }
            }
        }
        protected virtual void OnDisable() => StopSession();
        protected virtual void OnDestroy() => StopSession();
        void OnApplicationQuit() => StopSession();
    }
}
