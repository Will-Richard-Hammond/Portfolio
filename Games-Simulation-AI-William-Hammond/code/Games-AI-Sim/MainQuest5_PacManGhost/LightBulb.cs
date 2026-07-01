using MGGameLibrary.Graphs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace MainQuest5_PacManGhost
{
    /// <summary>
    /// A GameComponent that manages a light bulb FSM.
    /// Press and hold O to turn on; release O to turn off.
    /// </summary>
    public class LightBulb : GameComponent
    {
        private FiniteStateMachine<LightBulbState, IStateTransition> _fsm;

        /// <summary>Returns the source rectangle of the current FSM state for drawing.</summary>
        public Rectangle SourceRectangle => _fsm.CurrentState.SourceRectangle;

        public LightBulb(Game game) : base(game)
        {
            LightBulbOffState offState = new(new Rectangle(0, 0, 112, 150));
            LightBulbOnState onState  = new(new Rectangle(112, 0, 112, 150));

            KeyPressTransition   turnOnTransition  = new(Keys.O);
            KeyReleaseTransition turnOffTransition = new(Keys.O);

            SparseGraph<LightBulbState, IStateTransition> graph = new();

            graph.AddNode(offState);
            graph.AddNode(onState);
            graph.AddEdge(offState, turnOnTransition,  onState);
            graph.AddEdge(onState,  turnOffTransition, offState);

            _fsm = new FiniteStateMachine<LightBulbState, IStateTransition>(graph, offState);
        }

        public override void Update(GameTime gameTime)
        {
            float seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _fsm.Update(seconds);
            base.Update(gameTime);
        }
    }
}
