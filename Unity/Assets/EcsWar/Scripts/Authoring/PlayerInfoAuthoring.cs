using Unity.Entities;
using UnityEngine;

namespace EcsWar
{
    public class PlayerInfoAuthoring : MonoBehaviour
    {
        public PlayerInfo playerInfo;
        public GameObject BoltPrefab = null;

        public class MyBaker : Baker<PlayerInfoAuthoring>
        {
            public override void Bake(PlayerInfoAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new PlayerData
                {
                    ElapsedTime = 0,
                    PlayerIndex = 0,
                    BoltPrefabEntity = authoring.BoltPrefab != null ? GetEntity(authoring.BoltPrefab, TransformUsageFlags.Dynamic) : Entity.Null,
                });

                AddComponent(entity, new ActiveInput
                {
                    Reverse = false,
                    Accelerate = false,
                    Left = false,
                    Right = false,
                    Shoot = false
                });
                AddBuffer<HitBuffer>(entity);
                AddSharedComponent(entity, authoring.playerInfo);
            }
        }
    }
}