using SharedGame;
using System.Collections.Generic;
using UnityGGPO;

namespace EcsWar {

    public class EcsGameManager : GameManager {
        public EcsSceneInfo ecsSceneInfo;

        public override void StartLocalGame() {
            Game = new LocalGame(new EcsGameState(ecsSceneInfo));
        }

        public override void StartGGPOGame(IPerfUpdate perfPanel, IList<Connections> connections, int playerIndex) {
            var game = new GGPOGame("ecsgame", new EcsGameState(ecsSceneInfo), perfPanel);
            game.Init(connections, playerIndex);
            Game = game;
        }
    }
}