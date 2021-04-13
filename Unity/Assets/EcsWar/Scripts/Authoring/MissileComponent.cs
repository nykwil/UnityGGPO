using Unity.Entities;
using UnityEngine;

namespace Spaceship {

    public class MissileComponent : MonoBehaviour, IConvertGameObjectToEntity {

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
            dstManager.AddComponentData(entity, new Missile {
            });
            dstManager.AddComponentData(entity, new MoveData {
            });
            dstManager.AddBuffer<HitBuffer>(entity);
        }
    }
}
