using Unity.Entities;

namespace EcsWar {

    [GenerateAuthoringComponent]
    public struct Bolt : IComponentData {
        public int PlayerIndex;
        public float Radius;
    }
}