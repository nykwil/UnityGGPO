using System.IO;
using SharedGame.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
namespace SharedGame.Samples {
    public static class CreateDuelScene {
        [MenuItem("GekkoNet/Create SharedGame Rollback Duel Scene")]
        public static void Create() {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string scriptPath = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("RollbackDuel t:MonoScript")[0]);
            string folder = Path.GetDirectoryName(scriptPath).Replace('\\', '/');
            var panel = SharedGameEditorUtility.CreatePanelSettings(scaleWithScreenSize: true);
            AssetDatabase.CreateAsset(panel, AssetDatabase.GenerateUniqueAssetPath(folder + "/DuelPanel.asset"));
            var gameObject = new GameObject("Rollback Duel");
            var document = gameObject.AddComponent<PanelRenderer>(); document.panelSettings = panel; document.sortingOrder = 0;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(folder + "/Duel.uxml");
            var game = gameObject.AddComponent<RollbackDuel>(); game.gameDocument = document;
            var uiObject = new GameObject("Session UI");
            var sessionDocument = uiObject.AddComponent<PanelRenderer>(); sessionDocument.panelSettings = panel; sessionDocument.sortingOrder = 10;
            sessionDocument.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SharedGameEditorUtility.SessionUxmlPath);
            var ui = uiObject.AddComponent<SharedGameUI>(); ui.manager = game; ui.document = sessionDocument;
            EditorSceneManager.SaveScene(scene, AssetDatabase.GenerateUniqueAssetPath(folder + "/RollbackDuel.unity"));
            AssetDatabase.SaveAssets(); Selection.activeGameObject = gameObject;
        }
    }
}
