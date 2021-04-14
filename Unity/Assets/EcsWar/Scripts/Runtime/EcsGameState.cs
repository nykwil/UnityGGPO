using SharedGame;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace EcsWar {

    public struct EcsGameState : IGameState {
        public const int INPUT_THRUST = (1 << 0);
        public const int INPUT_BREAK = (1 << 1);
        public const int INPUT_ROTATE_LEFT = (1 << 2);
        public const int INPUT_ROTATE_RIGHT = (1 << 3);
        public const int INPUT_FIRE = (1 << 4);
        public const int INPUT_BOMB = (1 << 5);

        public int Framenumber { get; private set; }

        public int Checksum { get; private set; }

        private Dictionary<byte, World> savedStates;
        private byte lastWorldId;
        private World activeWorld;
        private IEnumerable<ComponentSystemBase> simSystems;

        public void FromBytes(NativeArray<byte> data) {
            if (savedStates.TryGetValue(data[0], out var savedWorld)) {
                CopyWorld(ref savedWorld, ref activeWorld);
            }
        }

        public static void CopyWorld(ref World fromWorld, ref World toWorld) {
            toWorld.EntityManager.DestroyAndResetAllEntities();
            toWorld.EntityManager.CopyAndReplaceEntitiesFrom(fromWorld.EntityManager);
            toWorld.SetTime(new Unity.Core.TimeData(fromWorld.Time.ElapsedTime, fromWorld.Time.DeltaTime));
        }

        public EcsGameState(World activeWorld) {
            Checksum = 0;
            Framenumber = 0;
            lastWorldId = 0;
            savedStates = new Dictionary<byte, World>();
            this.activeWorld = activeWorld;
            var simGroup = activeWorld.GetExistingSystem<SimulationSystemGroup>();
            simSystems = simGroup.Systems;
        }

        public void LogInfo(string filename) {
            //@TODO
        }

        public ulong ReadInputs(int id) {
            ulong input = 0;

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

        public NativeArray<byte> ToBytes() {
            lastWorldId++;

            var na = new NativeArray<byte>(1, Allocator.Persistent);
            na[0] = lastWorldId;
            var newWorld = new World(lastWorldId.ToString(), WorldFlags.Simulation);
            savedStates[lastWorldId] = newWorld;
            CopyWorld(ref activeWorld, ref newWorld);
            return na;
        }

        private void InjectInputs(ulong[] inputsList) {
            var em = activeWorld.EntityManager;
            var group = em.CreateEntityQuery(
                typeof(ActiveInput)
            );

            var entities = group.ToEntityArray(Allocator.TempJob);
            for (int i = 0; i < entities.Length; ++i) {
                var playerData = em.GetComponentData<Player>(entities[i]);
                var inputs = inputsList[playerData.PlayerIndex];

                em.SetComponentData(entities[i], new ActiveInput {
                    Accelerate = (inputs & INPUT_THRUST) != 0,
                    Left = (inputs & INPUT_ROTATE_LEFT) != 0,
                    Reverse = (inputs & INPUT_BREAK) != 0,
                    Right = (inputs & INPUT_ROTATE_RIGHT) != 0,
                    Shoot = (inputs & INPUT_FIRE) != 0
                });
            }
            entities.Dispose();
        }

        public void Update(ulong[] inputs, int disconnect_flags) {
            InjectInputs(inputs);
            foreach (var sys in simSystems) {
                sys.Update();
            }
        }

        public void FreeBytes(NativeArray<byte> data) {
            if (savedStates.TryGetValue(data[0], out var world)) {
                world.Dispose();
                savedStates.Remove(data[0]);
            }
            if (data.IsCreated) {
                data.Dispose();
            }
        }
    }
}