using System.Diagnostics;
using Unity.Collections;

namespace SharedGame {

    public static class Utils {

        public static T GetInterface<T>(GameObject inObj) where T : class {
            if (!typeof(T).IsInterface) {
                UnityEngine.Debug.LogError(typeof(T).ToString() + ": is not an actual interface!");
                return null;
            }

            return inObj.GetComponents<Component>().OfType<T>().FirstOrDefault();
        }
    }

    public interface IGameState {
        int _framenumber { get; }

        void Init(int num_players);

        void Update(ulong[] inputs, int disconnect_flags);

        void FromBytes(NativeArray<byte> data);

        NativeArray<byte> ToBytes();

        ulong ReadInputs(int controllerId);

        void LogInfo(string filename);
    }

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