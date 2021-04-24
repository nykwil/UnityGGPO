using Unity.Entities;
using Unity.Mathematics;

namespace EcsWar {

    [GenerateAuthoringComponent]
    public struct MoveData : IComponentData {
        public float3 Angular;
        public float3 Linear;
    }
}