using Unity.Entities;
using UnityEngine;

namespace Tests {

    public class TickCounterDataAuthoring : MonoBehaviour {
        public TickCounterData data;

        public class ComponentDataBaker : Baker<TickCounterDataAuthoring> {

            public override void Bake(TickCounterDataAuthoring authoring) {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, authoring.data);
            }
        }
    }
}