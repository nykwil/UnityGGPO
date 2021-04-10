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

        //var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default);
        //DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(simulationWorld, systems);

        //FixedStepSimulationSystemGroup fixGroup = simulationWorld.GetExistingSystem<FixedStepSimulationSystemGroup>();
        //fixGroup.FixedRateManager = new FixedRateUtils.FixedRateSimpleManager(Time.fixedDeltaTime);
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

//public static class FixedUpdateRunner {
//    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
//    private static void MoveSimulationGroup() {
//        // This must be called AFTER DefaultWorldInitialization, otherwise DefaultWorldInitialization overwrites PlayerLoop
//        var playerLoop = ScriptBehaviourUpdateOrder.CurrentPlayerLoop;
//        var func = RemoveCallback<SimulationSystemGroup>(playerLoop);
//        if (func != null) {
//            InstallCallback<SimulationSystemGroup>(playerLoop, typeof(FixedUpdate), func);
//            ScriptBehaviourUpdateOrder.SetPlayerLoop(playerLoop);
//        }
//    }

//    private static void InstallCallback<T>(PlayerLoopSystem playerLoop, Type subsystem, PlayerLoopSystem.UpdateFunction callback) {
//        for (var i = 0; i < playerLoop.subSystemList.Length; ++i) {
//            int subsystemListLength = playerLoop.subSystemList[i].subSystemList.Length;
//            if (playerLoop.subSystemList[i].type == subsystem) {
//                // Create new subsystem list and add callback
//                var newSubsystemList = new PlayerLoopSystem[subsystemListLength + 1];
//                for (var j = 0; j < subsystemListLength; j++) {
//                    newSubsystemList[j] = playerLoop.subSystemList[i].subSystemList[j];
//                }
//                newSubsystemList[subsystemListLength].type = typeof(FixedUpdateRunner);
//                newSubsystemList[subsystemListLength].updateDelegate = callback;
//                playerLoop.subSystemList[i].subSystemList = newSubsystemList;
//            }
//        }
//    }

//    private static PlayerLoopSystem.UpdateFunction RemoveCallback<T>(PlayerLoopSystem playerLoop) {
//        for (var i = 0; i < playerLoop.subSystemList.Length; ++i) {
//            int subsystemListLength = playerLoop.subSystemList[i].subSystemList.Length;
//            for (var j = 0; j < subsystemListLength; j++) {
//                var item = playerLoop.subSystemList[i].subSystemList[j];
//                if (item.type == typeof(T)) {
//                    playerLoop.subSystemList[i].subSystemList = ExceptIndex(playerLoop.subSystemList[i].subSystemList, j);
//                    return item.updateDelegate;
//                }
//            }
//        }
//        return null;
//    }

//    private static T[] ExceptIndex<T>(T[] array, int exceptIndex) {
//        T[] result = new T[array.Length - 1];
//        if (exceptIndex > 0) {
//            Array.Copy(array, result, exceptIndex);
//        }
//        if (exceptIndex < array.Length - 1) {
//            Array.Copy(array, exceptIndex + 1, result, exceptIndex, array.Length - exceptIndex - 1);
//        }
//        return result;
//    }
//}

//public class RollbackTests : MonoBehaviour {
//    public bool pressedRollback;
//    public bool shouldSimulate;
//    public bool pressedSnapshot;

//    private static int simulatedFrame;
//    private static World simulationWorld;

//    private static List<float3> positions = new List<float3>();
//    private static List<float3> velocites = new List<float3>();

//    public bool isSimulating = false;
//    public World world;
//    public ComponentSystemGroup simGroup;

//    private void Awake() {
//        world = World.DefaultGameObjectInjectionWorld;
//        simGroup = world.GetExistingSystem<SimulationSystemGroup>();
//        simGroup.Enabled = false;
//    }

//    private void FixedUpdate() {
//        simGroup.Enabled = true;
//        simGroup.Update();
//        simGroup.Enabled = false;
//    }

//    private void Start() {
//        simulationWorld = new World("lockStepWorld", WorldFlags.Simulation);

//        var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default);
//        DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(simulationWorld, systems);

//        FixedStepSimulationSystemGroup fixGroup = simulationWorld.GetExistingSystem<FixedStepSimulationSystemGroup>();
//        fixGroup.FixedRateManager = new FixedRateUtils.FixedRateSimpleManager(Time.fixedDeltaTime);
//    }

//    //private void FixedUpdate() {
//    //    if (isSimulating) {
//    //        World.DefaultGameObjectInjectionWorld.Update();
//    //    }
//    //    else {
//    //        Debug.Log("Normal " + positions.Count + " " + GetBallPosition());

//    //        AddBallStateToLists();

//    //        if (positions.Count == 10) {
//    //            RollBackTo(5);
//    //        }
//    //    }
//    //}

//    private void RollBackTo(int tickToRollBackTo) {
//        Debug.Log("Rolling back to " + tickToRollBackTo);

//        SetBallPosAndVelToTick(tickToRollBackTo);

//        CreateSnapShot(ref simulationWorld, ref world);

//        SimulateTicks(positions.Count - tickToRollBackTo - 1);

//        world.EntityManager.DestroyAndResetAllEntities();
//        world.EntityManager.CopyAndReplaceEntitiesFrom(simulationWorld.EntityManager);
//    }

//    private void CreateSnapShot(ref World snapShotWorld, ref World world) {
//        snapShotWorld.EntityManager.DestroyAndResetAllEntities();
//        snapShotWorld.EntityManager.CopyAndReplaceEntitiesFrom(world.EntityManager);
//        snapShotWorld.SetTime(new Unity.Core.TimeData(world.Time.ElapsedTime, world.Time.DeltaTime));
//    }

//    private void SimulateTicks(int tickNumber) {
//        isSimulating = true;

//        Debug.Log("Simulating the next " + tickNumber + " ticks");
//        for (int i = 0; i < tickNumber; i++) {
//            simulationWorld.Update();
//        }
//        isSimulating = false;
//    }

//    public void SetBallPosAndVelToTick(int tick) {
//        float3 ballPosition = positions[tick];
//        float3 ballVelocity = velocites[tick];

//        Entities.ForEach((ref PhysicsVelocity physicsVelocity, ref Translation position, ref Rotation rotation) => {
//            position.Value = ballPosition;
//            physicsVelocity.Linear = ballVelocity;
//        });
//    }

//    private void AddBallStateToLists() {
//        positions.Add(GetBallPosition());
//        velocites.Add(GetBallVelocity());
//    }
//}