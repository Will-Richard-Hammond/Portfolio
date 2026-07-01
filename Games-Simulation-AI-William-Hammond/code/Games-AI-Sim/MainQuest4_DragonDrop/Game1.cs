using MGGameLibrary.Shapes;
using MGGameLibrary.Steering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace MainQuest4_DragonDrop
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Texture2D _dragonsTexture;
        private Texture2D _coinTexture;
        private Texture2D _rockTexture;

        private Agent _agent;
        private Agent _agent2;          // second dragon — seeks the first
        private Agent _agentPath;       // third dragon — follows a path
        private Agent _agentFlee;       // fourth dragon — flees from the coin
        private Agent _truncatedSum;    // fifth dragon — flee coin + seek path follower (compound)
        private List<Coin> _coins;
        private Coin _coin;            // the single draggable target coin
        private Rock _rock;

        // ── Background colour — changes when the rock occludes the coin ───────
        private Color _backgroundColour = Color.CornflowerBlue;

        // ── Drag state ────────────────────────────────────────────────────────
        private bool _dragged = false;
        private Vector2 _dragOffset;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // SeekBehaviour and coins are set up in LoadContent once the viewport is available,
            // so Agent construction is deferred there. Components.Add happens after base.Initialize.
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _dragonsTexture = Content.Load<Texture2D>("dragons");
            _coinTexture    = Content.Load<Texture2D>("coins");
            _rockTexture    = Content.Load<Texture2D>("rock");

            int vw = GraphicsDevice.Viewport.Width;
            int vh = GraphicsDevice.Viewport.Height;

            // Place a handful of coins around the screen for the dragon to seek
            _coins = new List<Coin>
            {
                new Coin(new Vector2(vw * 0.25f, vh * 0.25f), this),
                new Coin(new Vector2(vw * 0.75f, vh * 0.25f), this),
                new Coin(new Vector2(vw * 0.50f, vh * 0.50f), this),
                new Coin(new Vector2(vw * 0.25f, vh * 0.75f), this),
                new Coin(new Vector2(vw * 0.75f, vh * 0.75f), this),
            };

            // Place a rock in the centre of the screen
            _rock = new Rock(new Vector2(vw * 0.5f, vh * 0.5f), 50f);

            // Dragon 1 — seeks the draggable coin and avoids the rock with whiskers.
            // Two angled forward whiskers probe for obstacles; heading 0 = facing -Y.
            _coin = _coins[0];
            List<SteeringBehaviour> dragon1Behaviours = new List<SteeringBehaviour>()
            {
                new SeekBehaviour(_coin),
                new AvoidCollidablesWithWhiskersBehaviour(
                    new List<MGGameLibrary.Interfaces.ICollidable>() { _rock },
                    new List<Vector2>()
                    {
                        new Vector2( 30, -100),   // forward-right whisker
                        new Vector2(-30, -100),   // forward-left whisker
                    }),
            };
            _agent = new Agent(new Vector2(100, 100), 0, this,
                new TruncatedSumSteeringBehaviour(dragon1Behaviours, 200f));
            Components.Add(_agent);

            // Dragon 2 seeks Dragon 1 — Agent implements ITargetable so it can be
            // passed directly to SeekBehaviour without any wrapper
            SeekBehaviour seek2 = new SeekBehaviour(_agent);
            _agent2 = new Agent(new Vector2(600, 400), 0, this, seek2);
            Components.Add(_agent2);

            // Dragon 3 — follows a rectangular patrol path around the screen
            var waypoints = new List<ITargetable>
            {
                new SimpleTargetable(new Vector2(vw * 0.10f, vh * 0.10f)),  // top-left
                new SimpleTargetable(new Vector2(vw * 0.90f, vh * 0.10f)),  // top-right
                new SimpleTargetable(new Vector2(vw * 0.90f, vh * 0.90f)),  // bottom-right
                new SimpleTargetable(new Vector2(vw * 0.10f, vh * 0.90f)),  // bottom-left
            };
            PathFollowingBehaviour pathBehaviour = new PathFollowingBehaviour(waypoints, arrivalThreshold: 40f);
            _agentPath = new Agent(new Vector2(vw * 0.10f, vh * 0.10f), 0, this, pathBehaviour);
            Components.Add(_agentPath);

            // Dragon 4 — flees the draggable coin
            FleeBehaviour fleeBehaviour = new FleeBehaviour(_coin);
            _agentFlee = new Agent(new Vector2(vw * 0.5f, vh * 0.3f), 0, this, fleeBehaviour);
            Components.Add(_agentFlee);

            // Dragon 5 — compound: flees the coin AND seeks the path follower (truncated sum)
            List<SteeringBehaviour> behaviours = new List<SteeringBehaviour>()
            {
                new FleeBehaviour(_coin),
                new SeekBehaviour(_agentPath),
            };
            TruncatedSumSteeringBehaviour truncatedSumSteeringForce = new TruncatedSumSteeringBehaviour(behaviours, 150.0f);
            _truncatedSum = new Agent(new Vector2(600, 300), 0, this, truncatedSumSteeringForce);
            Components.Add(_truncatedSum);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // ── Coin dragging ─────────────────────────────────────────────────
            if (_dragged)
            {
                _coin.MoveCoin(Mouse.GetState().Position.ToVector2() + _dragOffset);
            }

            if (Mouse.GetState().LeftButton == ButtonState.Pressed)
            {
                if (!_dragged)
                {
                    foreach (Coin coin in _coins)
                    {
                        if (coin.Circle.IsInside(Mouse.GetState().Position))
                        {
                            _coin = coin;
                            _dragged = true;
                            _dragOffset = coin.Circle.Centre - Mouse.GetState().Position.ToVector2();
                            break;
                        }
                    }
                }
            }
            else
            {
                _dragged = false;
            }

            // ── Line-of-sight: does the rock occlude the coin from the dragon? ─
            LineSegment toCoin = new LineSegment(_agent.Position, _coin.Circle.Centre);

            if (Shape.Intersects(toCoin, _rock.Circle))
                _backgroundColour = Color.SlateBlue;
            else
                _backgroundColour = Color.CornflowerBlue;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(_backgroundColour);

            _spriteBatch.Begin();

            // Draw the rock
            _rock.Draw(_spriteBatch, _rockTexture);

            // Draw each coin
            foreach (Coin coin in _coins)
                coin.Draw(_spriteBatch, _coinTexture);

            // Draw the dragon sprite, rotated around the sprite's centre point.
            // The dragons texture is a 3-column × 2-row sprite sheet.
            int spriteW = _dragonsTexture.Width  / 3;
            int spriteH = _dragonsTexture.Height / 2;
            Vector2 origin = new Vector2(_dragonsTexture.Width / 6f, _dragonsTexture.Height / 4f);

            // Dragon 1 — column 0, row 0
            _spriteBatch.Draw(
                _dragonsTexture,
                _agent.Position,
                new Rectangle(0, 0, spriteW, spriteH),
                Color.White,
                _agent.Heading,
                origin,
                1.0f,
                SpriteEffects.None,
                0f);

            // Dragon 2 — column 1, row 0 (different sprite to distinguish it)
            _spriteBatch.Draw(
                _dragonsTexture,
                _agent2.Position,
                new Rectangle(spriteW, 0, spriteW, spriteH),
                Color.White,
                _agent2.Heading,
                origin,
                1.0f,
                SpriteEffects.None,
                0f);

            // Dragon 3 — column 2, row 0 — path follower
            _spriteBatch.Draw(
                _dragonsTexture,
                _agentPath.Position,
                new Rectangle(spriteW * 2, 0, spriteW, spriteH),
                Color.White,
                _agentPath.Heading,
                origin,
                1.0f,
                SpriteEffects.None,
                0f);

            // Dragon 4 — column 0, row 1 — flees the coin
            _spriteBatch.Draw(
                _dragonsTexture,
                _agentFlee.Position,
                new Rectangle(0, spriteH, spriteW, spriteH),
                Color.White,
                _agentFlee.Heading,
                origin,
                1.0f,
                SpriteEffects.None,
                0f);

            // Dragon 5 — column 1, row 1 — truncated sum (flee coin + seek path follower)
            _spriteBatch.Draw(
                _dragonsTexture,
                _truncatedSum.Position,
                new Rectangle(spriteW, spriteH, spriteW, spriteH),
                Color.White,
                _truncatedSum.Heading,
                origin,
                1.0f,
                SpriteEffects.None,
                0f);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
