using SharedGame;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace EcsWar {

    public struct EcsGameState : IGameState {
        public int Framenumber { get; set; }

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

        public void Init(int num_players) {
            Framenumber = 0;
            lastWorldId = 0;
            savedStates = new Dictionary<byte, World>();
            activeWorld = World.DefaultGameObjectInjectionWorld;
            var simGroup = activeWorld.GetExistingSystem<SimulationSystemGroup>();
            simGroup.Enabled = false;
            simSystems = simGroup.Systems;
        }

        public void LogInfo(string filename) {
            //@TODO
        }

        public ulong ReadInputs(int controllerId) {
            //@TODO
            return 0;
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

        public void Update(ulong[] inputs, int disconnect_flags) {
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