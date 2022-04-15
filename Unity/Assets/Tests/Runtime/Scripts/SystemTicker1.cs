using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Tests {

    public class SystemTicker1 : MonoBehaviour {
        private IEnumerable<ComponentSystemBase> simSystems;
        public World world;
        public bool isSimulating = false;

        private void Awake() {
            world = World.DefaultGameObjectInjectionWorld;
            var simGroup = world.GetExistingSystem<SimulationSystemGroup>();
            simSystems = simGroup.Systems;
            simGroup.Enabled = false;
        }

        private void FixedUpdate() {
            if (isSimulating) {
                foreach (var sys in simSystems) {
                    sys.Update();
                }
            }
        }
    }
}