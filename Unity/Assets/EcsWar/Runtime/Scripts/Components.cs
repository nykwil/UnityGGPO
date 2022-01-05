using Unity.Entities;

namespace EcsWar {

    public struct HitBuffer : IBufferElementData {
        public int Damage;
    }

    public struct ActiveInput : IComponentData {
        public bool Reverse;
        public bool Accelerate;
        public bool Left;
        public bool Right;
        public bool Shoot;
    }

    public struct LifeData : IComponentData {
        public int Life;
    }

    [System.Serializable]
    public struct Player : IComponentData {

        // Constants
        public float RotationSpeed;
        public float MoveSpeed;
        public int FireRate;
        public float FireSpeed;
        public int MaxLife;
        public float Radius;
        public float Friction;

        // Data
        [UnityEngine.HideInInspector]
        public int ElapsedTime;

        [UnityEngine.HideInInspector]
        public int PlayerIndex;

        [UnityEngine.HideInInspector]
        public Entity BoltPrefabEntity;
    }
}