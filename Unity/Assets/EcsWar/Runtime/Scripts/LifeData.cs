using Unity.Entities;

namespace EcsWar {

    [GenerateAuthoringComponent]
    public struct LifeData : IComponentData {
        public int Life;
    }
}