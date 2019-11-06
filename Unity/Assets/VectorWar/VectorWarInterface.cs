using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VectorWar {

    public class VectorWarInterface : MonoBehaviour {
        public int maxLogLines = 20;
        public Text txtStatus;
        public Text txtPeriodicChecksum;
        public Text txtNowChecksum;
        public Text txtLog;
        public Button btnPlayer1;
        public Button btnPlayer2;
        public Button btnConnect;
        public VectorWarRunner runner;
        readonly List<string> logs = new List<string>();

        void Awake() {
            VectorWarRunner.OnStatus += OnStatus;
            VectorWarRunner.OnPeriodicChecksum += OnPeriodicChecksum;
            VectorWarRunner.OnNowChecksum += OnNowChecksum;
            VectorWarRunner.OnLog += OnLog;
            VectorWar.OnLog += OnLog;
            btnConnect.onClick.AddListener(OnConnect);
            btnPlayer1.onClick.AddListener(OnPlayer1);
            btnPlayer2.onClick.AddListener(OnPlayer2);
            SetConnectText("Startup");
        }

        void OnDestroy() {
            VectorWarRunner.OnStatus -= OnStatus;
            VectorWarRunner.OnPeriodicChecksum -= OnPeriodicChecksum;
            VectorWarRunner.OnNowChecksum -= OnNowChecksum;
            VectorWarRunner.OnLog -= OnLog;
            VectorWar.OnLog -= OnLog;
            btnConnect.onClick.RemoveListener(OnConnect);
            btnPlayer1.onClick.RemoveListener(OnPlayer1);
            btnPlayer2.onClick.RemoveListener(OnPlayer2);
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
                SetConnectText("Shutdown");
                runner.Startup();
            }
            else {
                SetConnectText("Startup");
                runner.Shutdown();
            }
        }

        void OnNowChecksum(string text) {
            txtNowChecksum.text = text;
        }

        void OnPeriodicChecksum(string text) {
            txtPeriodicChecksum.text = text;
        }

        void OnStatus(string text) {
            txtStatus.text = text;
        }
    }
}
