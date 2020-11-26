//-----------------------------------------------------------------------
// <copyright file="OdinECSEditorGUI.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Sirenix.OdinInspector.Modules.Entities
{
    using UnityEditor;
    using UnityEngine;
    using Sirenix.Utilities.Editor;
    using Sirenix.Utilities;

    public static class OdinECSEditorGUI
    {
        private static Color darkerLinerColor = EditorGUIUtility.isProSkin ? new Color(0.11f, 0.11f, 0.11f, 0.294f) : new Color(0, 0, 0, 0.2f);
        private static Color lighterLineColor = EditorGUIUtility.isProSkin ? new Color(1.000f, 1.000f, 1.000f, 0.103f) : new Color(1, 1, 1, 1);
        private static Texture2D entityIcon;

        public static Texture2D EntityIcon
        {
            get
            {
                if (entityIcon == null)
                {
                    entityIcon = GUIHelper.GetAssetThumbnail(null, typeof(UnityEngine.Object), false);
                }

                return entityIcon;
            }
        }

        public static void DrawHorizontalSeparator(Rect rect)
        {
            EditorGUI.DrawRect(rect, darkerLinerColor);
            EditorGUI.DrawRect(rect.AlignTop(1), darkerLinerColor);
            EditorGUI.DrawRect(rect.AlignBottom(1), lighterLineColor);
        }

        public static void DrawVerticalInspectorSeparator()
        {
            var rect = GUILayoutUtility.GetRect(0, 5 + 4);
            rect.x -= 30;
            rect.width += 60;
            rect.y += 2;
            rect.height -= 4;
            OdinECSEditorGUI.DrawHorizontalSeparator(rect);
        }

        public static Rect HeaderLabel(string text, Texture2D icon, bool drawHeaderSeperator)
        {
            var rect = EditorGUILayout.GetControlRect(false);
            GUI.Label(rect.AddXMin(10), text, SirenixGUIStyles.Label);

            GUI.DrawTexture(rect.AddX(-6).AlignLeft(16).AlignCenterY(16), icon);

            if (drawHeaderSeperator)
            {
                GUILayout.Space(4);
            }

            return rect;
        }
    }
}
