using SharedGame;
using System.Collections.Generic;
using Unity.Entities;

namespace EcsWar {

    public class EcsGameManager : GameManager {
        private World activeWorld;

        private void Awake() {
            activeWorld = World.DefaultGameObjectInjectionWorld;
            var simGroup = activeWorld.GetExistingSystem<SimulationSystemGroup>();
            simGroup.Enabled = false;
        }

        protected override IGame CreateLocalGame() {
            return new LocalGame(new EcsGameState(activeWorld));
        }

        protected override IGame CreateGGPOGame(IPerfUpdate perfPanel, IList<Connections> connections, int playerIndex) {
            var game = new GGPOGame("ecs-game", new EcsGameState(activeWorld), perfPanel);
            game.Init(connections, playerIndex);
            return game;
        }
    }
}