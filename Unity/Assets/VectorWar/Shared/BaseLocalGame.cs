using System.Diagnostics;
using Unity.Collections;

namespace SharedGame {
    public abstract class BaseLocalGame : IGame {
        private NativeArray<byte> buffer;

        public IGameState gs { get; private set; }

        public GameInfo ngs { get; private set; }

        public void Init(IGameState _gs, GameInfo _ngs) {
            gs = _gs;
            ngs = _ngs;
        }

        public void Idle(int ms) {
        }

        public void RunFrame() {
            var inputs = new ulong[ngs.players.Length];
            for (int i = 0; i < inputs.Length; ++i) {
                inputs[i] = gs.ReadInputs(ngs.players[i].controllerId);
            }
            gs.Update(inputs, 0);
        }

        public void OnTestSave() {
            if (buffer.IsCreated) {
                buffer.Dispose();
            }
            buffer = gs.ToBytes();
        }

        public void OnTestLoad() {
            gs.FromBytes(buffer);
        }

        public void Init() {
            Init(this.CreateGameState(), new GameInfo());
            int handle = 1;
            int controllerId = 0;
            ngs.players = new PlayerConnectionInfo[2];
            ngs.players[0] = new PlayerConnectionInfo {
                handle = handle,
                type = GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL,
                connect_progress = 100,
                controllerId = controllerId
            };
            ngs.SetConnectState(handle, PlayerConnectState.Connecting);
            ++handle;
            ++controllerId;
            ngs.players[1] = new PlayerConnectionInfo {
                handle = handle,
                type = GGPOPlayerType.GGPO_PLAYERTYPE_LOCAL,
                connect_progress = 100,
                controllerId = controllerId++
            };
            ngs.SetConnectState(handle, PlayerConnectState.Connecting);
            gs.Init(ngs.players.Length);
        }

        protected abstract IGameState CreateGameState();

        protected abstract string GetName();

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