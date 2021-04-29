using System.Diagnostics;
using Unity.Collections;
using UnityGGPO;

namespace SharedGame {

    public class LocalRunner : IGameRunner {
        private NativeArray<byte> buffer;

        public IGame Game { get; private set; }

        public GameInfo GameInfo { get; private set; }

        public void Idle(int ms) {
        }

        public void RunFrame() {
            var inputs = new long[GameInfo.players.Length];
            for (int i = 0; i < inputs.Length; ++i) {
                inputs[i] = Game.ReadInputs(GameInfo.players[i].controllerId);
            }
            Game.Update(inputs, 0);
        }

        public void OnTestSave() {
            if (buffer.IsCreated) {
                buffer.Dispose();
            }
            buffer = Game.ToBytes();
        }

        public void OnTestLoad() {
            Game.FromBytes(buffer);
        }

        public LocalRunner(IGame game) {
            Game = game;
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