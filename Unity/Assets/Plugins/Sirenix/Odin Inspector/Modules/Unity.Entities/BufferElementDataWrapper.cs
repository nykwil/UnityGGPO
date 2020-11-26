//-----------------------------------------------------------------------
// <copyright file="BufferElementDataWrapper.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Sirenix.OdinInspector.Modules.Entities
{
    using Unity.Entities;

    public struct BufferElementDataWrapper<T> where T : struct, IBufferElementData
    {
        [ShowInInspector, LabelText("Dynamic Buffer Contents"), ListDrawerSettings(HideAddButton = true, HideRemoveButton = true, DraggableItems = false, Expanded = true)]
        public DynamicBuffer<T> Buffer;
    }
}
