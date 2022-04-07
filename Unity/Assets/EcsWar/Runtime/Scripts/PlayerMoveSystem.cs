using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace EcsWar {

    public partial class PlayerMoveSystem : SystemBase {

        protected override void OnUpdate() {
            Entities
                .ForEach((ref MoveData pv, in Player player, in Rotation rot, in ActiveInput activeInput) => {
                    // move player
                    if (activeInput.Left) {
                        pv.Angular = new float3(0, -player.RotationSpeed, 0);
                    }
                    else if (activeInput.Right) {
                        pv.Angular = new float3(0, player.RotationSpeed, 0);
                    }
                    else {
                        pv.Angular = new float3(0, 0, 0);
                    }

                    var pos = float3.zero;

                    if (activeInput.Accelerate) {
                        pos.z = player.MoveSpeed;
                    }
                    else if (activeInput.Reverse) {
                        pos.z = -player.MoveSpeed;
                    }

                    float3 friction = new float3(0);

                    var mag = math.length(pv.Linear);
                    if (mag > 0) {
                        var dir = math.normalize(pv.Linear);
                        friction = dir * (mag * mag * player.Friction);
                    }

                    pv.Linear += math.mul(rot.Value, pos) - friction;

                }).Run();
        }
    }
}