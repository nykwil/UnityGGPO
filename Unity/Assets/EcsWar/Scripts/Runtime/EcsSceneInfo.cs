using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace EcsWar {

    [CreateAssetMenu]
    public class EcsSceneInfo : ScriptableObject {
        public GameObject playerPrefab;
        public Vector3[] startingPoints;

        public void CreateScene(World activeWorld) {
            activeWorld.EntityManager.DestroyAndResetAllEntities();
            var settings = GameObjectConversionSettings.FromWorld(activeWorld, null);
            var playerPrefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(playerPrefab, settings);
            for (int i = 0; i < startingPoints.Length; ++i) {
                var ent = activeWorld.EntityManager.Instantiate(playerPrefabEntity);
                var playerData = activeWorld.EntityManager.GetComponentData<Player>(ent);
                var posData = activeWorld.EntityManager.GetComponentData<Translation>(ent);

                posData.Value = startingPoints[i];
                playerData.PlayerIndex = i;

                activeWorld.EntityManager.SetComponentData(ent, playerData);
                activeWorld.EntityManager.SetComponentData(ent, posData);
            }
        }
    }
}