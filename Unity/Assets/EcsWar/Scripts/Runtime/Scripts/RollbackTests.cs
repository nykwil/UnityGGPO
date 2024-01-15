using Unity.Entities;
using UnityEngine;
using Unity.Burst;

namespace Tests {

    public class RollbackTests : MonoBehaviour {
        public bool pressedRollback;
        public bool pressedSnapshot;

        private World simulationWorld;

        public World activeWorld;

        private void Awake() {
            activeWorld = World.DefaultGameObjectInjectionWorld;
        }

        private void Update() {
            if (pressedRollback) {
                Debug.Log("pressedRollback");
                pressedRollback = false;
                CreateSnapShot(activeWorld, simulationWorld);
            }
            if (pressedSnapshot) {
                Debug.Log("pressedSnapshot");
                pressedSnapshot = false;
                simulationWorld = SaveWorld();
            }
        }

        public void CreateSnapShot(World snapShotWorld, World world) {
            snapShotWorld.EntityManager.DestroyAndResetAllEntities();
            snapShotWorld.EntityManager.CopyAndReplaceEntitiesFrom(world.EntityManager);
            snapShotWorld.SetTime(new Unity.Core.TimeData(world.Time.ElapsedTime, world.Time.DeltaTime));
        }

        public World SaveWorld() {
            var world = new World("lockStepWorld", WorldFlags.Simulation);
            world.EntityManager.CopyAndReplaceEntitiesFrom(activeWorld.EntityManager);
            world.SetTime(new Unity.Core.TimeData(activeWorld.Time.ElapsedTime, activeWorld.Time.DeltaTime));
            return world;
        }

        private void SimulateTicks(int tickNumber) {
            Debug.Log("Simulating the next " + tickNumber + " ticks");
            for (int i = 0; i < tickNumber; i++) {
                simulationWorld.Update();
            }
        }
    }
}