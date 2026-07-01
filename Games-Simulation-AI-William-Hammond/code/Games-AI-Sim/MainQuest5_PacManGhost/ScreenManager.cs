using MGGameLibrary.Graphs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MainQuest5_PacManGhost
{
    /// <summary>
    /// DrawableGameComponent that owns the screen-management FSM.
    /// Constructs all screen states, wires them into a SparseGraph,
    /// and drives the FSM each frame.
    /// </summary>
    public class ScreenManager : DrawableGameComponent
    {
        private FiniteStateMachine<ScreenState, IStateTransition> _fsm;

        public ScreenManager(Game game) : base(game)
        {
            // ?? Load content ??????????????????????????????????????????????
            Texture2D logo = Game.Content.Load<Texture2D>("lightBulb"); // swap for AjaniLink when available

            // ?? States ????????????????????????????????????????????????????
            FlashScreenState    flashScreen    = new(1f, logo, Game);
            TitleScreenState    titleScreen    = new(Game);
            CreditsScreenState  creditsScreen  = new(Game);
            GameScreenState     gameScreen     = new(Game);
            PauseScreenState    pauseScreen    = new(Game);
            GameOverScreenState gameOverScreen = new(2f, gameScreen, Game);

            // ?? Transitions ???????????????????????????????????????????????
            KeyPressTransition pressC     = new(Keys.C);
            KeyPressTransition pressP     = new(Keys.P);
            KeyPressTransition pressSpace = new(Keys.Space);

            TimedTransition timedFlashScreenTransition    = new(flashScreen.Timer);
            TimedTransition timedGameOverScreenTransition = new(gameOverScreen.Timer);
            GameOverTransition gameOverTransition         = new(gameScreen);

            // ?? Graph ?????????????????????????????????????????????????????
            SparseGraph<ScreenState, IStateTransition> graph = new();

            graph.AddEdge(flashScreen,    timedFlashScreenTransition,    titleScreen);
            graph.AddEdge(titleScreen,    pressC,                        creditsScreen);
            graph.AddEdge(creditsScreen,  pressC,                        titleScreen);
            graph.AddEdge(titleScreen,    pressSpace,                    gameScreen);
            graph.AddEdge(pauseScreen,    pressP,                        gameScreen);
            graph.AddEdge(gameScreen,     pressP,                        pauseScreen);
            graph.AddEdge(gameScreen,     gameOverTransition,            gameOverScreen);
            graph.AddEdge(gameOverScreen, timedGameOverScreenTransition, titleScreen);

            _fsm = new FiniteStateMachine<ScreenState, IStateTransition>(graph, flashScreen);
        }

        public override void Update(GameTime gameTime)
        {
            float seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _fsm.Update(seconds);
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            _fsm.CurrentState.Draw(gameTime);
            base.Draw(gameTime);
        }
    }
}
