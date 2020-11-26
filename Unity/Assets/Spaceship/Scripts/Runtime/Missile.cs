using Unity.Entities;
using Unity.Physics;

namespace Unity.Spaceship
{
    [GenerateAuthoringComponent]
    public struct Missile : IComponentData
    {
        public BlobAssetReference<Collider> collider;
    }
}