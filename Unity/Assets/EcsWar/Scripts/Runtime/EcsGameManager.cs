using SharedGame;
using System.Collections.Generic;

namespace EcsWar {

    public class EcsGameManager : GameManager {
        public EcsSceneInfo ecsSceneInfo;

        protected override IGame CreateLocalGame() {
            return new LocalGame(new EcsGameState(ecsSceneInfo));
        }

        protected override IGame CreateGGPOGame(IPerfUpdate perfPanel, IList<Connections> connections, int playerIndex) {
            var game = new GGPOGame("ecsgame", new EcsGameState(ecsSceneInfo), perfPanel);
            game.Init(connections, playerIndex);
            return game;
        }
    }
}