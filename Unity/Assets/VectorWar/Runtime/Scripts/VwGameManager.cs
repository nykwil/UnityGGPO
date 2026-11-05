using SharedGame;
namespace VectorWar {
    public sealed class VwGameManager : GameManager {
        protected override IGame CreateGame(int playerCount) => new VwGame(playerCount);
        protected override ILocalInputSource CreateInputSource() => new VwInputSource();
    }
}
