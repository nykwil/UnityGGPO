using System;
using System.IO;
using SharedGame.Samples.UnityTransport;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class TransportValidationBuild {
    public static void Mono() => Build(ScriptingImplementation.Mono2x, "Mono");
    public static void IL2CPP() => Build(ScriptingImplementation.IL2CPP, "IL2CPP");
    static void Build(ScriptingImplementation backend, string name) {
        CreateTransportScene.Create();
        var game = Selection.activeGameObject.GetComponent<UnityTransportDuel>();
        game.gameObject.AddComponent<TransportValidationPlayer>().manager = game;
        var scene = game.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../../.."));
        string output = Path.Combine(root, "build/players/UnityTransport-" + name + "/TransportDuel.exe");
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, backend);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { scene.path }, target = BuildTarget.StandaloneWindows64, locationPathName = output
        });
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Transport player build failed.");
        Debug.Log("TRANSPORT_BUILD_PASS " + name);
    }
}
