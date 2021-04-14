using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace EcsWar {

    public class PlayerComponent : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs {
        public float RotationSpeed = 2f;
        public float MoveSpeed = 4f;
        public int FireRate = 3;
        public float FireSpeed = 5f;
        public int PlayerIndex;
        public GameObject BoltPrefab = null;
        public float Radius;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
            dstManager.AddComponentData(entity, new Player {
                RotationSpeed = RotationSpeed,
                MoveSpeed = MoveSpeed,
                FireRate = FireRate,
                FireSpeed = FireSpeed,
                ElapsedTime = 0,
                PlayerIndex = PlayerIndex,
                BoltPrefab = BoltPrefab != null ? conversionSystem.GetPrimaryEntity(BoltPrefab) : Entity.Null,
                Radius = Radius,
            });

            dstManager.AddComponentData(entity, new ActiveInput {
                Reverse = false,
                Accelerate = false,
                Left = false,
                Right = false,
                Shoot = false
            });
            dstManager.AddBuffer<HitBuffer>(entity);
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs) {
            if (BoltPrefab != null)
                referencedPrefabs.Add(BoltPrefab);
        }
    }
}