//-----------------------------------------------------------------------
// <copyright file="BufferElementDataWrapperDrawer.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Sirenix.OdinInspector.Modules.Entities
{
    using Unity.Entities;
    using Sirenix.OdinInspector.Editor;
    using UnityEngine;
    using Sirenix.Utilities.Editor;

    public class BufferElementDataWrapperDrawer<TBufferElementData> : OdinValueDrawer<BufferElementDataWrapper<TBufferElementData>>
        where TBufferElementData : struct, IBufferElementData
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var rect = OdinECSEditorGUI.HeaderLabel(typeof(TBufferElementData).FullName, OdinECSEditorGUI.EntityIcon, this.Property.Children.Count > 0);
            GUI.Label(rect, "Buffer Element Data", SirenixGUIStyles.RightAlignedGreyMiniLabel);
            this.CallNextDrawer(null);
            OdinECSEditorGUI.DrawVerticalInspectorSeparator();
        }
    }
}
