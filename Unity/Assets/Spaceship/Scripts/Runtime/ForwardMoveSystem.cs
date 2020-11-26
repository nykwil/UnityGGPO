using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Unity.Spaceship
{
    [UpdateAfter(typeof(KeyboardInputSystem))]
    public class ForwardMoveSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .ForEach((ref ForwardMove move, ref Translation tr, ref Rotation rot) =>
                {
                    var pos = float3.zero;
                    pos.z = move.MoveSpeed;

                    tr.Value += math.mul(rot.Value, pos);
                }).Schedule();
        }
    }
}