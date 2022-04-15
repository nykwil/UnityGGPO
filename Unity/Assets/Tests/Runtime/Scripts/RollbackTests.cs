using Unity.Entities;
using UnityEngine;

namespace Tests {

    public class RollbackTests : MonoBehaviour {
        private bool autoMode;
        private World lockStepWorld;
        private World activeWorld;

        private void Awake() {
            activeWorld = World.DefaultGameObjectInjectionWorld;
            lockStepWorld = new World("lockStepWorld", WorldFlags.Simulation);
        }

        private void Update() {
            if (autoMode) {
                if (Time.frameCount % 10 == 0) {
                    SaveSimulationWorld();
                }
                else if (Time.frameCount % 7 == 0) {
                    RestoreSimulationWorld();
                }
            }
        }

        [InspectorButton]
        private void EnableAutoMode() {
            SaveSimulationWorld();
            autoMode = true;
        }

        [InspectorButton]
        private void SaveSimulationWorld() {
            CopyWorld(lockStepWorld, activeWorld);
        }

        [InspectorButton]
        private void RestoreSimulationWorld() {
            CopyWorld(activeWorld, lockStepWorld);
        }

        public void CopyWorld(World toWorld, World fromWorld) {
            //snapShotWorld.EntityManager.DestroyAndResetAllEntities();
            toWorld.EntityManager.CopyAndReplaceEntitiesFrom(fromWorld.EntityManager);
            toWorld.SetTime(new Unity.Core.TimeData(fromWorld.Time.ElapsedTime, fromWorld.Time.DeltaTime));
        }

        public World BackupWorld() {
            var world = new World("lockStepWorld", WorldFlags.Simulation);
            world.EntityManager.CopyAndReplaceEntitiesFrom(activeWorld.EntityManager);
            world.SetTime(new Unity.Core.TimeData(activeWorld.Time.ElapsedTime, activeWorld.Time.DeltaTime));
            return world;
        }
    }
}