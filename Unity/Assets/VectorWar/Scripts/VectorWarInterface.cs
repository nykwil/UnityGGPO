using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VectorWar {

    public class VectorWarInterface : MonoBehaviour {
        public int maxLogLines = 20;
        public Text txtStatus;
        public Text txtChecksum;
        public Text txtLog;
        public Button btnPlayer1;
        public Button btnPlayer2;
        public Button btnConnect;
        public VectorWarRunner runner;
        readonly List<string> logs = new List<string>();
        public Toggle tglRunnerLog;
        public Toggle tglVectorWarLog;
        public Toggle tglGameStateLog;
        public InputField[] inpIps;
        public Toggle[] tglSpectators;
        public InputField inpPlayerIndex;
        public GameObject pnlConnections;
        public GameObject pnlLog;

        void Awake() {
            VectorWarRunner.OnStatus += OnStatus;
            VectorWarRunner.OnChecksum += OnChecksum;
            VectorWarRunner.OnLog += OnLog;
            VectorWar.OnLog += OnLog;
            GameState.OnLog += OnLog;
            btnConnect.onClick.AddListener(OnConnect);
            btnPlayer1.onClick.AddListener(OnPlayer1);
            btnPlayer2.onClick.AddListener(OnPlayer2);

            tglRunnerLog.isOn = false;
            tglVectorWarLog.isOn = false;
            tglGameStateLog.isOn = false;

            tglRunnerLog.onValueChanged.AddListener(OnToggleRunnerLog);
            tglVectorWarLog.onValueChanged.AddListener(OnVectorWarLog);
            tglGameStateLog.onValueChanged.AddListener(OnGameStateLog);

            for (int i = 0; i < runner.connections.Count; ++i) {
                inpIps[i].text = runner.connections[i].ip + ":" + runner.connections[i].port;
                tglSpectators[i].isOn = runner.connections[i].spectator;
            }

            inpPlayerIndex.text = runner.PlayerIndex.ToString();

            SetConnectText("Startup");
            LogPanelVisibility();
        }

        void OnDestroy() {
            VectorWarRunner.OnStatus -= OnStatus;
            VectorWarRunner.OnChecksum -= OnChecksum;
            VectorWarRunner.OnLog -= OnLog;
            VectorWar.OnLog -= OnLog;
            GameState.OnLog -= OnLog;
            btnConnect.onClick.RemoveListener(OnConnect);
            btnPlayer1.onClick.RemoveListener(OnPlayer1);
            btnPlayer2.onClick.RemoveListener(OnPlayer2);
            tglRunnerLog.onValueChanged.RemoveListener(OnToggleRunnerLog);
            tglVectorWarLog.onValueChanged.RemoveListener(OnVectorWarLog);
            tglGameStateLog.onValueChanged.RemoveListener(OnGameStateLog);
        }

        void OnGameStateLog(bool value) {
            GameState.OnLog -= OnLog;
            if (value) {
                GameState.OnLog += OnLog;
            }
            LogPanelVisibility();
        }

        void OnVectorWarLog(bool value) {
            VectorWar.OnLog -= OnLog;
            if (value) {
                VectorWar.OnLog += OnLog;
            }
            LogPanelVisibility();
        }

        void OnToggleRunnerLog(bool value) {
            VectorWarRunner.OnLog -= OnLog;
            if (value) {
                VectorWarRunner.OnLog += OnLog;
            }
            LogPanelVisibility();
        }

        void LogPanelVisibility() {
            pnlLog.SetActive(tglGameStateLog.isOn || tglRunnerLog.isOn || tglVectorWarLog.isOn);
        }

        void SetConnectText(string text) {
            btnConnect.GetComponentInChildren<Text>().text = text;
        }

        void OnLog(string text) {
            logs.Insert(0, text);
            while (logs.Count > maxLogLines) {
                logs.RemoveAt(logs.Count - 1);
            }
            txtLog.text = string.Join("\n", logs);
        }

        void OnPlayer1() {
            runner.DisconnectPlayer(0);
        }

        void OnPlayer2() {
            runner.DisconnectPlayer(1);
        }

        void OnConnect() {
            if (!runner.Running) {
                pnlConnections.SetActive(false);
                SetConnectText("Shutdown");

                for (int i = 0; i < runner.connections.Count; ++i) {
                    var split = inpIps[i].text.Split(':');
                    runner.connections[i].ip = split[0];
                    runner.connections[i].port = ushort.Parse(split[1]);
                    runner.connections[i].spectator = tglSpectators[i].isOn;
                }

                runner.PlayerIndex = int.Parse(inpPlayerIndex.text);

                runner.Startup();
            }
            else {
                pnlConnections.SetActive(true);
                SetConnectText("Startup");
                runner.Shutdown();
            }
        }

        void OnChecksum(string text) {
            txtChecksum.text = text;
        }

        void OnStatus(string text) {
            txtStatus.text = text;
        }
    }
}
