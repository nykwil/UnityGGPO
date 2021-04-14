using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace EcsWar
{
    public class ForwardMoveSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .ForEach((ref MoveData move, ref Translation tr, ref Rotation rot) =>
                {
                    rot.Value = math.mul(rot.Value, quaternion.Euler(move.Angular));           
                    tr.Value = tr.Value + move.Linear;
                }).Schedule();
        }
    }
}