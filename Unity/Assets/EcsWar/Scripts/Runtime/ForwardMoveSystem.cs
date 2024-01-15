using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace EcsWar {

    public partial class ForwardMoveSystem : SystemBase {

        protected override void OnUpdate() {
            Entities
                .ForEach((ref LocalTransform lts, in MoveData move) => {
                    lts.Rotation = math.mul(lts.Rotation, quaternion.Euler(move.Angular));
                    lts.Position = lts.Position + move.Linear;
                }).Schedule();
        }
    }
}