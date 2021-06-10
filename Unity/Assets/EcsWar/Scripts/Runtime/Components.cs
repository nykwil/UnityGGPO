using Unity.Entities;

namespace EcsWar {

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

    public struct PlayerData : IComponentData {
        public int ElapsedTime;
        public int PlayerIndex;
        public Entity BoltPrefabEntity;
    }

    [System.Serializable]
    public struct PlayerInfo : ISharedComponentData {
        public float RotationSpeed;
        public float MoveSpeed;
        public int FireRate;
        public float FireSpeed;
        public float Radius;
        public int MaxLife;

        public override bool Equals(object obj) {
            return obj is PlayerInfo data &&
                   RotationSpeed == data.RotationSpeed &&
                   MoveSpeed == data.MoveSpeed &&
                   FireRate == data.FireRate &&
                   FireSpeed == data.FireSpeed &&
                   Radius == data.Radius &&
                   MaxLife == data.MaxLife;
        }

        public override int GetHashCode() {
            int hashCode = 1204851656;
            hashCode = hashCode * -1521134295 + RotationSpeed.GetHashCode();
            hashCode = hashCode * -1521134295 + MoveSpeed.GetHashCode();
            hashCode = hashCode * -1521134295 + FireRate.GetHashCode();
            hashCode = hashCode * -1521134295 + FireSpeed.GetHashCode();
            hashCode = hashCode * -1521134295 + Radius.GetHashCode();
            hashCode = hashCode * -1521134295 + MaxLife.GetHashCode();
            return hashCode;
        }
    }
}