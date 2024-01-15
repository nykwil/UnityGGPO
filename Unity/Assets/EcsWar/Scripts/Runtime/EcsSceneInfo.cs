using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Scenes;
using UnityEngine;

namespace EcsWar {

    [CreateAssetMenu]
    public class EcsSceneInfo : ScriptableObject {
        public GameObject playerPrefab;
        public Vector3[] startingPoints;
        public EntitySceneReference SceneReference;
        public UnityEditor.SceneAsset Scene;
        private EntitySceneReference reference;

        public void CreateScene(World activeWorld) {
            reference = new EntitySceneReference(Scene);

            activeWorld.EntityManager.DestroyAndResetAllEntities();
            SceneSystem.LoadSceneAsync(activeWorld.Unmanaged, reference);
        }
    }
}