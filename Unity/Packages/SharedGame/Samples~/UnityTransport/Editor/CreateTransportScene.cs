using System.IO;
using SharedGame.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace SharedGame.Samples.UnityTransport {
    public static class CreateTransportScene {
        [MenuItem("GekkoNet/Create Unity Transport Rollback Duel Scene")]
        public static void Create() {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateDuelScene.Create();
            var selected = Selection.activeGameObject;
            var old = selected.GetComponent<RollbackDuel>();
            var document = old.gameDocument;
            Undo.DestroyObjectImmediate(old);
            var game = Undo.AddComponent<UnityTransportDuel>(selected); game.gameDocument = document;
            selected.name = "Rollback Duel (Unity Transport)";
            var scene = selected.scene;
            foreach (var root in scene.GetRootGameObjects()) foreach (var ui in root.GetComponentsInChildren<SharedGameUI>(true)) {
                Undo.RecordObject(ui, "Assign Unity Transport manager"); ui.manager = game;
                EditorUtility.SetDirty(ui);
            }
            string script = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("UnityTransportDuel t:MonoScript")[0]);
            string path = Path.GetDirectoryName(script).Replace('\\', '/') + "/UnityTransportDuel.unity";
            EditorSceneManager.SaveScene(scene, AssetDatabase.GenerateUniqueAssetPath(path));
            AssetDatabase.SaveAssets();
        }
    }
}
