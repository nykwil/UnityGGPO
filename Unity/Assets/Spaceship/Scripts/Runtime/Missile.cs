using Unity.Entities;
using Unity.Physics;

namespace Spaceship
{
    [GenerateAuthoringComponent]
    public struct Missile : IComponentData
    {
        public BlobAssetReference<Collider> collider;
    }
}