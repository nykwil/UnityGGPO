using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Tests {

    public partial class RotationSpeedSystem : SystemBase {

        protected override void OnUpdate() {
            Entities
                .ForEach((ref LocalTransform lts, in RotationSpeedData rotSpeed) => {
                    lts.Rotation = math.mul(math.normalize(lts.Rotation), quaternion.AxisAngle(math.up(), rotSpeed.radiansPerTick));
                }).ScheduleParallel();
        }
    }
}