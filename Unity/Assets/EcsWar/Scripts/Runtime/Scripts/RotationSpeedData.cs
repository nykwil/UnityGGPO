using Unity.Entities;
using UnityEngine;
using Unity.Transforms;

namespace Tests {

    public struct RotationSpeedData : IComponentData {
        public float radiansPerTick;
    }
}