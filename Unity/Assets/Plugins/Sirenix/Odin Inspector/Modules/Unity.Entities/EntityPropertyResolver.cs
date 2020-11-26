//-----------------------------------------------------------------------
// <copyright file="EntityPropertyResolver.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Sirenix.OdinInspector.Modules.Entities
{
    using System;
    using System.Collections.Generic;
    using Unity.Entities;
    using Sirenix.OdinInspector.Editor;
    using UnityEngine;
    using System.Reflection;
    using Sirenix.Utilities;

    public class EntityPropertyResolver : OdinPropertyResolver<EntityInspectionData>, IRefreshableResolver
    {
        private static readonly MethodInfo CreateComponentDataGetterSetter_Method = typeof(EntityPropertyResolver).GetMethod("CreateComponentDataGetterSetter", Flags.StaticAnyVisibility);
        private static readonly MethodInfo CreateSharedComponentDataGetterSetter_Method = typeof(EntityPropertyResolver).GetMethod("CreateSharedComponentDataGetterSetter", Flags.StaticAnyVisibility);
        private static readonly MethodInfo CreateUnityObjectComponentGetterSetter_Method = typeof(EntityPropertyResolver).GetMethod("CreateUnityObjectComponentGetterSetter", Flags.StaticAnyVisibility);
        private static readonly MethodInfo CreateBufferElementDataGetterSetter_Method = typeof(EntityPropertyResolver).GetMethod("CreateBufferElementDataGetterSetter", Flags.StaticAnyVisibility);

        private Dictionary<int, InspectorPropertyInfo> infos = new Dictionary<int, InspectorPropertyInfo>();

        private static readonly Dictionary<Type, Type> ExpectedTypeMapping = new Dictionary<Type, Type>(FastTypeComparer.Instance);

        public override int ChildNameToIndex(string name)
        {
            return CollectionResolverUtilities.DefaultChildNameToIndex(name);
        }

        public override InspectorPropertyInfo GetChildInfo(int childIndex)
        {
            InspectorPropertyInfo result;

            if (!this.infos.TryGetValue(childIndex, out result))
            {
                var data = this.ValueEntry.SmartValue;

                ComponentType componentType;
                Type type;

                using (var types = data.EntityManager.GetComponentTypes(data.Entity))
                {
                    componentType = types[childIndex];
                    type = componentType.GetManagedType();
                }

                IValueGetterSetter getterSetter;

                bool isComponentData = type.IsValueType && typeof(IComponentData).IsAssignableFrom(type);
                bool isSharedComponentData = type.IsValueType && typeof(ISharedComponentData).IsAssignableFrom(type);
                bool isUnityObjectComponent = !isComponentData && !isSharedComponentData && typeof(Component).IsAssignableFrom(type);
                bool isBufferElementData = !isComponentData && !isSharedComponentData && !isUnityObjectComponent && type.IsValueType && typeof(IBufferElementData).IsAssignableFrom(type);

                // This rather horrid and inefficient reflection approach is merely temporary - API's to solve this problem in a faster and easier way are coming to Odin soon
                if (isComponentData)
                {
                    getterSetter = (IValueGetterSetter)CreateComponentDataGetterSetter_Method.MakeGenericMethod(type).Invoke(null, new object[] { componentType });
                }
                else if (isSharedComponentData)
                {
                    getterSetter = (IValueGetterSetter)CreateSharedComponentDataGetterSetter_Method.MakeGenericMethod(type).Invoke(null, new object[] { componentType });
                }
                else if (isUnityObjectComponent)
                {
                    getterSetter = (IValueGetterSetter)CreateUnityObjectComponentGetterSetter_Method.MakeGenericMethod(type).Invoke(null, new object[] { componentType });
                }
                else if (isBufferElementData)
                {
                    getterSetter = (IValueGetterSetter)CreateBufferElementDataGetterSetter_Method.MakeGenericMethod(type).Invoke(null, new object[] { componentType });
                }
                else
                {
                    throw new NotImplementedException("Missing support for putting " + type.GetNiceFullName() + " on entities.");
                }
                
                result = InspectorPropertyInfo.CreateValue(
                    CollectionResolverUtilities.DefaultIndexToChildName(childIndex),
                    0, SerializationBackend.None,
                    getterSetter);

                if (isUnityObjectComponent)
                {
                    var attrs = result.GetEditableAttributesList();
                    attrs.Add(new InlineEditorAttribute(InlineEditorObjectFieldModes.CompletelyHidden));
                    attrs.Add(new EntityManagedComponentAttribute());
                }

                this.infos.Add(childIndex, result);
            }

            return result;
        }

        protected override int GetChildCount(EntityInspectionData value)
        {
            return value.EntityManager.GetComponentCount(value.Entity);
        }

        private static IValueGetterSetter CreateComponentDataGetterSetter<T>(ComponentType componentType) where T : struct, IComponentData
        {
            int typeIndex = TypeManager.GetTypeIndex<T>();
            bool zeroSized = ComponentType.FromTypeIndex(typeIndex).IsZeroSized;

            return new GetterSetter<EntityInspectionData, T>(
                getter: (ref EntityInspectionData data) =>
                {
                    if (zeroSized) return default(T);

                    if (data.EntityManager.HasComponent<T>(data.Entity))
                        return data.EntityManager.GetComponentData<T>(data.Entity);

                    return default(T);
                },
                setter: (ref EntityInspectionData data, T value) => data.EntityManager.SetComponentData<T>(data.Entity, value)
            );
        }

        private static IValueGetterSetter CreateSharedComponentDataGetterSetter<T>(ComponentType componentType) where T : struct, ISharedComponentData
        {
            return new GetterSetter<EntityInspectionData, T>(
                getter: (ref EntityInspectionData data) =>
                {
                    if (data.EntityManager.HasComponent(data.Entity, componentType))
                        return data.EntityManager.GetSharedComponentData<T>(data.Entity);

                    return default(T);
                },
                setter: (ref EntityInspectionData data, T value) =>
                {
                // There's currently some oddity with using SetSharedComponentData - it doesn't seem to update
                // the values properly - and for the property bag values, the shared component data is completely funky.
                //
                // Our tentative "fix" for this is rather brutal, as you can see. This doesn't seem quite right.
                // Please do clarify how this is meant to work.

                using (var entities = data.EntityManager.GetAllEntities())
                    {
                        for (int i = 0; i < entities.Length; i++)
                        {
                            if (data.EntityManager.HasComponent(entities[i], componentType))
                                data.EntityManager.SetSharedComponentData(entities[i], value);
                        }
                    }
                }
            );
        }

        private static IValueGetterSetter CreateUnityObjectComponentGetterSetter<T>(ComponentType componentType) where T : Component
        {
            return new GetterSetter<EntityInspectionData, T>(
                getter: (ref EntityInspectionData data) =>
                {
                    if (data.EntityManager.HasComponent(data.Entity, componentType))
                        return data.EntityManager.GetComponentObject<T>(data.Entity);
                    return null;
                },
                setter: (ref EntityInspectionData data, T component) => { } // Cannot set this at all, as far as I can tell. That makes sense too.
            );
        }

        private static IValueGetterSetter CreateBufferElementDataGetterSetter<T>(ComponentType componentType) where T : struct, IBufferElementData
        {
            ExpectedTypeMapping[typeof(T)] = typeof(BufferElementDataWrapper<T>);

            return new GetterSetter<EntityInspectionData, BufferElementDataWrapper<T>>(
                getter: (ref EntityInspectionData data) =>
                {
                    return new BufferElementDataWrapper<T>()
                    {
                        Buffer = data.EntityManager.GetBuffer<T>(data.Entity)
                    };
                    //var buffer = data.EntityManager.GetBuffer<T>(data.Entity);

                },
                setter: (ref EntityInspectionData data, BufferElementDataWrapper<T> value) => {  } // Makes no sense to set this
            );
        }

        public bool ChildPropertyRequiresRefresh(int index, InspectorPropertyInfo info)
        {
            var data = this.ValueEntry.SmartValue;
            using (var types = data.EntityManager.GetComponentTypes(data.Entity))
            {
                var expectedType = types[index].GetManagedType();

                Type mapTo;

                if (ExpectedTypeMapping.TryGetValue(expectedType, out mapTo))
                {
                    expectedType = mapTo;
                }

                if (expectedType != info.TypeOfValue)
                {
                    this.infos.Remove(index);
                    return true;
                }
                return false;
            }
        }
    }
}
