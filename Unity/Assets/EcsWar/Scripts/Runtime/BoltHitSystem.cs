using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace EcsWar {

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

    public class BoltHitSystem : SystemBase {
        private EntityQuery bulletQuery;

        protected override void OnCreate() {
            base.OnCreate();
            bulletQuery = GetEntityQuery(
                ComponentType.ReadOnly<Player>(),
                ComponentType.ReadOnly<Translation>());
        }

        protected override void OnUpdate() {
            var plEntityList = bulletQuery.ToEntityArray(Allocator.TempJob);
            var plPlayerList = bulletQuery.ToComponentDataArray<Player>(Allocator.TempJob);
            var plPosList = bulletQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
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
                            damage = 1
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