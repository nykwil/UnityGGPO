//-----------------------------------------------------------------------
// <copyright file="EntitySharedComponentDataDrawer.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Sirenix.OdinInspector.Modules.Entities
{
    using Unity.Entities;
    using Sirenix.OdinInspector.Editor;
    using UnityEngine;
    using Sirenix.Utilities.Editor;

    public class EntitySharedComponentDataDrawer<T> : OdinValueDrawer<T> where T : struct, ISharedComponentData
    {
        //private static readonly string MessageBoxMessage = "Shared Components are acting weird - click to find out more.";

        //private static readonly string MessageBoxDetailedMessage =
        //        "There's currently some oddities with using SetSharedComponentData - it doesn't seem to update " +
        //        "the values properly - and for the PropertyBag-backed values, the shared component data is completely funky " +
        //        "when editing it through the inspector." +
        //        "\n\n" +
        //        "We haven't tried to truly dig through the innards of the ECS system to fix this properly, as this is merely a demo. " +
        //        "For now, we just iterate through all entities and set shared component data everywhere whenever it is changed through " +
        //        "the inspector. This seems to sort of work fine, though it is pretty darn brutal.";

        //private bool messageBoxIsFolded = true;

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var rect = OdinECSEditorGUI.HeaderLabel(typeof(T).FullName, OdinECSEditorGUI.EntityIcon, this.Property.Children.Count > 0);
            GUI.Label(rect, "Shared Data", SirenixGUIStyles.RightAlignedGreyMiniLabel);

            //this.messageBoxIsFolded = SirenixEditorGUI.DetailedMessageBox(
            //    MessageBoxMessage, MessageBoxDetailedMessage,
            //    MessageType.Warning, this.messageBoxIsFolded);

            for (int i = 0; i < this.Property.Children.Count; i++)
            {
                this.Property.Children[i].Draw();
            }
            OdinECSEditorGUI.DrawVerticalInspectorSeparator();
        }
    }
}
