using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Tests {

    public class SystemTicker1 : MonoBehaviour {
        public bool isSimulating = false;
        private World activeWorld;
        private World lockStepWorld;
        private IEnumerable<ComponentSystemBase> preSystems;
        private bool shouldRollback;
        private IEnumerable<ComponentSystemBase> simSystems;
        public static void CopyWorld(World toWorld, World fromWorld) {
            //snapShotWorld.EntityManager.DestroyAndResetAllEntities();
            toWorld.EntityManager.CopyAndReplaceEntitiesFrom(fromWorld.EntityManager);
            toWorld.SetTime(new Unity.Core.TimeData(fromWorld.Time.ElapsedTime, fromWorld.Time.DeltaTime));
        }

        public void Update() {
            if (isSimulating) {
                foreach (var sys in preSystems) {
                    sys.Update();
                }
            }
        }

        private void Awake() {
            activeWorld = World.DefaultGameObjectInjectionWorld;
            lockStepWorld = new World("lockStepWorld", WorldFlags.Simulation);
            var simGroup = activeWorld.GetExistingSystem<SimulationSystemGroup>();
            var preGroup = activeWorld.GetExistingSystem<PresentationSystemGroup>();
            simSystems = simGroup.Systems;
            preSystems = preGroup.Systems;
            simGroup.Enabled = false;
            preGroup.Enabled = false;
        }
        [InspectorButton]
        private void EnableAutoMode() {
            SaveSimulationWorld();
            shouldRollback = true;
        }

        private void FixedUpdate() {
            if (isSimulating) {
                foreach (var sys in simSystems) {
                    sys.Update();
                }
            }
            if (shouldRollback) {
                if (Time.frameCount % 10 == 0) {
                    SaveSimulationWorld();
                }
                else if (Time.frameCount % 7 == 0) {
                    RestoreSimulationWorld();
                }
            }
        }
        [InspectorButton]
        private void RestoreSimulationWorld() {
            CopyWorld(activeWorld, lockStepWorld);
        }

        [InspectorButton]
        private void SaveSimulationWorld() {
            CopyWorld(lockStepWorld, activeWorld);
        }
    }
}