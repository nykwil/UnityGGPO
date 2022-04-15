using Unity.Entities;

namespace EcsWar {

    [GenerateAuthoringComponent]
    public struct LifeDecayData : IComponentData {
        public int Life;
    }
}