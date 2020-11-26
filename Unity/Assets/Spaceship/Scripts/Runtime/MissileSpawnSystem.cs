using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Unity.Spaceship
{
    internal class MissileEmitSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithStructuralChanges()
                .ForEach((Entity playerEntity, ref ActiveInput activeInput, ref Player player, ref Translation playerTr, ref Rotation playerRot) =>
                {
                    player.ElapsedTime += 1;
                    if (activeInput.Shoot)
                    {
                        if (player.ElapsedTime > player.FireRate)
                        {
                            var missile = EntityManager.Instantiate(player.MissilePrefab);

                            var sphere = SphereCollider.Create(new SphereGeometry
                            {
                                Center = float3.zero,
                                Radius = 0.5f,
                            }, new CollisionFilter()
                            {
                                BelongsTo = player.BelongsTo,
                                CollidesWith = player.CollidesWith,
                                GroupIndex = 0
                            });

                            EntityManager.SetComponentData(missile, new Missile()
                            {
                                collider = sphere,
                            });
                            EntityManager.SetComponentData(missile, new ForwardMove()
                            {
                                MoveSpeed = player.FireSpeed
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