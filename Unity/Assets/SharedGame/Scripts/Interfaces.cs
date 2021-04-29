using System.Diagnostics;
using Unity.Collections;

namespace SharedGame {

    public class Connections {
        public ushort port;
        public string ip;
        public bool spectator;
    }

    public interface IGame {
        int Framenumber { get; }
        int Checksum { get; }

        void Update(long[] inputs, int disconnectFlags);

        void FromBytes(NativeArray<byte> data);

        NativeArray<byte> ToBytes();

        long ReadInputs(int controllerId);

        void LogInfo(string filename);

        void FreeBytes(NativeArray<byte> data);
    }

    public interface IGameRunner {
        IGame Game { get; }
        GameInfo GameInfo { get; }

        void Idle(int ms);

        void RunFrame();

        string GetStatus(Stopwatch updateWatch);

        void DisconnectPlayer(int player);

        void Shutdown();
    }

    public interface IGameView {

        void UpdateGameView(IGameRunner runner);
    }
}