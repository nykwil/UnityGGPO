//-----------------------------------------------------------------------
// <copyright file="OdinEntitySelectionProxyEditor.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Sirenix.OdinInspector.Modules.Entities
{
    using System;
    using System.Linq;
    using UnityEditor;
    using Unity.Entities;
    using Unity.Entities.Editor;
    using Sirenix.OdinInspector.Editor;
    using UnityEngine;
    using Sirenix.Serialization;
    using System.Reflection;
    using Sirenix.Utilities.Editor;

    [CustomEditor(typeof(EntitySelectionProxy))]
    [InitializeOnLoad]
    public class OdinEntitySelectionProxyEditor : Editor
    {
        static OdinEntitySelectionProxyEditor()
        {
            if (SystemInclusionList_Type != null)
            {
                SystemInclusionList_OnGUI_Method = SystemInclusionList_Type.GetMethod("OnGUI", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(World), typeof(Entity) }, null);
            }

            EditorApplication.update += ForceOdinEditor;
            ForceOdinEditor();
        }

        private static void ForceOdinEditor()
        {
            // We have to be a little forceful with this
            CustomEditorUtility.SetCustomEditor(typeof(EntitySelectionProxy), typeof(OdinEntitySelectionProxyEditor), false, true);

            ScriptableObject proxy = null;
            Editor editor = null;

            try
            {
                proxy = CreateInstance(typeof(EntitySelectionProxy));

                editor = Editor.CreateEditor(proxy);

                if (editor != null && editor is OdinEntitySelectionProxyEditor)
                {
                    // Done! Unity got the message.
                    EditorApplication.update -= ForceOdinEditor;
                    //Debug.LogError("Sucess!");

                }
                else
                {
                    //Debug.LogError("Trying again!");
                }
            }
            finally
            {
                if (proxy != null)
                    DestroyImmediate(proxy);

                if (editor != null)
                    DestroyImmediate(editor);
            }
        }

        private static readonly Type SystemInclusionList_Type = TwoWaySerializationBinder.Default.BindToType("Unity.Entities.Editor.SystemInclusionList");
        private static readonly MethodInfo SystemInclusionList_OnGUI_Method;

        private object systemInclusionList;
        private PropertyTree tree;
        
        private void OnEnable()
        {
            if (SystemInclusionList_Type != null && SystemInclusionList_OnGUI_Method != null)
            {
                systemInclusionList = Activator.CreateInstance(SystemInclusionList_Type);
            }
        }

        public override void OnInspectorGUI()
        {
            // Ugh, reference mismatch between UnityEngine.CoreModule.dll and UnityEngine.dll in our module dev environment
            //   means we need this extra cast. Whatever! :D
            var proxy = (EntitySelectionProxy)(object)this.target;

            if (this.tree == null)
            {
                this.tree = PropertyTree.Create(this.serializedObject);
            }

            GUI.enabled = true;

            this.tree.BeginDraw(true);

            string name = proxy.EntityManager.GetName(proxy.Entity);

            if (string.IsNullOrEmpty(name))
            {
                name = "Entity " + proxy.Entity.Index;
            }

            GUILayout.Space(5);

            EditorGUILayout.LabelField(name, SirenixGUIStyles.BoldTitle);
            EditorGUILayout.LabelField("World: " + proxy.World.Name + ", Index: " + proxy.Entity.Index + ", Version: " + proxy.Entity.Version, SirenixGUIStyles.Subtitle);

            OdinECSEditorGUI.DrawVerticalInspectorSeparator();

            var entityProp = this.tree.RootProperty.Children["Entity"];

            for (int i = 0; i < entityProp.Children.Count; i++)
            {
                entityProp.Children[i].Draw();
            }

            this.tree.EndDraw();

            GUILayout.FlexibleSpace();

            if (this.systemInclusionList != null)
            {
                SystemInclusionList_OnGUI_Method.Invoke(this.systemInclusionList, new object[] { proxy.World, proxy.Entity });
            }
            else
            {
                SirenixEditorGUI.ErrorMessageBox("Could not find internal Unity type and method 'SystemInclusionList.OnGUI(World world, Entity entity)'. System rendering is switched off.");
            }

            if (EditorApplication.isPlaying)
            {
                this.Repaint();
            }
        }
    }
}
