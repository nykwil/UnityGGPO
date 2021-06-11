using SharedGame;
using System.Collections.Generic;
using UnityGGPO;

namespace EcsWar {

    public class EcsGameManager : GameManager {
        public EcsSceneInfo ecsSceneInfo;
        public IGameRunner runner { get; set; }

        public override void StartLocalGame() {
            runner = new LocalRunner(new EcsGame(ecsSceneInfo));
            StartGame(runner);
        }

        public override void StartGGPOGame(IPerfUpdate perfPanel, IList<Connections> connections, int playerIndex) {
            runner = new GGPORunner("ecsgame", new EcsGame(ecsSceneInfo), perfPanel);
            ((GGPORunner)runner).Init(connections, playerIndex);
            StartGame(runner);
        }
    }
}