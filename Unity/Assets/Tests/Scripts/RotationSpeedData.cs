using Unity.Entities;

[GenerateAuthoringComponent]
public struct RotationSpeedData : IComponentData {
    public float radiansPerTick;
}