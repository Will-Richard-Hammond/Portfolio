using MGGameLibrary.Graphs;

namespace MainQuest5_PacManGhost
{
    /// <summary>
    /// Fires when the GameScreenState's Playing property becomes false.
    /// </summary>
    public class GameOverTransition : IStateTransition
    {
        private readonly GameScreenState _gameScreen;

        public GameOverTransition(GameScreenState gameScreen) => _gameScreen = gameScreen;

        public bool ToTransition() => !_gameScreen.Playing;
    }
}
