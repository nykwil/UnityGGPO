using Unity.Entities;
using UnityEngine;

namespace Tests {

    public class SystemTicker2 : MonoBehaviour {
        public World world;
        public SystemHandle simGroup;
        public bool isSimulating;

        private void Awake() {
            world = World.DefaultGameObjectInjectionWorld;
            simGroup = world.GetExistingSystem<SimulationSystemGroup>();
            var simState = world.Unmanaged.GetExistingSystemState<SimulationSystemGroup>();
            simState.Enabled = false;
        }

        private void FixedUpdate() {
            if (isSimulating) {
                simGroup.Update(world.Unmanaged);
            }
        }
    }
}