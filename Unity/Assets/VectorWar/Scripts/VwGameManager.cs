using SharedGame;
using System.Collections.Generic;

namespace VectorWar {

    public class VwGameManager : GameManager {

        protected override IGame CreateLocalGame() {
            return new LocalGame(new VwGameState(2));
        }

        protected override IGame CreateGGPOGame(IPerfUpdate perfPanel, IList<Connections> connections, int playerIndex) {
            var game = new GGPOGame("vectorwar", new VwGameState(connections.Count), perfPanel);
            game.Init(connections, playerIndex);
            return game;
        }
    }
}