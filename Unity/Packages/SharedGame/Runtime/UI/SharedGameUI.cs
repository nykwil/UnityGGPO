using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Nykwil.GekkoNet.Session;
namespace SharedGame.UI {
    [RequireComponent(typeof(PanelRenderer))]
    public sealed class SharedGameUI : MonoBehaviour {
        public GameManager manager;
        public PanelRenderer document;
        public event Action<bool> DiagnosticsVisibilityChanged;
        public bool DiagnosticsVisible { get; private set; }
#if SHAREDGAME_INPUT_SYSTEM
        [Tooltip("Key that shows or hides the menu. None disables the shortcut.")]
        public UnityEngine.InputSystem.Key menuKey = UnityEngine.InputSystem.Key.F1;
#elif ENABLE_LEGACY_INPUT_MANAGER
        [Tooltip("Key that shows or hides the menu. None disables the shortcut.")]
        public KeyCode menuKey = KeyCode.F1;
#endif
        [SerializeField, Tooltip("Whether the menu starts visible.")] bool menuVisible = true;
        /// <summary>Raised when the menu is shown or hidden.</summary>
        public event Action<bool> MenuVisibilityChanged;
        /// <summary>Whether the session menu card is shown. The toggle button stays on screen either way.</summary>
        public bool MenuVisible => menuVisible;
        VisualElement connection, session;
        EnumField mode;
        IntegerField playerCount, localPlayer, controller, host, listenPort;
        readonly TextField[] addresses = new TextField[4];
        readonly IntegerField[] ports = new IntegerField[4];
        readonly Label[] statuses = new Label[4];
        readonly ProgressBar[] progress = new ProgressBar[4];
        readonly Button[] disconnect = new Button[4];
        readonly Action[] disconnectActions = new Action[4];
        Button startButton, stopButton, restartButton, diagnosticsButton, menuButton;
        TextField spectators;
        Label message;
        ScrollView card;
        double nextRefresh;
        VisualElement root;
        int uiVersion = -1;
        bool bound;
        void OnEnable() {
            manager.OnRunningChanged += RunningChanged;
            manager.OnError += ShowError;
            document.RegisterUIReloadCallback(OnUIReload);
        }
        void OnUIReload(PanelRenderer renderer, VisualElement element, int version) {
            if (bound && root == element && uiVersion == version) return;
            Unbind(); root = element; uiVersion = version;
            card = root.Q<ScrollView>();
            connection = root.Q("connection"); session = root.Q("session");
            message = root.Q<Label>("message");
            mode = root.Q<EnumField>("mode"); if (mode.value == null) mode.Init(SessionMode.Local);
            playerCount = root.Q<IntegerField>("playerCount");
            localPlayer = root.Q<IntegerField>("localPlayer");
            controller = root.Q<IntegerField>("controller");
            host = root.Q<IntegerField>("host"); listenPort = root.Q<IntegerField>("listenPort");
            spectators = root.Q<TextField>("spectators");
            for (int i = 0; i < 4; i++) {
                addresses[i] = root.Q<TextField>("address" + i);
                ports[i] = root.Q<IntegerField>("port" + i);
                statuses[i] = root.Q<Label>("status" + i);
                progress[i] = root.Q<ProgressBar>("progress" + i);
                disconnect[i] = root.Q<Button>("disconnect" + i);
                statuses[i].parent.Add(progress[i]);
                int player = i;
                disconnectActions[i] = () => Attempt(() => manager.DisconnectPlayer(player));
                disconnect[i].clicked += disconnectActions[i];
            }
            startButton = root.Q<Button>("start"); stopButton = root.Q<Button>("stop");
            restartButton = root.Q<Button>("restart"); diagnosticsButton = root.Q<Button>("diagnosticsToggle");
            startButton.clicked += StartFromForm; stopButton.clicked += manager.StopSession;
            restartButton.clicked += Restart; diagnosticsButton.clicked += ToggleDiagnostics;
            menuButton = root.Q<Button>("menuToggle");
            if (menuButton != null) {
                // Pointer-only: never takes focus, so game keys (arrows, Enter) cannot navigate to or press it.
                menuButton.focusable = false;
                menuButton.clicked += ToggleMenu;
            }
            mode.RegisterValueChangedCallback(ModeChanged);
            playerCount.RegisterValueChangedCallback(CountChanged);
            bound = true;
            RefreshForm();
            RunningChanged(manager.IsRunning);
            ApplyMenuVisibility();
            if (manager.LastError != null) ShowError(manager.LastError);
        }
        /// <summary>Shows or hides the session menu.</summary>
        public void SetMenuVisible(bool visible) {
            if (menuVisible == visible) return;
            menuVisible = visible;
            ApplyMenuVisibility();
            MenuVisibilityChanged?.Invoke(visible);
        }
        public void ToggleMenu() => SetMenuVisible(!menuVisible);
        void ApplyMenuVisibility() {
            if (!bound) return;
            card.EnableInClassList("hidden", !menuVisible);
            // A hidden control must not keep focus, or Enter would still press it.
            if (!menuVisible && root.panel?.focusController?.focusedElement is VisualElement focused && card.Contains(focused)) focused.Blur();
            if (menuButton != null) menuButton.text = menuVisible ? "Hide menu" : "Menu";
        }
        bool MenuKeyPressed() {
#if SHAREDGAME_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            return menuKey != UnityEngine.InputSystem.Key.None && keyboard != null && keyboard[menuKey].wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return menuKey != KeyCode.None && Input.GetKeyDown(menuKey);
#else
            return false;
#endif
        }
        void OnDisable() {
            manager.OnRunningChanged -= RunningChanged; manager.OnError -= ShowError;
            document.UnregisterUIReloadCallback(OnUIReload);
            Unbind(); root = null;
        }
        void Unbind() {
            if (!bound) return;
            bound = false;
            mode.UnregisterValueChangedCallback(ModeChanged);
            playerCount.UnregisterValueChangedCallback(CountChanged);
            for (int i = 0; i < disconnect.Length; i++) if (disconnect[i] != null) disconnect[i].clicked -= disconnectActions[i];
            if (startButton != null) startButton.clicked -= StartFromForm;
            if (stopButton != null) stopButton.clicked -= manager.StopSession;
            if (restartButton != null) restartButton.clicked -= Restart;
            if (diagnosticsButton != null) diagnosticsButton.clicked -= ToggleDiagnostics;
            if (menuButton != null) menuButton.clicked -= ToggleMenu;
        }
        void Restart() => Attempt(manager.RestartSession);
        void ModeChanged(ChangeEvent<Enum> evt) => RefreshForm();
        void CountChanged(ChangeEvent<int> evt) => RefreshForm();
        void RefreshForm() {
            var selected = (SessionMode)mode.value;
            bool peer = selected == SessionMode.Peer;
            bool spectator = selected == SessionMode.Spectator;
            bool network = peer || spectator;
            int count = Math.Max(2, Math.Min(4, playerCount.value));
            for (int i = 0; i < 4; i++) addresses[i].parent.EnableInClassList("hidden", !network || i >= count);
            localPlayer.EnableInClassList("hidden", !peer);
            controller.EnableInClassList("hidden", !peer);
            host.EnableInClassList("hidden", !network);
            listenPort.EnableInClassList("hidden", !spectator);
            spectators.EnableInClassList("hidden", !peer);
            root.Q("rosterHelp").EnableInClassList("hidden", !network);
            root.Q("networkHelp").EnableInClassList("hidden", !network);
        }
        void ShowError(string error) {
            if (!bound) return;
            message.text = error; message.AddToClassList("error");
            SetMenuVisible(true);
            card.ScrollTo(message);
        }
        void Attempt(Action action) {
            try { action(); }
            catch (Exception error) { ShowError(error.Message); }
        }
        void StartFromForm() => Attempt(() => {
            int count = playerCount.value;
            if (count < 2 || count > 4) throw new ArgumentException("Choose 2–4 players.");
            var roster = new SessionEndpoint[count];
            for (int i = 0; i < count; i++) roster[i] = new SessionEndpoint { Address = addresses[i].value, Port = ports[i].value };
            var viewers = new List<SessionEndpoint>();
            foreach (var line in spectators.value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) {
                int colon = line.LastIndexOf(':');
                if (colon < 1 || !int.TryParse(line.Substring(colon + 1).Trim(), out int port))
                    throw new ArgumentException("Spectator endpoints use IPv4:port, one per line.");
                viewers.Add(new SessionEndpoint { Address = line.Substring(0, colon).Trim(), Port = port });
            }
            manager.StartSession(new SessionOptions {
                Mode = (SessionMode)mode.value, Players = roster, LocalPlayerIndex = localPlayer.value,
                LocalControllerId = controller.value, SpectatorHostIndex = host.value,
                SpectatorListenPort = listenPort.value, Spectators = viewers.ToArray()
            });
            message.RemoveFromClassList("error"); message.text = "Session started";
        });
        void ToggleDiagnostics() {
            DiagnosticsVisible = !DiagnosticsVisible;
            DiagnosticsVisibilityChanged?.Invoke(DiagnosticsVisible);
        }
        void RunningChanged(bool running) {
            if (!bound) return;
            root.Q(className: "card").EnableInClassList("session-card", running);
            connection.EnableInClassList("hidden", running);
            session.EnableInClassList("hidden", !running);
            if (!running) { message.text = "Session stopped. Choose a mode to start."; SetMenuVisible(true); }
        }
        void Update() {
            if (bound && MenuKeyPressed()) ToggleMenu();
            if (!bound || manager.Runner == null || Time.unscaledTimeAsDouble < nextRefresh) return;
            nextRefresh = Time.unscaledTimeAsDouble + .1;
            diagnosticsButton.SetEnabled(DiagnosticsVisibilityChanged != null);
            var info = manager.Runner.GameInfo; message.text = info.Status;
            for (int i = 0; i < 4; i++) {
                bool exists = i < info.Players.Length;
                statuses[i].parent.EnableInClassList("hidden", !exists);
                if (!exists) continue;
                var player = info.Players[i];
                statuses[i].text = $"Player {i}: {player.State}";
                progress[i].value = player.Progress;
                progress[i].EnableInClassList("hidden", player.State != PlayerConnectState.Connecting && player.State != PlayerConnectState.Synchronizing);
                disconnect[i].SetEnabled(player.CanDisconnect);
            }
        }
    }
}
