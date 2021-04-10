using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SharedGame {

    public class ConnectionPanel : MonoBehaviour {
        public Button btnLocal;
        public Button btnRemote;
        public Button btnHost;
        public Text inpIps;

        private GameManager runner => GameManager.Instance;
        private GgpoPerformancePanel perf;

        public static void LogTodo(string s) {
            // @TODO
            Debug.Log(s);
        }

        private void Awake() {
            perf = FindObjectOfType<GgpoPerformancePanel>();
            perf.Setup();
            btnHost.onClick.AddListener(OnHostClick);
            btnRemote.onClick.AddListener(OnRemoteClick);
            btnLocal.onClick.AddListener(OnLocalClick);
        }

        private void OnDestroy() {
            btnHost.onClick.RemoveListener(OnHostClick);
            btnRemote.onClick.RemoveListener(OnRemoteClick);
            btnLocal.onClick.RemoveListener(OnLocalClick);
        }

        private List<Connections> GetConnections() {
            var list = new List<Connections>();
            var split = inpIps.text.Split(':');
            list.Add(new Connections() {
                port = ushort.Parse(split[1]),
                ip = split[0],
                spectator = false
            });
            return list;
        }

        private void OnHostClick() {
            var game = runner.CreateGGPOGame(perf, LogTodo, GetConnections(), 0);
            runner.Startup(game);
        }

        private void OnRemoteClick() {
            var game = runner.CreateGGPOGame(perf, LogTodo, GetConnections(), 1);
            runner.Startup(game);
        }

        private void OnLocalClick() {
            var game = runner.CreateLocalGame();
            runner.Startup(game);
        }
    }
}