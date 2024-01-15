using Unity.Entities;

namespace Tests {

    public struct TickCounterData : IComponentData {
        public int tickCount;
    }
}