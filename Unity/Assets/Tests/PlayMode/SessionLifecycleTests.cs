using System.Collections;
using NUnit.Framework;
using SharedGame;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VectorWar;
public sealed class SessionTestManager : GameManager {
    sealed class Neutral : ILocalInputSource { public long ReadInput(int controllerId, int frame) => 0; }
    protected override IGame CreateGame(int count) => new VwGame(count);
    protected override ILocalInputSource CreateInputSource() => new Neutral();
}
public sealed class SessionLifecycleTests {
    [UnityTest] public IEnumerator RestartsAndDisableReleaseNativeOwnership() {
        var go = new GameObject("Session lifecycle test");
        try {
            var manager = go.AddComponent<SessionTestManager>();
            for (int i = 0; i < 5; i++) {
                manager.StartSession(new SessionOptions { Mode = SessionMode.Peer });
                Assert.That(manager.IsRunning, Is.True);
                manager.DisconnectPlayer(0);
                Assert.That(manager.IsRunning, Is.False, "Disconnecting the local network player must release the session.");
                manager.StopSession(); manager.StopSession();
            }
            manager.StartSession(new SessionOptions { Mode = SessionMode.Local });
            yield return null;
            Assert.That(manager.Runner.Game.FrameNumber, Is.GreaterThan(0));
            manager.RestartSession();
            Assert.That(manager.Runner.Game.FrameNumber, Is.Zero);
            go.SetActive(false);
            Assert.That(manager.IsRunning, Is.False);
        } finally { Object.DestroyImmediate(go); }
    }
    [UnityTest] public IEnumerator SceneUnloadStopsNetworkSession() {
        var scene = SceneManager.CreateScene("Session unload test");
        var go = new GameObject("Scene-owned session"); SceneManager.MoveGameObjectToScene(go, scene);
        var manager = go.AddComponent<SessionTestManager>();
        manager.StartSession(new SessionOptions { Mode = SessionMode.Peer });
        yield return SceneManager.UnloadSceneAsync(scene);
        var next = new GameObject("Replacement session");
        try {
            var replacement = next.AddComponent<SessionTestManager>();
            replacement.StartSession(new SessionOptions { Mode = SessionMode.Peer });
            Assert.That(replacement.IsRunning, Is.True);
        } finally { Object.DestroyImmediate(next); }
    }
}
