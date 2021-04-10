using SharedGame;
using System.Collections.Generic;

namespace VectorWar {

    public class VwGameManager : GameManager {

        public override IGame CreateLocalGame() {
            return new LocalGame(new VwGameState());
        }

        public override IGame CreateGGPOGame(IPerfUpdate perfPanel, GGPO.LogDelegate logDelegate, List<Connections> connections, int playerIndex) {
            var game = new GGPOGame("vectorwar", new VwGameState(), perfPanel, logDelegate);
            game.Init(connections, playerIndex);
            return game;
        }
    }
}