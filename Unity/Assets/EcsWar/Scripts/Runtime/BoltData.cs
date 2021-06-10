using Unity.Entities;

namespace EcsWar {

    [System.Serializable]
    public struct BoltInfo : ISharedComponentData {
        public float Radius;

        public override bool Equals(object obj) {
            return obj is BoltInfo data &&
                   Radius == data.Radius;
        }

        public override int GetHashCode() {
            int hashCode = -2082133434;
            hashCode = hashCode * -1521134295 + Radius.GetHashCode();
            return hashCode;
        }
    }

    [GenerateAuthoringComponent]
    public struct BoltData : IComponentData {
        public int PlayerIndex;
    }
}