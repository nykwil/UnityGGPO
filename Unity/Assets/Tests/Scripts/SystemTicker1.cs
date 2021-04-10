using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
//private World world;
//private ReferencedUnityObjects unityObjects;
//private NativeArray<byte> na;

//public unsafe void Serialize() {
//    using (var mbw = new MemoryBinaryWriter()) {
//        SerializeUtilityHybrid.Serialize(world.EntityManager, mbw, out unityObjects);
//        na = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(mbw.Data, mbw.Length, Allocator.Temp);
//    }
//}

//public unsafe void Deserialize() {
//    using (var mbr = new MemoryBinaryReader((byte*)na.GetUnsafePtr())) {
//        SerializeUtilityHybrid.Deserialize(world.EntityManager, mbr, unityObjects);
//    }
//}

//// Start is called before the first frame update
//private void Start() {
//    //        SerializeUtility.SerializeWorld()
//    //        Unity.Entities.Serialization.MemoryBinaryReader()
//}

//// Update is called once per frame
//private void Update() {
//}

public class SystemTicker1 : MonoBehaviour {
    private IEnumerable<ComponentSystemBase> simSystems;
    public World world;
    public bool isSimulating = false;

    private void Start() {
        world = World.DefaultGameObjectInjectionWorld;
        var simGroup = world.GetExistingSystem<SimulationSystemGroup>();
        simSystems = simGroup.Systems;
        simGroup.Enabled = false;
    }

    private void FixedUpdate() {
        if (isSimulating) {
            foreach (var sys in simSystems) {
                sys.Update();
            }
        }
    }
}
