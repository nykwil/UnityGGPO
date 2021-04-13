using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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

    private static World simulationWorld;

    public World world;

    private void Awake() {
        world = World.DefaultGameObjectInjectionWorld;
        simulationWorld = new World("lockStepWorld", WorldFlags.Simulation);
    }

    private void Update() {
        if (pressedRollback) {
            Debug.Log("pressedRollback");
            pressedRollback = false;
            CreateSnapShot(ref world, ref simulationWorld);
        }
        if (pressedSnapshot) {
            Debug.Log("pressedSnapshot");
            pressedSnapshot = false;
            CreateSnapShot(ref simulationWorld, ref world);
        }
    }

    public static void CreateSnapShot(ref World snapShotWorld, ref World world) {
        snapShotWorld.EntityManager.DestroyAndResetAllEntities();
        snapShotWorld.EntityManager.CopyAndReplaceEntitiesFrom(world.EntityManager);
        snapShotWorld.SetTime(new Unity.Core.TimeData(world.Time.ElapsedTime, world.Time.DeltaTime));
    }

    private void SimulateTicks(int tickNumber) {
        Debug.Log("Simulating the next " + tickNumber + " ticks");
        for (int i = 0; i < tickNumber; i++) {
            simulationWorld.Update();
        }
    }
}