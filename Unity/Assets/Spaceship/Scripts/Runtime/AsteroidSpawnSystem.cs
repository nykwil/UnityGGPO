using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Unity.Spaceship
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    internal class AsteroidSpawnSystem : ComponentSystem
    {
        private float m_ElapsedTime;
        private Mathematics.Random m_Random;

        protected override void OnCreate()
        {
            base.OnCreate();

            RequireSingletonForUpdate<AsteroidSpawner>();

            m_Random = new Mathematics.Random(314159);
        }

        protected override void OnUpdate()
        {
            m_ElapsedTime += Time.DeltaTime;

            var settings = GetSingleton<AsteroidSpawner>();

            // if (settings.Rate < 1)
            var timeLimit = 1.0f / settings.Rate;

            if (m_ElapsedTime > timeLimit)
            {
                // world view
                // find a point somewhere outside of view,
                var rot = quaternion.RotateZ(m_Random.NextFloat(2 * math.PI));
                var pos = new float3(settings.Max.x, settings.Max.y, 0);
                pos = math.mul(rot, pos);

                // aim it directly at the camera's position
                var dir = math.normalize(float3.zero - pos) * m_Random.NextFloat(settings.MinSpeed, settings.MaxSpeed);

                // vary the aim by a a little to miss the center
                rot = quaternion.RotateZ(m_Random.NextFloat(settings.PathVariation * 2 * math.PI)); // 10% variation
                // vary the speed as well
                dir = math.mul(rot, dir);

                var ast = PostUpdateCommands.Instantiate(settings.Prefab);

                PostUpdateCommands.SetComponent(ast, new PhysicsVelocity
                {
                    Linear = new float3(dir.x, 0, dir.y)
                });

                PostUpdateCommands.SetComponent(ast, new Translation
                {
                    Value = pos
                });

                m_ElapsedTime = 0;
            }
        }
    }
}