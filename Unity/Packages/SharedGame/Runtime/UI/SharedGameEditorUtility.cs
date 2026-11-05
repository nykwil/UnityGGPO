#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace SharedGame.UI {
    /// <summary>Editor-only asset paths and helpers shared by scene builders, tools and tests.</summary>
    public static class SharedGameEditorUtility {
        public static string PackageRoot => UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(SharedGameUI).Assembly).assetPath;
        public static string SessionUxmlPath => PackageRoot + "/Runtime/UI/Session.uxml";
        public static string DiagnosticsUxmlPath => PackageRoot + "/Runtime/Diagnostics/Diagnostics.uxml";
        public static string ThemePath => PackageRoot + "/Runtime/Resources/SharedGameTheme.tss";
        /// <summary>New in-memory PanelSettings using the SharedGame theme. Callers own and destroy or save it.</summary>
        public static PanelSettings CreatePanelSettings(bool scaleWithScreenSize = false) {
            var panel = ScriptableObject.CreateInstance<PanelSettings>();
            if (scaleWithScreenSize) {
                panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panel.referenceResolution = new Vector2Int(960, 540);
            }
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            return panel;
        }
        /// <summary>Sends a navigation submit to the named button, as keyboard or gamepad activation would.</summary>
        public static void Submit(VisualElement root, string buttonName) {
            var button = root.Q<Button>(buttonName);
            using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = button; button.SendEvent(evt); }
        }
    }
}
#endif
