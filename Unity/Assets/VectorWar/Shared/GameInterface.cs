using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SharedGame {

    public class GameInterface : MonoBehaviour {
        public int maxLogLines = 20;
        public Text txtStatus;
        public Text txtChecksum;
        public Text txtLog;
        public Button btnPlayer1;
        public Button btnPlayer2;
        public Button btnConnect;
        public GameRunner runner;
        private readonly List<string> logs = new List<string>();
        public Toggle tglRunnerLog;
        public Toggle tglGameLog;
        public IConnectionInterface connectionInterface;
        public GameObject pnlLog;

        private void Awake() {
            GameRunner.OnStatus += OnStatus;
            GameRunner.OnChecksum += OnChecksum;
            GameRunner.OnLog += OnLog;
            BaseGGPOGame.OnLog += OnLog;
            btnConnect.onClick.AddListener(OnConnect);
            btnPlayer1.onClick.AddListener(OnPlayer1);
            btnPlayer2.onClick.AddListener(OnPlayer2);

            tglRunnerLog.isOn = false;
            tglGameLog.isOn = false;

            tglRunnerLog.onValueChanged.AddListener(OnToggleRunnerLog);
            tglGameLog.onValueChanged.AddListener(OnToggleGameLog);

            SetConnectText("Startup");
            LogPanelVisibility();
            connectionInterface = Utils.GetInterface<IConnectionInterface>(gameObject);

            Debug.Assert(connectionInterface != null);
        }

        private void OnDestroy() {
            GameRunner.OnStatus -= OnStatus;
            GameRunner.OnChecksum -= OnChecksum;
            GameRunner.OnLog -= OnLog;
            btnConnect.onClick.RemoveListener(OnConnect);
            btnPlayer1.onClick.RemoveListener(OnPlayer1);
            btnPlayer2.onClick.RemoveListener(OnPlayer2);
            tglRunnerLog.onValueChanged.RemoveListener(OnToggleRunnerLog);
            tglGameLog.onValueChanged.RemoveListener(OnToggleGameLog);
        }

        private void OnToggleGameLog(bool value) {
            BaseGGPOGame.OnLog -= OnLog;
            if (value) {
                BaseGGPOGame.OnLog += OnLog;
            }
            LogPanelVisibility();
        }

        private void OnToggleRunnerLog(bool value) {
            GameRunner.OnLog -= OnLog;
            if (value) {
                GameRunner.OnLog += OnLog;
            }
            LogPanelVisibility();
        }

        private void LogPanelVisibility() {
            pnlLog.SetActive(tglGameLog.isOn || tglRunnerLog.isOn);
        }

        private void SetConnectText(string text) {
            btnConnect.GetComponentInChildren<Text>().text = text;
        }

        private void OnLog(string text) {
            logs.Insert(0, text);
            while (logs.Count > maxLogLines) {
                logs.RemoveAt(logs.Count - 1);
            }
            txtLog.text = string.Join("\n", logs);
        }

        private void OnPlayer1() {
            runner.DisconnectPlayer(0);
        }

        private void OnPlayer2() {
            runner.DisconnectPlayer(1);
        }

        private void OnConnect() {
            if (!runner.IsRunning) {
                connectionInterface.SetVisible(false);
                SetConnectText("Shutdown");

                runner.Startup(connectionInterface.CreateGame());
            }
            else {
                connectionInterface.SetVisible(true);
                SetConnectText("Startup");
                runner.Shutdown();
            }
        }

        private void OnChecksum(string text) {
            txtChecksum.text = text;
        }

        private void OnStatus(string text) {
            txtStatus.text = text;
        }
    }
}