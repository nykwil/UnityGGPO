using Unity.Entities;
using Unity.Entities.Hybrid;

namespace Tests {

    [GenerateAuthoringComponent]
    public struct RotationSpeedData : IComponentData {
        public float radiansPerTick;
    }
}