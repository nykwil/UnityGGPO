using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Spaceship
{
    public struct AsteroidSpawner : IComponentData
    {
        public Entity Prefab;
        public float Rate;
        public float MinSpeed;
        public float MaxSpeed;
        public float PathVariation;
        public float2 Max;
    }

    public struct ActiveInput : IComponentData
    {
        public bool Reverse;
        public bool Accelerate;
        public bool Left;
        public bool Right;
        public bool Shoot;
    }

    public struct LifeData : IComponentData 
    {
        public int Life;
    }

    public struct Player : IComponentData
    {
        public float RotationSpeed;
        public float MoveSpeed;
        public int FireRate;
        public float FireSpeed;
        public int ElapsedTime;
        public int PlayerIndex;
        public Entity MissilePrefab;
        public uint BelongsTo;
        public uint CollidesWith;
        public BlobAssetReference<Collider> MissileCollider;
        public int MaxLife;
    }
}