using Unity.Entities;
using UnityEngine;

//public abstract class BakeDataAuthoring<T> : MonoBehaviour where T : unmanaged, IComponentData {
//    public T data;

//    public class ComponentDataBaker : Baker<BakeDataAuthoring<T>> {

//        public override void Bake(BakeDataAuthoring<T> authoring) {
//            var entity = GetEntity(TransformUsageFlags.Dynamic);

//            AddComponent(entity, authoring.data);
//        }
//    }
//}

//public abstract class BakeSharedDataAuthoring<K, T> : MonoBehaviour where K : BakeSharedDataAuthoring<K, T> where T : unmanaged, ISharedComponentData {
//    public T data;

//    public class ComponentDataBaker : Baker<K> {
//        public override void Bake(K authoring) {
//            var entity = GetEntity(TransformUsageFlags.Dynamic);

//            AddSharedComponent(entity, authoring.data);
//        }
//    }
//}

//public abstract class BakeSharedDataManagedAuthoring<K, T> : MonoBehaviour where K : BakeSharedDataManagedAuthoring<K, T> where T : struct, ISharedComponentData {
//    public T data;

//    public class ComponentDataBaker : Baker<K> {
//        public override void Bake(K authoring) {
//            var entity = GetEntity(TransformUsageFlags.Dynamic);

//            AddSharedComponentManaged(entity, authoring.data);
//        }
//    }
//}