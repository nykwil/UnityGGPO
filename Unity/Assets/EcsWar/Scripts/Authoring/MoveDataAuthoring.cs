using Unity.Entities;
using UnityEngine;

namespace EcsWar {

    public class MoveDataAuthoring : MonoBehaviour {
        public MoveData data;

        public class ComponentDataBaker : Baker<MoveDataAuthoring> {

            public override void Bake(MoveDataAuthoring authoring) {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, authoring.data);
            }
        }
    }
}