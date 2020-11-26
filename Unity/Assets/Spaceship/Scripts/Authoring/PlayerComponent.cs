using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics.Authoring;
using Unity.Spaceship;
using UnityEngine;

public class PlayerComponent : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public float RotationSpeed = 2f;
    public float MoveSpeed = 4f;
    public int FireRate = 3;
    public float FireSpeed = 5f;
    public int PlayerIndex;
    public GameObject MissilePrefab = null;

    public PhysicsShapeAuthoring physicsShape;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Player
        {
            RotationSpeed = RotationSpeed,
            MoveSpeed = MoveSpeed,
            FireRate = FireRate,
            FireSpeed = FireSpeed,
            ElapsedTime = 0,
            PlayerIndex = PlayerIndex,
            BelongsTo = physicsShape.BelongsTo.Value,
            CollidesWith = physicsShape.CollidesWith.Value,
            MissilePrefab = MissilePrefab != null ? conversionSystem.GetPrimaryEntity(MissilePrefab) : Entity.Null
        });
        dstManager.AddComponentData(entity, new ActiveInput
        {
            Reverse = false,
            Accelerate = false,
            Left = false,
            Right = false,
            Shoot = false
        });
        dstManager.AddBuffer<HitBuffer>(entity);
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        if (MissilePrefab != null)
            referencedPrefabs.Add(MissilePrefab);
    }
}