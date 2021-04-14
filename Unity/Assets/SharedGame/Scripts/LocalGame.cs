using System.Diagnostics;
using Unity.Collections;

namespace SharedGame {

    public class LocalGame : IGame {
        private NativeArray<byte> buffer;

        public IGameState GameState { get; private set; }

        public GameInfo GameInfo { get; private set; }

        public void Idle(int ms) {
        }

        public void RunFrame() {
            var inputs = new ulong[GameInfo.players.Length];
            for (int i = 0; i < inputs.Length; ++i) {
                inputs[i] = GameState.ReadInputs(GameInfo.players[i].controllerId);
            }
            GameState.Update(inputs, 0);
        }

        public void OnTestSave() {
            if (buffer.IsCreated) {
                buffer.Dispose();
            }
            buffer = GameState.ToBytes();
        }

        public void OnTestLoad() {
            GameState.FromBytes(buffer);
        }

        public LocalGame(IGameState gameState) {
            GameState = gameState;
            GameInfo = new GameInfo();
            int handle = 1;
            int controllerId = 0;
            GameInfo.players = new PlayerConnectionInfo[2];
            GameInfo.players[0] = new PlayerConnectionInfo {
                handle = handle,
                type = GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL,
                connect_progress = 100,
                controllerId = controllerId
            };
            GameInfo.SetConnectState(handle, PlayerConnectState.Connecting);
            ++handle;
            ++controllerId;
            GameInfo.players[1] = new PlayerConnectionInfo {
                handle = handle,
                type = GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL,
                connect_progress = 100,
                controllerId = controllerId++
            };
            GameInfo.SetConnectState(handle, PlayerConnectState.Connecting);
        }

        public string GetStatus(Stopwatch updateWatch) {
            return string.Format("time{0:.00}", (float)updateWatch.ElapsedMilliseconds);
        }

        public void DisconnectPlayer(int player) {
        }

        public void Shutdown() {
            if (buffer.IsCreated) {
                buffer.Dispose();
            }
        }
    }
}