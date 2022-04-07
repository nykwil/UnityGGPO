using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Tests {
    public partial class RotationSpeedSystem : SystemBase {

        protected override void OnUpdate() {
            Entities
                .ForEach((ref Rotation rotation, in RotationSpeedData rotSpeed) => {
                    rotation.Value = math.mul(math.normalize(rotation.Value), quaternion.AxisAngle(math.up(), rotSpeed.radiansPerTick));
                }).ScheduleParallel();
        }
    }
}