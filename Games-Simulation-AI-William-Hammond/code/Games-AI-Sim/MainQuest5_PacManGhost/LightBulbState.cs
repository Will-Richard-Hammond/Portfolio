using MGGameLibrary.Graphs;
using Microsoft.Xna.Framework;

namespace MainQuest5_PacManGhost
{
    /// <summary>
    /// Abstract base class for all light bulb states.
    /// Holds the sprite-sheet source rectangle for drawing.
    /// </summary>
    public abstract class LightBulbState : IState
    {
        public Rectangle SourceRectangle { get; protected set; }

        protected LightBulbState(Rectangle sourceRectangle)
        {
            SourceRectangle = sourceRectangle;
        }

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void OnUpdate(float seconds) { }
    }

    /// <summary>Light bulb is off — uses the left half of the sprite sheet.</summary>
    public class LightBulbOffState : LightBulbState
    {
        public LightBulbOffState(Rectangle sourceRectangle) : base(sourceRectangle) { }
    }

    /// <summary>Light bulb is on — uses the right half of the sprite sheet.</summary>
    public class LightBulbOnState : LightBulbState
    {
        public LightBulbOnState(Rectangle sourceRectangle) : base(sourceRectangle) { }
    }
}
