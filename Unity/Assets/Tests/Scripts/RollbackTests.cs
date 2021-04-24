using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Tests {

    public class TickCounterSystem : SystemBase {
        private int systemTick;

        protected override void OnUpdate() {
            systemTick += 1;
            string s = "";
            Entities
                .ForEach((Entity entity, ref TickCounterData tickData) => {
                    tickData.tickCount += 1;
                    s += $"{entity} = {tickData.tickCount}\n";
                }).WithoutBurst().Run();

            Debug.Log($"-- System Tick = {systemTick} --\n" + s);
        }
    }

    public class RotationSpeedSystem : SystemBase {

        protected override void OnUpdate() {
            Entities
                .ForEach((ref Rotation rotation, in RotationSpeedData rotSpeed) => {
                    rotation.Value = math.mul(math.normalize(rotation.Value), quaternion.AxisAngle(math.up(), rotSpeed.radiansPerTick));
                }).ScheduleParallel();
        }
    }

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