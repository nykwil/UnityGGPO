using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace EcsWar {

    public class PlayerComponent : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs {
        public Player player;
        public GameObject BoltPrefab = null;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
            var p = player;
            p.BoltPrefabEntity = BoltPrefab != null ? conversionSystem.GetPrimaryEntity(BoltPrefab) : Entity.Null;
            dstManager.AddComponentData(entity, p);
            dstManager.AddComponentData(entity, new MoveData());
            dstManager.AddComponentData(entity, new LifeData() {
                Life = p.MaxLife
            });
            dstManager.AddComponentData(entity, new ActiveInput {
                Reverse = false,
                Accelerate = false,
                Left = true,
                Right = false,
                Shoot = true
            });
            dstManager.AddBuffer<HitBuffer>(entity);
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs) {
            if (BoltPrefab != null) {
                referencedPrefabs.Add(BoltPrefab);
            }
        }
    }
}