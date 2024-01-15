using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace EcsWar {

    public partial class BoltEmitSystem : SystemBase {

        protected override void OnUpdate() {
            Entities
                .WithStructuralChanges()
                .ForEach((Entity playerEntity, ref PlayerData playerData, in ActiveInput activeInput, in LocalTransform playerLts, in PlayerInfo playerInfo) => {
                    playerData.ElapsedTime += 1;
                    if (activeInput.Shoot) {
                        if (playerData.ElapsedTime > playerInfo.FireRate) {
                            var boltEnt = EntityManager.Instantiate(playerData.BoltPrefabEntity);

                            var data = EntityManager.GetComponentData<BoltData>(boltEnt);
                            data.PlayerIndex = playerData.PlayerIndex;
                            EntityManager.SetComponentData(boltEnt, data);

                            EntityManager.SetComponentData(boltEnt, new MoveData() {
                                Linear = math.mul(playerLts.Rotation, new float3(0, 0, playerInfo.FireSpeed))
                            });
                            EntityManager.SetComponentData(boltEnt, playerLts);
                            playerData.ElapsedTime = 0;
                        }
                    }
                }).Run();
        }
    }
}