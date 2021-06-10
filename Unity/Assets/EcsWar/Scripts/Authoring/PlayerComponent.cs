using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace EcsWar {

    public class PlayerComponent : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs {
        public PlayerInfo playerInfo;
        public GameObject BoltPrefab = null;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
            dstManager.AddComponentData(entity, new PlayerData {
                ElapsedTime = 0,
                PlayerIndex = 0,
                BoltPrefabEntity = BoltPrefab != null ? conversionSystem.GetPrimaryEntity(BoltPrefab) : Entity.Null,
            });

            dstManager.AddComponentData(entity, new ActiveInput {
                Reverse = false,
                Accelerate = false,
                Left = false,
                Right = false,
                Shoot = false
            });
            dstManager.AddBuffer<HitBuffer>(entity);

            dstManager.AddSharedComponentData(entity, playerInfo);
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs) {
            if (BoltPrefab != null) {
                referencedPrefabs.Add(BoltPrefab);
            }
        }
    }
}