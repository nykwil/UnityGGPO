using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Spaceship
{
    [GenerateAuthoringComponent]
    public struct MoveData : IComponentData
    {
        public float3 Angular;
        public float3 Linear;
    }
}