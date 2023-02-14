using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace EcsWar {

    public partial class MoveSystem : SystemBase {

        protected override void OnUpdate() {
            Entities
                .ForEach((ref Translation tr, ref Rotation rot, in MoveData move) => {
                    rot.Value = math.mul(rot.Value, quaternion.Euler(move.Angular));
                    tr.Value = tr.Value + move.Linear;
                }).Schedule();
        }
    }
}