using Unity.Entities;

[GenerateAuthoringComponent]
public struct TickCounterData : IComponentData {
    public int tickCount;
}

