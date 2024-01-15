using UnityEngine;
using UnityGGPO;

public class TestUnityGGPO : MonoBehaviour {
    private static System.Diagnostics.Stopwatch sleepWatch = new();

    private void Start() {
        Debug.Log(GGPO.BuildNumber);

        Debug.Log(GGPO.Version);

        Debug.Log(GGPO.UggTimeGetTime());

        Test1(1000);
        Test2(1200);
    }

    private static void Test1(int ms) {
        sleepWatch.Start();
        GGPO.UggSleep(ms);
        sleepWatch.Stop();
        UnityEngine.Debug.Log($"ms {ms} actual {sleepWatch.ElapsedMilliseconds}");
    }

    private static void Test2(int ms) {
        int start = Utils.TimeGetTime();
        GGPO.UggSleep(ms);
        int end = Utils.TimeGetTime();
        UnityEngine.Debug.Log($"ms {ms} actual {end - start}");
    }

    private static void Test3(int ms) {
        int start = Utils.TimeGetTime();
        System.Threading.Thread.Sleep(ms);
        int end = Utils.TimeGetTime();
        UnityEngine.Debug.Log($"ms {ms} actual {end - start}");
    }

    public int frames = 4;

    private void Update() {
        int ms = (int)(1000f * frames / 60f);
        if (test1) {
            test1 = false;
            Test1(ms);
        }
        if (test2) {
            test2 = false;
            Test2(ms);
        }
        if (test3) {
            test3 = false;
            Test3(ms);
        }
    }

    private bool test1;
    private bool test2;
    private bool test3;

    private void OnGUI() {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Test1")) {
            test1 = true;
        }
        if (GUILayout.Button("Test2")) {
            test2 = true;
        }
        if (GUILayout.Button("Test3")) {
            test3 = true;
        }
        GUILayout.EndHorizontal();
    }
}