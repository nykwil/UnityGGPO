using SharedGame;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace EcsWar {

    public class EcsGame : IGame {
        public const int INPUT_THRUST = (1 << 0);
        public const int INPUT_BREAK = (1 << 1);
        public const int INPUT_ROTATE_LEFT = (1 << 2);
        public const int INPUT_ROTATE_RIGHT = (1 << 3);
        public const int INPUT_FIRE = (1 << 4);
        public const int INPUT_BOMB = (1 << 5);

        public bool verbose = false;

        public int Framenumber { get; private set; }

        public int Checksum { get; private set; }

        private Dictionary<byte, World> savedStates;
        private byte lastWorldId;
        private World activeWorld;
        private IEnumerable<ComponentSystemBase> simSystems;

        public EcsGame(EcsSceneInfo sceneInfo) {
            Checksum = 0;
            Framenumber = 0;
            lastWorldId = 0;
            savedStates = new Dictionary<byte, World>();

            activeWorld = World.DefaultGameObjectInjectionWorld;
            var simGroup = activeWorld.GetExistingSystem<SimulationSystemGroup>();
            //simGroup.Enabled = false;
            //simSystems = simGroup.Systems;
            //@TODO
            sceneInfo.CreateScene(activeWorld);
        }

        public void LogInfo(string filename) {
            //@TODO
        }

        public long ReadInputs(int id) {
            long input = 0;

            if (id == 0) {
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.UpArrow)) {
                    input |= INPUT_THRUST;
                }
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.DownArrow)) {
                    input |= INPUT_BREAK;
                }
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftArrow)) {
                    input |= INPUT_ROTATE_LEFT;
                }
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightArrow)) {
                    input |= INPUT_ROTATE_RIGHT;
                }
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightControl)) {
                    input |= INPUT_FIRE;
                }
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightShift)) {
                    input |= INPUT_BOMB;
                }
            }
            else if (id == 1) {
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.W)) {
                    input |= INPUT_THRUST;
                }
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.S)) {
                    input |= INPUT_BREAK;
                }
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.A)) {
                    input |= INPUT_ROTATE_LEFT;
                }
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.D)) {
                    input |= INPUT_ROTATE_RIGHT;
                }
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.F)) {
                    input |= INPUT_FIRE;
                }
                if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.G)) {
                    input |= INPUT_BOMB;
                }
            }

            return input;
        }

        public static void CopyWorld(ref World fromWorld, ref World toWorld) {
            toWorld.EntityManager.CopyAndReplaceEntitiesFrom(fromWorld.EntityManager);
            toWorld.SetTime(new Unity.Core.TimeData(fromWorld.Time.ElapsedTime, fromWorld.Time.DeltaTime));
        }

        public void FromBytes(NativeArray<byte> data) {
            if (verbose) {
                Debug.Log("Load State " + data[0]);
            }
            if (savedStates.TryGetValue(data[0], out var savedWorld)) {
                CopyWorld(ref savedWorld, ref activeWorld);
            }
        }

        public NativeArray<byte> ToBytes() {
            lastWorldId = (byte)((lastWorldId + 1) % byte.MaxValue);
            if (verbose) {
                Debug.Log("Save State to " + lastWorldId);
            }

            var na = new NativeArray<byte>(1, Allocator.Persistent);
            na[0] = lastWorldId;
            var newWorld = new World(lastWorldId.ToString(), WorldFlags.Simulation);
            CopyWorld(ref activeWorld, ref newWorld);
            savedStates[lastWorldId] = newWorld;
            return na;
        }

        private void InjectInputs(long[] inputsList) {
            var em = activeWorld.EntityManager;
            var group = em.CreateEntityQuery(
                typeof(ActiveInput),
                typeof(PlayerData)
            );

            var entities = group.ToEntityArray(Allocator.TempJob);
            var playerDatas = group.ToComponentDataArray<PlayerData>(Allocator.TempJob);
            for (int i = 0; i < entities.Length; ++i) {
                var inputs = inputsList[playerDatas[i].PlayerIndex];

                em.SetComponentData(entities[i], new ActiveInput {
                    Accelerate = (inputs & INPUT_THRUST) != 0,
                    Left = (inputs & INPUT_ROTATE_LEFT) != 0,
                    Reverse = (inputs & INPUT_BREAK) != 0,
                    Right = (inputs & INPUT_ROTATE_RIGHT) != 0,
                    Shoot = (inputs & INPUT_FIRE) != 0
                });
            }
            entities.Dispose();
            playerDatas.Dispose();
        }

        public void Update(long[] inputs, int disconnect_flags) {
            InjectInputs(inputs);
            foreach (var sys in simSystems) {
                sys.Update();
            }
            Framenumber += 1;
            Checksum = Framenumber; // @todo
        }

        public void FreeBytes(NativeArray<byte> arr) {
            if (savedStates.TryGetValue(arr[0], out var world)) {
                if (verbose) {
                    Debug.Log("Free State at " + arr[0]);
                }

                if (world.IsCreated) {
                    world.Dispose();
                }
                savedStates.Remove(arr[0]);
            }
            if (arr.IsCreated) {
                arr.Dispose();
            }
        }
    }
}