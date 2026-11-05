using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
public static class TransportPackageInstaller {
    static AddAndRemoveRequest request;
    static double deadline;
    public static void Install() {
        request = Client.AddAndRemove(new[] { "com.unity.transport@2.7.4", "com.unity.test-framework@1.8.0",
            "com.unity.modules.uielements@1.0.0", "com.unity.modules.jsonserialize@1.0.0", "com.unity.modules.screencapture@1.0.0" });
        deadline = EditorApplication.timeSinceStartup + 600;
        EditorApplication.update += Poll;
    }
    static void Poll() {
        if (!request.IsCompleted && EditorApplication.timeSinceStartup < deadline) return;
        EditorApplication.update -= Poll;
        if (!request.IsCompleted || request.Status != StatusCode.Success) {
            Debug.LogError(request.Error?.message ?? "Package resolution timed out."); EditorApplication.Exit(1);
        } else EditorApplication.Exit(0);
    }
}
