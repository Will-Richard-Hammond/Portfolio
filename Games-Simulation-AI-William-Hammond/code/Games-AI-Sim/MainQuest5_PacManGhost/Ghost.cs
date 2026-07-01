using MGGameLibrary.Graphs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace MainQuest5_PacManGhost
{
    /// <summary>
    /// DrawableGameComponent that owns the Pac-Man ghost FSM.
    ///
    /// State diagram
    /// ?????????????
    ///  Chase  ??[scatter timer]???  Scatter
    ///  Chase  ??[P: power pellet]?? Frightened
    ///  Scatter??[scatter timer]???  Chase
    ///  Scatter??[P: power pellet]?? Frightened
    ///  Frightened?[E: eaten]??????? Eaten
    ///  Frightened?[fright timer]??? Chase
    ///  Eaten  ??[eaten timer]?????? Chase
    ///
    /// Keys
    ///  P  — Pac-Man eats a power pellet  (any ? Frightened, if allowed)
    ///  E  — Pac-Man eats the ghost        (Frightened ? Eaten)
    /// </summary>
    public class Ghost : DrawableGameComponent
    {
        // ?? FSM ????????????????????????????????????????????????????????????
        private readonly FiniteStateMachine<GhostState, IStateTransition> _fsm;
        private readonly ChaseState      _chase;
        private readonly ScatterState    _scatter;
        private readonly FrightenedState _frightened;
        private readonly EatenState      _eaten;

        // ?? Drawing ????????????????????????????????????????????????????????
        private SpriteBatch _spriteBatch;
        private SpriteFont  _font;
        private Texture2D   _pixel;       // 1×1 white used for all solid drawing

        // Ghost dot position — lerps smoothly between zone centres
        private Vector2 _ghostPos;
        private const float LerpSpeed = 4f;

        // ?? Layout constants ???????????????????????????????????????????????
        private const int ZoneW  = 200;
        private const int ZoneH  = 130;
        private const int Pad    = 40;
        private const int StartY = 80;

        public Ghost(Game game) : base(game)
        {
            int cx = game.GraphicsDevice.Viewport.Width  / 2;
            int row1Y = StartY;
            int row2Y = StartY + ZoneH + Pad;

            // ?? Zone rectangles ????????????????????????????????????????????
            Rectangle chaseRect      = new(cx - ZoneW - Pad / 2, row1Y,               ZoneW, ZoneH);
            Rectangle scatterRect    = new(cx + Pad / 2,          row1Y,               ZoneW, ZoneH);
            Rectangle frightenedRect = new(cx - ZoneW - Pad / 2, row2Y,               ZoneW, ZoneH);
            Rectangle eatenRect      = new(cx + Pad / 2,          row2Y,               ZoneW, ZoneH);

            // ?? States ????????????????????????????????????????????????????
            _chase      = new ChaseState(chaseRect,           7f);
            _scatter    = new ScatterState(scatterRect,       7f);
            _frightened = new FrightenedState(frightenedRect, 6f);
            _eaten      = new EatenState(eatenRect,           3f);

            _ghostPos = ZoneCentre(_chase);

            // ?? Transitions ???????????????????????????????????????????????
            // Timer-based
            TimedTransition chaseTimeout      = new(_chase.Timer);
            TimedTransition scatterTimeout    = new(_scatter.Timer);
            TimedTransition frightenedTimeout = new(_frightened.Timer);
            TimedTransition eatenTimeout      = new(_eaten.Timer);

            // Key-based — each transition instance is single-use edge
            KeyPressTransition pelletFromChase    = new(Keys.P);
            KeyPressTransition pelletFromScatter  = new(Keys.P);
            KeyPressTransition eatGhost           = new(Keys.E);

            // ?? Graph ?????????????????????????????????????????????????????
            SparseGraph<GhostState, IStateTransition> graph = new();

            // Chase  ??? Scatter (timer)
            graph.AddEdge(_chase,      chaseTimeout,       _scatter);
            // Chase  ??? Frightened (power pellet)
            graph.AddEdge(_chase,      pelletFromChase,    _frightened);

            // Scatter ??? Chase (timer)
            graph.AddEdge(_scatter,    scatterTimeout,     _chase);
            // Scatter ??? Frightened (power pellet)
            graph.AddEdge(_scatter,    pelletFromScatter,  _frightened);

            // Frightened ??? Eaten (E key)
            graph.AddEdge(_frightened, eatGhost,           _eaten);
            // Frightened ??? Chase (timer expired)
            graph.AddEdge(_frightened, frightenedTimeout,  _chase);

            // Eaten ??? Chase (timer expired — ghost returns to normal)
            graph.AddEdge(_eaten,      eatenTimeout,       _chase);

            _fsm = new FiniteStateMachine<GhostState, IStateTransition>(graph, _chase);
        }

        // ?? Lifecycle ??????????????????????????????????????????????????????

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = Game.Content.Load<SpriteFont>("GhostFont");

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            // Inject shared resources into every state
            foreach (var state in new GhostState[] { _chase, _scatter, _frightened, _eaten })
            {
                state.SpriteBatch = _spriteBatch;
                state.Font        = _font;
                state.Pixel       = _pixel;
            }

            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _fsm.Update(dt);

            // Smoothly move ghost dot toward active zone centre
            Vector2 target = ZoneCentre(_fsm.CurrentState);
            _ghostPos = Vector2.Lerp(_ghostPos, target, 1f - MathF.Pow(0.001f, dt * LerpSpeed));

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            _spriteBatch.Begin();

            GhostState current = _fsm.CurrentState;

            // ?? Draw all four zone boxes ???????????????????????????????????
            foreach (var state in new GhostState[] { _chase, _scatter, _frightened, _eaten })
                state.DrawZone(state == current);

            // ?? Draw arrows between boxes (static layout) ?????????????????
            DrawArrows(current);

            // ?? Draw the ghost dot ????????????????????????????????????????
            DrawGhostDot(current.GhostColour);

            // ?? Draw HUD ?????????????????????????????????????????????????
            DrawHUD(current);

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        // ?? Helpers ????????????????????????????????????????????????????????

        private static Vector2 ZoneCentre(GhostState s) =>
            new(s.Zone.X + s.Zone.Width / 2f, s.Zone.Y + s.Zone.Height / 2f);

        private void DrawGhostDot(Color colour)
        {
            const float R  = 28f;
            const float EyeR = 5f;

            // Body — filled circle approximation (filled rectangle clipped by draw order)
            DrawFilledCircle(_ghostPos, R, colour);

            // Eyes
            DrawFilledCircle(_ghostPos + new Vector2(-9, -8), EyeR, Color.White);
            DrawFilledCircle(_ghostPos + new Vector2( 9, -8), EyeR, Color.White);
            DrawFilledCircle(_ghostPos + new Vector2(-7, -8), EyeR * 0.5f, Color.DarkBlue);
            DrawFilledCircle(_ghostPos + new Vector2(11, -8), EyeR * 0.5f, Color.DarkBlue);
        }

        private void DrawFilledCircle(Vector2 centre, float radius, Color colour)
        {
            // Approximate a filled circle with a tight grid of 1-px pixels
            int r = (int)MathF.Ceiling(radius);
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                    if (dx * dx + dy * dy <= radius * radius)
                        _spriteBatch.Draw(_pixel,
                            new Rectangle((int)centre.X + dx, (int)centre.Y + dy, 1, 1),
                            colour);
        }

        private void DrawArrows(GhostState current)
        {
            Color col = new Color(180, 180, 180);

            // Chase ? Scatter
            DrawArrow(RightMid(_chase.Zone),   LeftMid(_scatter.Zone),   col);
            DrawArrow(LeftMid(_scatter.Zone),  RightMid(_chase.Zone),    col);

            // Chase ? Frightened
            DrawArrow(BottomMid(_chase.Zone),  TopMid(_frightened.Zone), col);

            // Scatter ? Frightened
            DrawArrow(BottomMid(_scatter.Zone), TopMid(_frightened.Zone), col);

            // Frightened ? Eaten
            DrawArrow(RightMid(_frightened.Zone), LeftMid(_eaten.Zone),   col);

            // Frightened ? Chase (timer)
            // use a curved offset so the two arrows don't overlap
            Vector2 frightenedTop = TopMid(_frightened.Zone) + new Vector2(-20, 0);
            Vector2 chaseBot      = BottomMid(_chase.Zone)   + new Vector2(-20, 0);
            DrawArrow(frightenedTop, chaseBot, col);

            // Eaten ??? Chase (arc up through right side)
            DrawArrow(TopMid(_eaten.Zone) + new Vector2(20, 0),
                      BottomMid(_scatter.Zone) + new Vector2(20, 0), col);
        }

        private void DrawArrow(Vector2 from, Vector2 to, Color colour)
        {
            DrawLine(from, to, colour, 2);

            // Arrowhead
            Vector2 dir  = Vector2.Normalize(to - from);
            Vector2 perp = new Vector2(-dir.Y, dir.X);
            const float HeadLen = 10f, HeadW = 5f;
            Vector2 tip  = to;
            Vector2 base1 = tip - dir * HeadLen + perp * HeadW;
            Vector2 base2 = tip - dir * HeadLen - perp * HeadW;
            DrawLine(tip, base1, colour, 2);
            DrawLine(tip, base2, colour, 2);
        }

        private void DrawLine(Vector2 start, Vector2 end, Color colour, int thickness = 2)
        {
            Vector2 delta = end - start;
            float   angle = MathF.Atan2(delta.Y, delta.X);
            float   len   = delta.Length();
            _spriteBatch.Draw(_pixel,
                new Rectangle((int)start.X, (int)start.Y, (int)len, thickness),
                null, colour, angle, Vector2.Zero, SpriteEffects.None, 0f);
        }

        private static Vector2 RightMid(Rectangle r)  => new(r.Right,        r.Y + r.Height / 2f);
        private static Vector2 LeftMid(Rectangle r)   => new(r.Left,         r.Y + r.Height / 2f);
        private static Vector2 TopMid(Rectangle r)    => new(r.X + r.Width / 2f, r.Top);
        private static Vector2 BottomMid(Rectangle r) => new(r.X + r.Width / 2f, r.Bottom);

        private void DrawHUD(GhostState current)
        {
            if (_font == null) return;

            int vw = GraphicsDevice.Viewport.Width;
            int vh = GraphicsDevice.Viewport.Height;

            // ?? Key legend ????????????????????????????????????????????????
            string legend =
                "P = Power Pellet (Chase/Scatter ? Frightened)\n" +
                "E = Eat Ghost    (Frightened ? Eaten)\n" +
                "Timers auto-advance all other transitions";

            _spriteBatch.DrawString(_font, legend,
                new Vector2(Pad, vh - 80), Color.LightGray);

            // ?? Active state name + timer ??????????????????????????????????
            string stateLine = $"State: {current.Label}";
            Timer t = GetTimer(current);
            if (t != null)
                stateLine += $"   [{t.TimeRemaining:F1}s remaining]";

            Vector2 stateSize = _font.MeasureString(stateLine);
            _spriteBatch.DrawString(_font, stateLine,
                new Vector2((vw - stateSize.X) / 2f, 20), current.GhostColour);

            // ?? Timer bar ?????????????????????????????????????????????????
            if (t != null)
            {
                const int BarW = 300, BarH = 12;
                int barX = (vw - BarW) / 2;
                int barY = 44;
                float fill = Math.Clamp(t.TimeRemaining / t.Duration, 0f, 1f);

                // Background
                _spriteBatch.Draw(_pixel, new Rectangle(barX, barY, BarW, BarH), Color.DarkGray);
                // Fill
                _spriteBatch.Draw(_pixel, new Rectangle(barX, barY, (int)(BarW * fill), BarH),
                    current.GhostColour);
            }
        }

        private static Timer GetTimer(GhostState s) => s switch
        {
            ChaseState      c => c.Timer,
            ScatterState    sc=> sc.Timer,
            FrightenedState f => f.Timer,
            EatenState      e => e.Timer,
            _                 => null
        };
    }
}
