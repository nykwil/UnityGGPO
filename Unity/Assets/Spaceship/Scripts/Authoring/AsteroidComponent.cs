using Unity.Entities;
using Unity.Spaceship;
using UnityEngine;

public class AsteroidComponent : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Asteroid
        {
        });
        dstManager.AddBuffer<HitBuffer>(entity);
    }
}