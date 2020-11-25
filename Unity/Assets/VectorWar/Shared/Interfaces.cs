using System.Diagnostics;

namespace SharedGame {

    public interface IConnectionInterface {

        IGame CreateGame();

        void SetVisible(bool v);
    }

    public interface IGame {
        IGameState gs { get; }
        GameInfo ngs { get; }

        void Init();

        void Idle(int ms);

        void RunFrame();

        string GetStatus(Stopwatch updateWatch);

        void DisconnectPlayer(int player);

        void Shutdown();
    }

    public interface IGameView {

        void UpdateGameView(IGame game);
    }
}