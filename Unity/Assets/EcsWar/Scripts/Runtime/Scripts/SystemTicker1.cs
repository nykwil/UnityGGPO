using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Tests {

    public class SystemTicker1 : MonoBehaviour {
        private SystemHandle simGroup;
        public World world;
        public bool isSimulating = false;

        private void Start() {
            world = World.DefaultGameObjectInjectionWorld;
            var simState = world.Unmanaged.GetExistingSystemState<SimulationSystemGroup>();
            simGroup = world.GetExistingSystem<SimulationSystemGroup>();
            simState.Enabled = false;
        }

        private void FixedUpdate() {
            if (isSimulating) {
                simGroup.Update(world.Unmanaged);
            }
        }
    }
}