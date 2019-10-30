using UnityEngine;
using UnityEngine.UI;

namespace VectorWar {

    public class VectorWarInterface : MonoBehaviour {
        public Text txtStatus;
        public Text txtPeriodicChecksum;
        public Text txtNowChecksum;
        public Button btnPlayer1;
        public Button btnPlayer2;
        public Button btnConnect;
        public VectorWarRunner runner;

        void Awake() {
            VectorWarRunner.OnStatus += OnStatus;
            VectorWarRunner.OnPeriodicChecksum += OnPeriodicChecksum;
            VectorWarRunner.OnNowChecksum += OnNowChecksum;
            btnConnect.onClick.AddListener(OnConnect);
            btnPlayer1.onClick.AddListener(OnPlayer1);
            btnPlayer2.onClick.AddListener(OnPlayer2);
            btnConnect.GetComponentInChildren<Text>().text = "Startup";
        }

        void OnPlayer1() {
            runner.DisconnectPlayer(0);
        }

        void OnPlayer2() {
            runner.DisconnectPlayer(1);
        }

        void OnConnect() {
            if (!runner.Running) {
                btnConnect.GetComponentInChildren<Text>().text = "Shutdown";
                runner.Startup();
            }
            else {
                btnConnect.GetComponentInChildren<Text>().text = "Startup";
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
