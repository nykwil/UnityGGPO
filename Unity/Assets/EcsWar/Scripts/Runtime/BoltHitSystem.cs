using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace EcsWar {

    public struct HitBuffer : IBufferElementData {
        public int damage;
    }

    public partial class PlayerHitProcessingSystem : SystemBase {

        protected override void OnUpdate() {
            Entities
                .WithoutBurst()
                .ForEach((Entity entity, ref LifeData player) => {
                    var lookup = GetBufferLookup<HitBuffer>();
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

    public partial class BoltHitSystem : SystemBase {
        private EntityQuery playerQuery;
        private List<PlayerInfo> plInfos = new List<PlayerInfo>();

        protected override void OnCreate() {
            base.OnCreate();
            playerQuery = GetEntityQuery(
                ComponentType.ReadOnly<PlayerInfo>(),
                ComponentType.ReadOnly<PlayerData>(),
                ComponentType.ReadOnly<LocalTransform>());
        }

        protected override void OnUpdate() {
            EntityManager.GetAllUniqueSharedComponentsManaged(plInfos);
            for (int piIndex = 0; piIndex < plInfos.Count; ++piIndex) {
                playerQuery.SetSharedComponentFilter(plInfos[piIndex]);
                var plEntityList = playerQuery.ToEntityArray(Allocator.TempJob);
                var plDataList = playerQuery.ToComponentDataArray<PlayerData>(Allocator.TempJob);
                var plPosList = playerQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
                var queue = new NativeQueue<HitInfo>(Allocator.TempJob);

                var pl = queue.AsParallelWriter();
                for (int i = 0; i < plEntityList.Length; ++i) {
                    PlayerInfo playerInfo = plInfos[piIndex];
                    Entity playerEntity = plEntityList[i];
                    LocalTransform playerLts = plPosList[i];
                    var playerData = plDataList[i];
                    Entities
                        .ForEach((Entity boltEntity, in BoltInfo boltInfo, in BoltData bolt, in LocalTransform boltLts) => {
                            if (playerData.PlayerIndex != bolt.PlayerIndex) {
                                if (math.distance(playerLts.Position, boltLts.Position) < boltInfo.Radius + playerInfo.Radius) {
                                    pl.Enqueue(new HitInfo() {
                                        hitEntity = playerEntity,
                                        projectileEnt = boltEntity
                                    });
                                }
                            }
                        }).WithoutBurst().Run();
                }

                while (queue.TryDequeue(out var item)) {
                    EntityManager.DestroyEntity(item.projectileEnt);
                    var lookup = GetBufferLookup<HitBuffer>();
                    if (lookup.HasBuffer(item.hitEntity)) {
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
                plDataList.Dispose();
                plPosList.Dispose();
            }
        }
    }
}