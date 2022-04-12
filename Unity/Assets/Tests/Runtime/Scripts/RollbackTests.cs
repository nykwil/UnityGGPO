using Unity.Entities;
using UnityEngine;

namespace Tests {

    public class RollbackTests : MonoBehaviour {
        public bool pressedRollback;
        public bool pressedSnapshot;

        public bool autoMode;

        private World simulationWorld;

        private World activeWorld;

        private void Awake() {
            activeWorld = World.DefaultGameObjectInjectionWorld;
        }

        private void Update() {
            if (pressedRollback) {
                Debug.Log("pressedRollback");
                pressedRollback = false;
                RestoreSimulationWorld();
            }
            if (pressedSnapshot) {
                Debug.Log("pressedSnapshot");
                pressedSnapshot = false;
                SaveSimulationWorld();
            }
            if (autoMode) {
                if (Time.frameCount % 10 == 0) {
                    SaveSimulationWorld();
                }
                else if (Time.frameCount % 7 == 0) {
                    RestoreSimulationWorld();
                }
            }
        }

        private void SaveSimulationWorld() {
            simulationWorld = BackupWorld();
        }

        private void RestoreSimulationWorld() {
            if (simulationWorld != null) {
                CopyWorld(activeWorld, simulationWorld);
                simulationWorld.Dispose();
                simulationWorld = null;
            }
        }

        public void CopyWorld(World snapShotWorld, World world) {
            //snapShotWorld.EntityManager.DestroyAndResetAllEntities();
            snapShotWorld.EntityManager.CopyAndReplaceEntitiesFrom(world.EntityManager);
            snapShotWorld.SetTime(new Unity.Core.TimeData(world.Time.ElapsedTime, world.Time.DeltaTime));
        }

        public World BackupWorld() {
            var world = new World("lockStepWorld", WorldFlags.Simulation);
            world.EntityManager.CopyAndReplaceEntitiesFrom(activeWorld.EntityManager);
            world.SetTime(new Unity.Core.TimeData(activeWorld.Time.ElapsedTime, activeWorld.Time.DeltaTime));
            return world;
        }
    }
}