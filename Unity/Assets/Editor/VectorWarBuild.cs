using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>Batch-mode Windows player builds of the VectorWar scene, used by scripts/run-player-validation.ps1.</summary>
public static class VectorWarBuild {
    const string ScenePath = "Assets/VectorWar/Runtime/Scenes/VectorWar.unity";
    public static void BuildMono() => Build(ScriptingImplementation.Mono2x, "Mono");
    public static void BuildIL2CPP() => Build(ScriptingImplementation.IL2CPP, "IL2CPP");
    static void Build(ScriptingImplementation backend, string name) {
        string repository = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
        string configured = Environment.GetEnvironmentVariable("GEKKONET_BUILD_OUTPUT_DIR");
        string output = Path.Combine(string.IsNullOrEmpty(configured) ? Path.Combine(repository, "build/players/" + name) : configured, "VectorWar.exe");
        var previous = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
        try {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, backend);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception(name + " build failed.");
            Debug.Log("VECTORWAR_BUILD_PASS: " + name + " " + output);
        } finally { PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, previous); }
    }
}
