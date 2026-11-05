#if UNITY_EDITOR
using System.Collections;
using System.Net;
using System.Net.Sockets;
using NUnit.Framework;
using SharedGame.Samples.UnityTransport;
using SharedGame.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Nykwil.GekkoNet.Session;
public sealed class TransportLifecycleTests {
    [UnityTest] public IEnumerator SharedManagerStopAndDisableReleaseTransportAndSocket() {
        var go = new GameObject("Transport manager lifecycle"); go.SetActive(false);
        var panel = SharedGameEditorUtility.CreatePanelSettings();
        bool previousBackground = Application.runInBackground;
        try {
            var document=go.AddComponent<PanelRenderer>();document.panelSettings=panel;
            document.visualTreeAsset=AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Samples/RollbackDuel/Duel.uxml");
            var manager=go.AddComponent<UnityTransportDuel>();manager.gameDocument=document;
            var options=new SessionOptions { Mode=SessionMode.Peer, Players=new[] {
                new SessionEndpoint { Port=35700 },new SessionEndpoint { Port=35701 }
            }};
            go.SetActive(true);
            Assert.That(Application.runInBackground,Is.True,"Unfocused peers must keep polling.");
            for(int i=0;i<3;i++) {
                manager.StartSession(options);yield return null;
                Assert.That(manager.IsRunning,Is.True);
                if(i==1)go.SetActive(false);else manager.StopSession();
                Assert.That(manager.IsRunning,Is.False);
                using(var socket=new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp))socket.Bind(new IPEndPoint(IPAddress.Any,35700));
                using(var adapter=new UnityTransportAdapter(options))Assert.That(adapter.NativeAdapter,Is.Not.EqualTo(System.IntPtr.Zero));
                go.SetActive(true);
            }
        }finally{Object.DestroyImmediate(go);Object.DestroyImmediate(panel);Application.runInBackground=previousBackground;}
    }
}
#endif
