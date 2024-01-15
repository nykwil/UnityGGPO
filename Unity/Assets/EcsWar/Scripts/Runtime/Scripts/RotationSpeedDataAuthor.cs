using Unity.Entities;
using UnityEngine;

namespace Tests {

    public class RotationSpeedDataAuthor : MonoBehaviour {
        public RotationSpeedData data;

        public class ComponentDataBaker : Baker<RotationSpeedDataAuthor> {

            public override void Bake(RotationSpeedDataAuthor authoring) {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, authoring.data);
            }
        }
    }
}