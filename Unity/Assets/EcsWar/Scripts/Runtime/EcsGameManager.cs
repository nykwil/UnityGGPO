using SharedGame;
using System.Collections.Generic;
using UnityGGPO;

namespace EcsWar {

    public class EcsGameManager : GameManager {
        public EcsSceneInfo ecsSceneInfo;

        public override void StartLocalGame() {
            StartGame(new LocalRunner(new EcsGame(ecsSceneInfo)));
        }

        public override void StartGGPOGame(IPerfUpdate perfPanel, IList<Connections> connections, int playerIndex) {
            var game = new GGPORunner("ecsgame", new EcsGame(ecsSceneInfo), perfPanel);
            game.Init(connections, playerIndex);
            StartGame(game);
        }
    }
}