using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SharedGame {

    public class GameInterface : MonoBehaviour {
        public int maxLogLines = 20;
        public Text txtGameLog;
        public Text txtPluginLog;
        public Text txtIdlePerc;
        public Text txtUpdatePerc;
        public Text txtNowChecksum;
        public Text txtPeriodicChecksum;
        public Button btnPlayer1;
        public Button btnPlayer2;
        public Button btnConnect;

        private GameManager gameManager => GameManager.Instance;

        private void Awake() {
            gameManager.OnStatus += OnStatus;
            gameManager.OnRunningChanged += OnRunningChanged;

            btnConnect.onClick.AddListener(OnConnect);
            btnPlayer1.onClick.AddListener(OnPlayer1);
            btnPlayer2.onClick.AddListener(OnPlayer2);

            SetConnectText("");
        }

        private void OnDestroy() {
            gameManager.OnStatus -= OnStatus;
            gameManager.OnRunningChanged -= OnRunningChanged;

            btnConnect.onClick.RemoveListener(OnConnect);
            btnPlayer1.onClick.RemoveListener(OnPlayer1);
            btnPlayer2.onClick.RemoveListener(OnPlayer2);
        }

        private void OnRunningChanged(bool obj) {
            SetConnectText(obj ? "Shutdown" : "--");
        }

        private void SetConnectText(string text) {
            btnConnect.GetComponentInChildren<Text>().text = text;
        }

        private void OnPlayer1() {
            gameManager.DisconnectPlayer(0);
        }

        private void OnPlayer2() {
            gameManager.DisconnectPlayer(1);
        }

        private void OnConnect() {
            if (gameManager.IsRunning) {
                gameManager.Shutdown();
            }
        }

        private void OnStatus(StatusInfo status) {
            txtIdlePerc.text = "IP:" + status.idlePerc.ToString();
            txtUpdatePerc.text = "UP: " + status.updatePerc.ToString();
            txtNowChecksum.text = "NC: " + status.now.ToString();
            txtPeriodicChecksum.text = "PC: " + status.periodic.ToString();
        }
    }
}