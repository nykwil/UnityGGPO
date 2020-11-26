//-----------------------------------------------------------------------
// <copyright file="DynamicBufferCollectionResolver.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Sirenix.OdinInspector.Modules.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Unity.Entities;
    using Sirenix.OdinInspector.Editor;

    public class DynamicBufferCollectionResolver<T> : BaseOrderedCollectionResolver<DynamicBuffer<T>> where T : struct
    {
        private static readonly EqualityComparer<T> ElementComparer = EqualityComparer<T>.Default;
        private Dictionary<int, InspectorPropertyInfo> childInfos = new Dictionary<int, InspectorPropertyInfo>();

        public override Type ElementType { get { return typeof(T); } }

        public override InspectorPropertyInfo GetChildInfo(int childIndex)
        {
            if (childIndex < 0 || childIndex >= this.ChildCount)
            {
                throw new IndexOutOfRangeException();
            }

            InspectorPropertyInfo result;

            if (!this.childInfos.TryGetValue(childIndex, out result))
            {
                result = InspectorPropertyInfo.CreateValue(
                    name: CollectionResolverUtilities.DefaultIndexToChildName(childIndex),
                    order: childIndex,
                    serializationBackend: this.Property.BaseValueEntry.SerializationBackend,
                    getterSetter: new GetterSetter<DynamicBuffer<T>, T>(
                        getter: (ref DynamicBuffer<T> list) => list[childIndex],
                        setter: (ref DynamicBuffer<T> list, T element) => list[childIndex] = element),
                    attributes: this.Property.Attributes.Where(attr => !attr.GetType().IsDefined(typeof(DontApplyToListElementsAttribute), true)).Append(new EnableGUIAttribute()).ToArray());

                this.childInfos[childIndex] = result;
            }

            return result;
        }

        public override bool ChildPropertyRequiresRefresh(int index, InspectorPropertyInfo info)
        {
            return false;
        }

        public override int ChildNameToIndex(string name)
        {
            return CollectionResolverUtilities.DefaultChildNameToIndex(name);
        }

        protected override int GetChildCount(DynamicBuffer<T> value)
        {
            if (!value.IsCreated) return 0;

            return value.Length;
        }

        protected override void Add(DynamicBuffer<T> collection, object value)
        {
            if (!collection.IsCreated) return;
            collection.Add((T)value);
        }

        protected override void InsertAt(DynamicBuffer<T> collection, int index, object value)
        {
            if (!collection.IsCreated) return;
            collection.Insert(index, (T)value);
        }

        protected override void Remove(DynamicBuffer<T> collection, object value)
        {
            if (!collection.IsCreated) return;
            T val = (T)value;

            for (int i = 0; i < collection.Length; i++)
            {
                if (ElementComparer.Equals(collection[i], val))
                {
                    collection.RemoveAt(i);
                    return;
                }
            }
        }

        protected override void RemoveAt(DynamicBuffer<T> collection, int index)
        {
            if (!collection.IsCreated) return;
            collection.RemoveAt(index);
        }

        protected override void Clear(DynamicBuffer<T> collection)
        {
            if (!collection.IsCreated) return;
            collection.Clear();
        }

        protected override bool CollectionIsReadOnly(DynamicBuffer<T> collection)
        {
            // Let's... not edit this, perhaps?
            return false;
        }
    }
}
