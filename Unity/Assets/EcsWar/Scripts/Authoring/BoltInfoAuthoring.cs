using Unity.Entities;
using UnityEngine;

namespace EcsWar {

    public class BoltInfoAuthoring : MonoBehaviour {
        public BoltInfo data;

        public class ComponentDataBaker : Baker<BoltInfoAuthoring> {

            public override void Bake(BoltInfoAuthoring authoring) {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddSharedComponent(entity, authoring.data);
            }
        }
    }
}