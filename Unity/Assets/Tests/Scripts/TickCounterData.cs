using Unity.Entities;

namespace Tests {

    [GenerateAuthoringComponent]
    public struct TickCounterData : IComponentData {
        public int tickCount;
    }
}