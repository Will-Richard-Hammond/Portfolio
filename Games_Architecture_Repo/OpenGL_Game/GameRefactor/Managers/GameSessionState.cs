namespace OpenGL_Game.GameRefactor.Managers
{
    static class GameSessionState
    {
        public static string LastGameResultMessage { get; set; } = "Game Over";

        /// <summary>Score carried from the game scene to the game-over scene.</summary>
        public static int LastScore { get; set; } = 0;
    }
}