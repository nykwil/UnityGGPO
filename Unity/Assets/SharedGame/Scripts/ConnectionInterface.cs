using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SharedGame {

    public class ConnectionInterface : MonoBehaviour {
        public InputField[] inpIps;
        public Toggle[] tglSpectators;
        public InputField inpPlayerIndex;
        public Toggle tgLocal;
        public Button btnConnect;

        private List<Connections> connections = new List<Connections>();
        private int playerIndex;
        private GameManager runner => GameManager.Instance;

        private void Awake() {
            runner.OnRunningChanged += OnRunningChanged;
            btnConnect.onClick.AddListener(OnConnect);
            connections.Add(new Connections() {
                ip = "127.0.0.1",
                port = 7000,
                spectator = false
            });
            connections.Add(new Connections() {
                ip = "127.0.0.1",
                port = 7001,
                spectator = false
            });
            playerIndex = 0;
            Load();
        }

        private void OnConnect() {
            runner.Startup(CreateGame());
        }

        private void OnDestroy() {
            runner.OnRunningChanged -= OnRunningChanged;
            btnConnect.onClick.RemoveListener(OnConnect);
        }

        private void OnRunningChanged(bool obj) {
            gameObject.SetActive(!obj);
        }

        public void Load() {
            for (int i = 0; i < connections.Count; ++i) {
                inpIps[i].text = connections[i].ip + ":" + connections[i].port;
                tglSpectators[i].isOn = connections[i].spectator;
            }

            inpPlayerIndex.text = playerIndex.ToString();
        }

        public void Save() {
            for (int i = 0; i < connections.Count; ++i) {
                var split = inpIps[i].text.Split(':');
                connections[i].ip = split[0];
                connections[i].port = ushort.Parse(split[1]);
                connections[i].spectator = tglSpectators[i].isOn;
            }

            playerIndex = int.Parse(inpPlayerIndex.text);
        }

        public static void LogTodo(string s) {
            // @TODO
            Debug.Log(s);
        }

        public IGame CreateGame() {
            if (tgLocal.isOn) {
                return GameManager.Instance.CreateLocalGame();
            }
            else {
                Save();
                var perf = FindObjectOfType<GgpoPerformancePanel>();
                perf.Setup();
                return GameManager.Instance.CreateGGPOGame(perf, LogTodo, connections, playerIndex);
            }
        }
    }
}