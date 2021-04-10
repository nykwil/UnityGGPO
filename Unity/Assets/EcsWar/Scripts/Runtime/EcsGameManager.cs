using SharedGame;
using System.Collections.Generic;

namespace EcsWar {

    public class EcsGameManager : GameManager {

        public override IGame CreateLocalGame() {
            return new LocalGame(new EcsGameState());
        }

        public override IGame CreateGGPOGame(IPerfUpdate perfPanel, GGPO.LogDelegate logDelegate, List<Connections> connections, int playerIndex) {
            var game = new GGPOGame("ecs-game", new EcsGameState(), perfPanel, logDelegate);
            game.Init(connections, playerIndex);
            return game;
        }
    }
}