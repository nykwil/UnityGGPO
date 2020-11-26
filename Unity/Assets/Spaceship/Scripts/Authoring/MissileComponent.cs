using Unity.Entities;
using Unity.Spaceship;
using UnityEngine;

public class MissileComponent : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Missile
        {
        });
        dstManager.AddComponentData(entity, new ForwardMove
        {
        });
        dstManager.AddBuffer<HitBuffer>(entity);
    }
}