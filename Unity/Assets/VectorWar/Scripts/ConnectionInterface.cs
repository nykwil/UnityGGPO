using UnityEngine;
using UnityEngine.UI;

namespace VectorWar {

    public class ConnectionInterface : MonoBehaviour {
        public InputField[] inpIps;
        public Toggle[] tglSpectators;
        public InputField inpPlayerIndex;
        public Toggle tgLocal;

        public void Load(OnlineGame runner) {
            for (int i = 0; i < runner.connections.Count; ++i) {
                inpIps[i].text = runner.connections[i].ip + ":" + runner.connections[i].port;
                tglSpectators[i].isOn = runner.connections[i].spectator;
            }

            inpPlayerIndex.text = runner.PlayerIndex.ToString();
        }

        public void Save(OnlineGame runner) {
            for (int i = 0; i < runner.connections.Count; ++i) {
                var split = inpIps[i].text.Split(':');
                runner.connections[i].ip = split[0];
                runner.connections[i].port = ushort.Parse(split[1]);
                runner.connections[i].spectator = tglSpectators[i].isOn;
            }

            runner.PlayerIndex = int.Parse(inpPlayerIndex.text);
        }

        public IGame CreateGame() {
            if (tgLocal.isOn) {
                return new LocalGame();
            }
            else {
                var game = new OnlineGame();
                Save(game);
                return game;
            }
        }
    }
}