using SharedGame;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;

namespace EcsWar {

    public class EcsGameManager : GameManager {
        public UnityEngine.GameObject playerPrefab;
        public UnityEngine.Transform[] startingPoints;
        private World activeWorld;
        private Entity playerPrefabEntity;

        private void Awake() {
            activeWorld = World.DefaultGameObjectInjectionWorld;
            var simGroup = activeWorld.GetExistingSystem<SimulationSystemGroup>();
            simGroup.Enabled = false;
        }

        private void Start() {
            var settings = GameObjectConversionSettings.FromWorld(activeWorld, null);
            playerPrefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(playerPrefab, settings);
        }

        public void CreateScene() {
            for (int i = 0; i < startingPoints.Length; ++i) {
                var ent = activeWorld.EntityManager.Instantiate(playerPrefabEntity);
                var playerData = activeWorld.EntityManager.GetComponentData<Player>(ent);
                var posData = activeWorld.EntityManager.GetComponentData<Translation>(ent);

                posData.Value = startingPoints[i].position;
                playerData.PlayerIndex = i;

                activeWorld.EntityManager.SetComponentData(ent, playerData);
                activeWorld.EntityManager.SetComponentData(ent, posData);
            }
        }

        protected override IGame CreateLocalGame() {
            CreateScene();
            return new LocalGame(new EcsGameState(activeWorld));
        }

        protected override IGame CreateGGPOGame(IPerfUpdate perfPanel, IList<Connections> connections, int playerIndex) {
            var game = new GGPOGame("ecsgame", new EcsGameState(activeWorld), perfPanel);
            game.Init(connections, playerIndex);
            return game;
        }
    }
}