using Unity.Entities;
using UnityEngine;

namespace Tests {

    public class SystemTicker2 : MonoBehaviour {
        public World world;
        public ComponentSystemGroup simGroup;
        public bool isSimulating;

        private void Awake() {
            world = World.DefaultGameObjectInjectionWorld;
            simGroup = world.GetExistingSystem<SimulationSystemGroup>();
            simGroup.Enabled = false;
        }

        private void FixedUpdate() {
            if (isSimulating) {
                simGroup.Update();
            }
        }
    }
}