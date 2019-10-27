using UnityEngine;

public class VectorWarRunner : MonoBehaviour {
    public VectorWar vw;
    public GameState gs;
    public NonGameState ngs;
    public VectorWarRenderer rend;
    public GGPOPerformance perf;
    public GGPOPlayer[] players;

    private void Awake() {
        vw = new VectorWar(gs, ngs, rend, perf);
        vw.VectorWar_Init(9000, players.Length, players, 0);
        vw.InitSpectator()
        vw.AdvanceFrame
        vw.DisconnectPlayer
            DrawCurrentFrame
            AdvanceFrame
            RunFrame
            Idle
            Exit
    }

    void Update() {
        Ve
    }
}
