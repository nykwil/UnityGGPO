//-----------------------------------------------------------------------
// <copyright file="EntityManagedComponentAttributeDrawer.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Sirenix.OdinInspector.Modules.Entities
{
    using UnityEditor;
    using Sirenix.OdinInspector.Editor;
    using UnityEngine;
    using Sirenix.Utilities.Editor;

    [DrawerPriority(0.51, 0, 0)]
    public class EntityManagedComponentAttributeDrawer<T> : OdinAttributeDrawer<EntityManagedComponentAttribute, T> where T : Component
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var drawComponent = this.ValueEntry.ValueState != PropertyValueState.NullReference;
            var rect = OdinECSEditorGUI.HeaderLabel(typeof(T).FullName, GUIHelper.GetAssetThumbnail(this.ValueEntry.SmartValue, this.ValueEntry.BaseValueType, false), drawComponent);

            if (drawComponent)
            {
                GUIHelper.PushGUIEnabled(false);
                rect.xMin += EditorGUIUtility.labelWidth;
                SirenixEditorFields.UnityObjectField(rect, this.ValueEntry.SmartValue, this.ValueEntry.BaseValueType, false);
                GUIHelper.PopGUIEnabled();

                this.CallNextDrawer(null);
            }

            OdinECSEditorGUI.DrawVerticalInspectorSeparator();
        }
    }
}
