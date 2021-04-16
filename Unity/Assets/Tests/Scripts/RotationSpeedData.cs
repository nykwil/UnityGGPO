using Unity.Entities;

namespace Tests {

    [GenerateAuthoringComponent]
    public struct RotationSpeedData : IComponentData {
        public float radiansPerTick;
    }
}