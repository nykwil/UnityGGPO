using SharedGame;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NewConnectionPanel : MonoBehaviour {
    public Button btnLocal;
    public Button btnRemote;
    public Button btnHost;

    public GameRunner runner;

    public static void LogTodo(string s) {
        // @TODO
        Debug.Log(s);
    }

    private void Awake() {
        btnHost.onClick.AddListener(OnHostClick);
        btnRemote.onClick.AddListener(OnRemoteClick);
        btnLocal.onClick.AddListener(OnLocalClick);
    }

    private void OnDestroy() {
        btnHost.onClick.RemoveListener(OnHostClick);
        btnRemote.onClick.RemoveListener(OnRemoteClick);
        btnLocal.onClick.RemoveListener(OnLocalClick);
    }

    public Text inpIps;

    private List<Connections> GetConnections() {
        var list = new List<Connections>();
        var split = inpIps.text.Split(':');
        list.Add(new Connections() {
            port = ushort.Parse(split[1]),
            ip = split[0],
            spectator = false
        });
        return list;
    }

    private void OnHostClick() {
        var perf = FindObjectOfType<GGPOPerformancePanel>();
        perf.Setup();
        var game = new VectorWar.VwGGPOGame(perf, LogTodo);
        game.Init(GetConnections(), 0);
        runner.Startup(game);
    }

    private void OnRemoteClick() {
        var perf = FindObjectOfType<GGPOPerformancePanel>();

        var game = new VectorWar.VwGGPOGame(perf, LogTodo);
        game.Init(GetConnections(), 1);
        runner.Startup(game);
    }

    private void OnLocalClick() {
        var game = new VectorWar.VwLocalGame();
        runner.Startup(game);
    }
}