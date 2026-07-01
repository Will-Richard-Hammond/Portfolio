using MGGameLibrary.Graphs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace MainQuest5_PacManGhost
{
    // ??????????????????????????????????????????????????????????????????????????
    // Abstract base
    // ??????????????????????????????????????????????????????????????????????????

    /// <summary>
    /// Base class for all ghost states. Each state knows:
    ///   • Which labelled rectangle to draw (its "zone" on screen)
    ///   • What colour to use for the ghost indicator circle
    ///   • Its display name
    /// </summary>
    public abstract class GhostState : IState
    {
        // ?? Shared drawing resources (injected by Ghost on construction) ????
        public SpriteBatch  SpriteBatch { get; set; }
        public SpriteFont   Font        { get; set; }
        public Texture2D    Pixel       { get; set; }    // 1×1 white pixel for rectangles

        /// <summary>The coloured zone rectangle that represents this state on screen.</summary>
        public Rectangle Zone { get; }

        /// <summary>Colour used for the ghost dot indicator inside the zone.</summary>
        public Color GhostColour { get; }

        /// <summary>Human-readable name shown inside the zone.</summary>
        public string Label { get; }

        protected GhostState(Rectangle zone, Color ghostColour, string label)
        {
            Zone         = zone;
            GhostColour  = ghostColour;
            Label        = label;
        }

        // ?? IState ?????????????????????????????????????????????????????????
        public virtual void OnEnter()              { }
        public virtual void OnExit()               { }
        public virtual void OnUpdate(float seconds){ }

        // ?? Drawing ????????????????????????????????????????????????????????

        /// <summary>
        /// Draws the zone border. Active state is drawn by Ghost after calling this,
        /// which adds the ghost circle on top.
        /// </summary>
        public void DrawZone(bool isActive)
        {
            Color border = isActive ? GhostColour : Color.Gray;
            DrawRect(Zone, border, filled: false, thickness: isActive ? 4 : 2);

            // State label centred in the zone
            if (Font != null)
            {
                Vector2 size   = Font.MeasureString(Label);
                Vector2 centre = new Vector2(Zone.X + Zone.Width / 2f, Zone.Y + Zone.Height / 2f);
                SpriteBatch.DrawString(Font, Label, centre - size / 2f,
                    isActive ? Color.White : Color.Gray);
            }
        }

        // ?? Helpers ????????????????????????????????????????????????????????
        protected void DrawRect(Rectangle rect, Color colour, bool filled, int thickness = 2)
        {
            if (filled)
            {
                SpriteBatch.Draw(Pixel, rect, colour);
                return;
            }
            // Top
            SpriteBatch.Draw(Pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), colour);
            // Bottom
            SpriteBatch.Draw(Pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), colour);
            // Left
            SpriteBatch.Draw(Pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), colour);
            // Right
            SpriteBatch.Draw(Pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), colour);
        }

        protected void DrawCircle(Vector2 centre, float radius, Color colour, int segments = 24)
        {
            float step = MathF.PI * 2f / segments;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * step, a1 = (i + 1) * step;
                Vector2 p0 = centre + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * radius;
                Vector2 p1 = centre + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * radius;
                DrawLine(p0, p1, colour, 3);
            }
        }

        protected void DrawLine(Vector2 start, Vector2 end, Color colour, int thickness = 2)
        {
            Vector2 delta = end - start;
            float   angle = MathF.Atan2(delta.Y, delta.X);
            float   len   = delta.Length();
            SpriteBatch.Draw(Pixel,
                new Rectangle((int)start.X, (int)start.Y, (int)len, thickness),
                null, colour, angle, Vector2.Zero, SpriteEffects.None, 0f);
        }
    }

    // ??????????????????????????????????????????????????????????????????????????
    // Concrete states
    // ??????????????????????????????????????????????????????????????????????????

    /// <summary>Chase — ghost actively hunts Pac-Man. Exits when scatter timer elapses.</summary>
    public class ChaseState : GhostState
    {
        public Timer Timer { get; }

        public ChaseState(Rectangle zone, float duration)
            : base(zone, new Color(255, 40, 40), "CHASE")
        {
            Timer = new Timer(duration);
        }

        public override void OnEnter() => Timer.Reset();
        public override void OnUpdate(float seconds) => Timer.Update(seconds);
    }

    /// <summary>Scatter — ghost retreats to its corner. Exits when timer elapses.</summary>
    public class ScatterState : GhostState
    {
        public Timer Timer { get; }

        public ScatterState(Rectangle zone, float duration)
            : base(zone, new Color(255, 165, 0), "SCATTER")
        {
            Timer = new Timer(duration);
        }

        public override void OnEnter() => Timer.Reset();
        public override void OnUpdate(float seconds) => Timer.Update(seconds);
    }

    /// <summary>
    /// Frightened — ghost turns blue after Pac-Man eats a power pellet.
    /// Exits when timer elapses (or Pac-Man eats the ghost ? Eaten).
    /// </summary>
    public class FrightenedState : GhostState
    {
        public Timer Timer { get; }

        public FrightenedState(Rectangle zone, float duration)
            : base(zone, new Color(20, 20, 255), "FRIGHTENED")
        {
            Timer = new Timer(duration);
        }

        public override void OnEnter() => Timer.Reset();
        public override void OnUpdate(float seconds) => Timer.Update(seconds);
    }

    /// <summary>Eaten — ghost eyes return to the house. Exits when timer elapses.</summary>
    public class EatenState : GhostState
    {
        public Timer Timer { get; }

        public EatenState(Rectangle zone, float duration)
            : base(zone, new Color(200, 200, 255), "EATEN")
        {
            Timer = new Timer(duration);
        }

        public override void OnEnter() => Timer.Reset();
        public override void OnUpdate(float seconds) => Timer.Update(seconds);
    }
}
