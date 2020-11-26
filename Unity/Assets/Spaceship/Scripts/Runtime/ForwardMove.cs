using Unity.Entities;

namespace Unity.Spaceship
{
    [GenerateAuthoringComponent]
    public struct ForwardMove : IComponentData
    {
        public float MoveSpeed;
    }
}