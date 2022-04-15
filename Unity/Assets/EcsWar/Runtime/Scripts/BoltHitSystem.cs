using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace EcsWar {

    public partial class PlayerHitProcessingSystem : SystemBase {

        protected override void OnUpdate() {
            Entities
                .WithoutBurst()
                .ForEach((Entity entity, ref LifeData lifeData, ref DynamicBuffer<HitBuffer> buffer) => {
                    for (int i = 0; i < buffer.Length; ++i) {
                        lifeData.Life -= buffer[i].Damage;
                    }
                    buffer.Clear();
                }).Run();
        }
    }

    public partial class LifDataLifeSystem : SystemBase {
        private EntityCommandBufferSystem _entityCommandBufferSystem;

        protected override void OnCreate() {
            _entityCommandBufferSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate() {
            var ecb = _entityCommandBufferSystem.CreateCommandBuffer();
            Entities
                .ForEach((Entity entity, ref LifeData lifeData) => {
                    if (lifeData.Life <= 0) {
                        ecb.DestroyEntity(entity);
                    }
                }).Schedule();
            _entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }

    public partial class LifDataDecaySystem : SystemBase {

        protected override void OnUpdate() {
            Entities
                .ForEach((Entity entity, ref LifeData lifeData, ref LifeDecayData decay) => {
                    lifeData.Life -= decay.Life;
                }).Schedule();
        }
    }

    public struct HitInfo {
        public Entity projectileEnt;
        public Entity hitEntity;
    }

    // @TODO burstify this
    public partial class BoltHitSystem : SystemBase {
        private EntityQuery playerQuery;

        protected override void OnCreate() {
            base.OnCreate();
            playerQuery = GetEntityQuery(
                ComponentType.ReadOnly<Player>(),
                ComponentType.ReadOnly<Translation>());
        }

        protected override void OnUpdate() {
            var plEntityList = playerQuery.ToEntityArray(Allocator.TempJob);
            var plPlayerList = playerQuery.ToComponentDataArray<Player>(Allocator.TempJob);
            var plPosList = playerQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
            var queue = new NativeQueue<HitInfo>(Allocator.TempJob);

            var pl = queue.AsParallelWriter();
            for (int i = 0; i < plEntityList.Length; ++i) {
                Entity playerEntity = plEntityList[i];
                Player player = plPlayerList[i];
                Translation playerPos = plPosList[i];
                Entities
                    .ForEach((Entity boltEntity, ref Bolt bolt, ref Translation boltPos) => {
                        if (player.PlayerIndex != bolt.PlayerIndex) {
                            if (math.distance(playerPos.Value, boltPos.Value) < bolt.Radius + player.Radius) {
                                pl.Enqueue(new HitInfo() {
                                    hitEntity = playerEntity,
                                    projectileEnt = boltEntity
                                });
                            }
                        }
                    }).Run();
            }

            while (queue.TryDequeue(out var item)) {
                EntityManager.DestroyEntity(item.projectileEnt);
                var lookup = GetBufferFromEntity<HitBuffer>();
                if (lookup.HasComponent(item.hitEntity)) {
                    var buffer = lookup[item.hitEntity];
                    if (buffer.IsCreated) {
                        buffer.Add(new HitBuffer() {
                            Damage = 1
                        });
                    }
                }
            }

            queue.Dispose();
            plEntityList.Dispose();
            plPlayerList.Dispose();
            plPosList.Dispose();
        }
    }
}