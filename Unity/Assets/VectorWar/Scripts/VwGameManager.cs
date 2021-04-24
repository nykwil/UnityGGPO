using SharedGame;
using System.Collections.Generic;
using UnityGGPO;

namespace VectorWar {

    public class VwGameManager : GameManager {

        public override void StartLocalGame() {
            Game = new LocalGame(new VwGameState(2));
        }

        public override void StartGGPOGame(IPerfUpdate perfPanel, IList<Connections> connections, int playerIndex) {
            var game = new GGPOGame("vectorwar", new VwGameState(connections.Count), perfPanel);
            game.Init(connections, playerIndex);
            Game = game;
        }
    }
}