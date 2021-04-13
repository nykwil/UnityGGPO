using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spaceship {

    internal class MissileEmitSystem : SystemBase {

        protected override void OnUpdate() {
            Entities
                .WithStructuralChanges()
                .ForEach((Entity playerEntity, ref ActiveInput activeInput, ref Player player, ref Translation playerTr, ref Rotation playerRot) => {
                    player.ElapsedTime += 1;
                    if (activeInput.Shoot) {
                        if (player.ElapsedTime > player.FireRate) {
                            var missile = EntityManager.Instantiate(player.MissilePrefab);

                            EntityManager.SetComponentData(missile, new Missile() {
                                collider = player.MissileCollider,
                            });
                            EntityManager.SetComponentData(missile, new MoveData() {
                                Linear = math.mul(playerRot.Value, new float3(0, 0, player.FireSpeed))
                            });
                            EntityManager.SetComponentData(missile, playerRot);
                            EntityManager.SetComponentData(missile, playerTr);
                            player.ElapsedTime = 0;
                        }
                    }
                }).Run();
        }
    }
}