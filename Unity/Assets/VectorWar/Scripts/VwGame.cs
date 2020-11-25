using SharedGame;

namespace VectorWar {

    public class VwGGPOGame : BaseGGPOGame {

        public override string GetName() {
            return "vectorwar";
        }

        protected override IGameState CreateGameState() {
            return new VwGameState();
        }
    }

    public class VwLocalGame : BaseLocalGame {

        protected override string GetName() {
            return "vectorwar";
        }

        protected override IGameState CreateGameState() {
            return new VwGameState();
        }
    }
}