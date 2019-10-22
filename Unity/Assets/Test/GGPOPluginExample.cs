using System;
using System.Text;
using Unity.Collections;
using UnityEngine;

public class GGPOPluginExample : MonoBehaviour {
    static StringBuilder console = new StringBuilder();

    GGPOSession session;
    NativeArray<byte> data;

    // Use this for initialization
    void Start() {
        data = new NativeArray<byte>(new byte[] { 0, 1, 2, 3, 4, 5, 6 }, Allocator.Persistent);
        Log(string.Format("Plugin Version: {0} build {1}", GGPO.Version, GGPO.BuildNumber));

        session = new GGPOSession();
        session.logDelegate = Log;
        session.loadGameStateCallback = loadGameStateCallback;
//        session.logGameStateCallback = LogGameState;
//        session.saveGameStateDelegate = Callback;
    }

    private bool loadGameStateCallback(NativeArray<byte> data) {
        throw new NotImplementedException();
    }

    private void OnDestroy() {
        if (data.IsCreated) {
            data.Dispose();
        }
    }

    //unsafe void* Callback(out int length, out int checksum, int frame) {
    //    Debug.Log("Callback " + frame);
    //    checksum = 2;
    //    length = data.Length;
    //    return GGPO.ToPtr(data);
    //}

    //unsafe bool LogGameState(void* buffer, int length) {
    //    return true;
    //}

    public static void Log(string obj) {
        Debug.Log(obj);
        console.Append(obj + "\n");
    }

    void OnGUI() {
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), console.ToString());
    }
}
