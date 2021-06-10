using EcsWar;
using Unity.Entities;
using UnityEngine;

public class BoltComponent : MonoBehaviour, IConvertGameObjectToEntity {
    public BoltInfo infoData;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
        dstManager.AddSharedComponentData(entity, infoData);
    }
}