using Unity.Entities;
using UnityEngine;

namespace Tests {
    public class SystemTicker3 : MonoBehaviour {
        World world;
        private PresentationSystemGroup clientRender;
        private SimulationSystemGroup clientSim;

        private void Awake() {
            world = DefaultWorldInitialization.Initialize("Server World", false);
            clientSim = world.GetExistingSystem<SimulationSystemGroup>();
            clientRender = world.GetExistingSystem<PresentationSystemGroup>();
        }

        private void Update() {
            clientRender.Update();
        }

        private void FixedUpdate() {
            world.Update();
            clientSim.Update();
        }
    }
}