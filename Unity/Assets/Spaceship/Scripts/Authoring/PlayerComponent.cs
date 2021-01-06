using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Spaceship {

    public class PlayerComponent : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs {
        public float RotationSpeed = 2f;
        public float MoveSpeed = 4f;
        public int FireRate = 3;
        public float FireSpeed = 5f;
        public int PlayerIndex;
        public GameObject MissilePrefab = null;

        public PhysicsShapeAuthoring physicsShape;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
            var BelongsTo = physicsShape.BelongsTo.Value;
            var CollidesWith = physicsShape.CollidesWith.Value;

            var sphere = Unity.Physics.SphereCollider.Create(new SphereGeometry {
                Center = float3.zero,
                Radius = 0.5f,
            }, new CollisionFilter() {
                BelongsTo = BelongsTo,
                CollidesWith = CollidesWith,
                GroupIndex = 0
            });

            dstManager.AddComponentData(entity, new Player {
                RotationSpeed = RotationSpeed,
                MoveSpeed = MoveSpeed,
                FireRate = FireRate,
                FireSpeed = FireSpeed,
                ElapsedTime = 0,
                PlayerIndex = PlayerIndex,
                BelongsTo = BelongsTo,
                CollidesWith = CollidesWith,
                MissileCollider = sphere,
                MissilePrefab = MissilePrefab != null ? conversionSystem.GetPrimaryEntity(MissilePrefab) : Entity.Null
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
            if (MissilePrefab != null)
                referencedPrefabs.Add(MissilePrefab);
        }
    }
}
