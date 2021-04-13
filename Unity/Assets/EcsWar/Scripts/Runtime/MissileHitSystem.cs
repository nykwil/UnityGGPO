using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Spaceship {

    public struct HitBuffer : IBufferElementData {
        public int damage;
    }

    public class PlayerHitProcessingSystem : SystemBase {

        protected override void OnUpdate() {
            Entities
                .WithoutBurst()
                .ForEach((Entity entity, ref LifeData player) => {
                    var lookup = GetBufferFromEntity<HitBuffer>();
                    var buffer = lookup[entity];
                    for (int i = 0; i < buffer.Length; ++i) {
                        player.Life -= buffer[i].damage;
                    }
                    buffer.Clear();
                }).Run();
        }
    }

    public struct HitInfo {
        public Entity projectileEnt;
        public Entity hitEntity;
    }

    public class MissileHitSystem : SystemBase {
        private BuildPhysicsWorld m_BuildPhysicsWorld;

        protected override void OnCreate() {
            base.OnCreate();
            m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
        }

        protected override void OnUpdate() {
            PhysicsWorld physicsWorld = m_BuildPhysicsWorld.PhysicsWorld;
            CollisionWorld collisionWorld = m_BuildPhysicsWorld.PhysicsWorld.CollisionWorld;
            var queue = new NativeQueue<HitInfo>(Allocator.TempJob);
            var pl = queue.AsParallelWriter();
            Entities
                .ForEach((Entity missileEntity, ref Missile missile, ref Translation position, ref Rotation rotation) => {
                    unsafe {
                        var input = new ColliderCastInput {
                            Collider = (Collider*)missile.collider.GetUnsafePtr(),
                            Orientation = quaternion.identity,
                            Start = position.Value,
                            End = position.Value
                        };
                        if (physicsWorld.CastCollider(input, out ColliderCastHit colliderHit)) {
                            pl.Enqueue(new HitInfo() {
                                hitEntity = physicsWorld.Bodies[colliderHit.RigidBodyIndex].Entity,
                                projectileEnt = missileEntity
                            });
                            var hitEntity = physicsWorld.Bodies[colliderHit.RigidBodyIndex].Entity;
                        }
                    }
                }).Run();

            while (queue.TryDequeue(out var item)) {
                EntityManager.DestroyEntity(item.projectileEnt);
                var lookup = GetBufferFromEntity<HitBuffer>();
                if (lookup.HasComponent(item.hitEntity)) {
                    var buffer = lookup[item.hitEntity];
                    if (buffer.IsCreated) {
                        buffer.Add(new HitBuffer() {
                            damage = 1
                        });
                    }
                }
            }
            queue.Dispose();
        }
    }
}