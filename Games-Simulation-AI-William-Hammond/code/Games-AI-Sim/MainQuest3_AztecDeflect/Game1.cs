
using MGGameLibrary.Interfaces;
using MGGameLibrary.Shapes;
using MGGameLibrary.Abstract;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using static MainQuest3_AztecDeflect.EnergyOrb;

namespace MainQuest3_AztecDeflect
{
    public class Game1 : Game
    {
        // ── Core ──────────────────────────────────────────────────────────────
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;
        private Texture2D _textureAtlas;

        // ── Game objects ──────────────────────────────────────────────────────
        private PlayerShip _playerShip;
        private List<EnergyOrb> _orbsList;
        private List<Obstacle> _obstaclesList;
        private List<Disc> _discsList;

        // ── Game state ────────────────────────────────────────────────────────
        private int _score = 0;
        private int _lives = 3;
        private bool _gameOver = false;

        // ── Tonaltin spawning ─────────────────────────────────────────────────
        private float _spawnTimer = 0f;
        private float _spawnInterval = 3f;          // seconds between Tonaltin drops
        private readonly Random _rng = new Random();

        // ── Constants ─────────────────────────────────────────────────────────
        private const float SPAWN_SPEED = 60f;      // initial downward speed (px/s)

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _orbsList     = new List<EnergyOrb>();
            _obstaclesList = new List<Obstacle>();
            _discsList    = new List<Disc>();
        }

        protected override void Initialize()
        {
            _playerShip = new PlayerShip(this);
            Components.Add(_playerShip);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _textureAtlas = Content.Load<Texture2D>("TextureAtlas");
            _font         = Content.Load<SpriteFont>("GameFont");

            int vw = GraphicsDevice.Viewport.Width;
            int vh = GraphicsDevice.Viewport.Height;

            // -- Tier 1 (top): crown of three circles --
            // The apex splits Tonaltin falling straight down; shoulders deflect
            // them toward the diamond below.
            _obstaclesList.Add(new Obstacle(new Circle(new Vector2(vw * 0.50f, vh * 0.12f), 45)));  // apex
            _obstaclesList.Add(new Obstacle(new Circle(new Vector2(vw * 0.25f, vh * 0.20f), 38)));  // left shoulder
            _obstaclesList.Add(new Obstacle(new Circle(new Vector2(vw * 0.75f, vh * 0.20f), 38)));  // right shoulder

            // -- Tier 2 (middle): three circles in a wide diamond --
            // The flanking pair funnel objects inward; the centre bump sends
            // Tonaltin flying back up or to the sides for scoring opportunities.
            _obstaclesList.Add(new Obstacle(new Circle(new Vector2(vw * 0.18f, vh * 0.46f), 30)));  // far left
            _obstaclesList.Add(new Obstacle(new Circle(new Vector2(vw * 0.50f, vh * 0.42f), 35)));  // centre
            _obstaclesList.Add(new Obstacle(new Circle(new Vector2(vw * 0.82f, vh * 0.46f), 30)));  // far right

            // -- Tier 3 (lower): two square altar-stones --
            // Placed close to the player's firing line, these redirect any
            // Tonaltin that slip through the upper tiers, giving the player
            // one last chance to knock them sideways.
            _obstaclesList.Add(new Obstacle(new Square(new Vector2(vw * 0.15f, vh * 0.64f), 70)));  // left altar
            _obstaclesList.Add(new Obstacle(new Square(new Vector2(vw * 0.75f, vh * 0.64f), 70)));  // right altar
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Freeze everything on game over
            if (_gameOver)
            {
                base.Update(gameTime);
                return;
            }

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // ── Fire orbs ─────────────────────────────────────────────────────
            if (Keyboard.GetState().IsKeyDown(Keys.Space))
            {
                EnergyOrb orb = _playerShip.FireEnergyOrb(gameTime);
                if (orb != null)
                {
                    _orbsList.Add(orb);
                    if (!Components.Contains(orb))
                        Components.Add(orb);
                }
            }

            // ── Orb vs obstacle collisions ────────────────────────────────────
            for (int i = 0; i < _orbsList.Count; ++i)
            {
                var orb = _orbsList[i];
                foreach (Obstacle obstacle in _obstaclesList)
                {
                    Vector2 collisionNormal = Vector2.Zero;
                    if (obstacle.CollidesWith(orb, ref collisionNormal))
                    {
                        orb.RevertToPreviousPosition();
                        orb.Velocity = Vector2.Reflect(orb.Velocity, collisionNormal);
                        orb.Position += collisionNormal;
                    }
                }
            }

            RemoveOrbs();

            // ── Spawn Tonaltin ────────────────────────────────────────────────
            _spawnTimer += dt;
            if (_spawnTimer >= _spawnInterval)
            {
                _spawnTimer = 0f;
                SpawnTonaltin();
            }

            // ── Disc physics + obstacle collisions ────────────────────────────
            for (int i = 0; i < _discsList.Count; ++i)
            {
                Disc disc = _discsList[i];

                disc.ApplyGravity();
                disc.Update(dt);

                foreach (var obstacle in _obstaclesList)
                {
                    Vector2 collisionNormal = Vector2.Zero;
                    if (obstacle.CollidesWith(disc, ref collisionNormal))
                    {
                        disc.RevertToPreviousPosition();
                        Vector2 desiredVelocity = Vector2.Reflect(disc.Velocity, collisionNormal);
                        disc.ApplyImpulse(desiredVelocity - disc.Velocity, dt);
                    }
                }
            }

            // ── Disc vs disc collisions ───────────────────────────────────────
            for (int i = 0; i < _discsList.Count; i++)
            {
                for (int j = i + 1; j < _discsList.Count; j++)
                {
                    Disc a = _discsList[i];
                    Disc b = _discsList[j];

                    var ca = a.Shapes as Circle;
                    var cb = b.Shapes as Circle;
                    if (ca == null || cb == null) continue;

                    Vector2 delta = b.Position - a.Position;
                    float dist    = delta.Length();
                    float minDist = ca.Radius + cb.Radius;

                    if (dist <= 0f || dist >= minDist) continue;

                    Vector2 n = delta / dist;

                    // Positional correction
                    float penetration = minDist - dist;
                    a.Position = ca.Position = a.Position - n * (penetration * 0.5f);
                    b.Position = cb.Position = b.Position + n * (penetration * 0.5f);

                    // Elastic velocity exchange along normal
                    Vector2 va = a.Velocity;
                    Vector2 vb = b.Velocity;

                    Vector2 va_parallel = Vector2.Dot(va, n) * n;
                    Vector2 vb_parallel = Vector2.Dot(vb, n) * n;

                    a.Velocity = (va - va_parallel) + vb_parallel;
                    b.Velocity = (vb - vb_parallel) + va_parallel;
                }
            }

            // ── Orb vs disc collisions ────────────────────────────────────────
            for (int i = 0; i < _orbsList.Count; i++)
            {
                EnergyOrb orb = _orbsList[i];
                for (int j = 0; j < _discsList.Count; j++)
                {
                    Disc disc = _discsList[j];
                    Vector2 collisionNormal = Vector2.Zero;
                    if (orb.CollidesWith(disc, ref collisionNormal))
                    {
                        // Separate both objects to their pre-collision positions
                        orb.RevertToPreviousPosition();
                        disc.RevertToPreviousPosition();

                        // Reflect the orb off the disc surface
                        orb.Velocity = Vector2.Reflect(orb.Velocity, collisionNormal);

                        // collisionNormal points FROM orb TO disc (discCenter - orbCenter),
                        // so applying it to the disc pushes it AWAY from the orb (correct deflection)
                        disc.ApplyImpulse(collisionNormal * orb.Velocity.Length() * 1f, dt);
                    }
                }
            }

            // ── Disc boundary checks ──────────────────────────────────────────
            RemoveDiscs();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();

            if (_gameOver)
            {
                // ── Game Over screen ──────────────────────────────────────────
                string msg1 = "GAME OVER";
                string msg2 = $"Final Score: {_score}";
                string msg3 = "Press Escape to quit";

                Vector2 size1 = _font.MeasureString(msg1);
                Vector2 size2 = _font.MeasureString(msg2);
                Vector2 size3 = _font.MeasureString(msg3);

                int cx = GraphicsDevice.Viewport.Width  / 2;
                int cy = GraphicsDevice.Viewport.Height / 2;

                _spriteBatch.DrawString(_font, msg1, new Vector2(cx - size1.X / 2, cy - 60), Color.Red);
                _spriteBatch.DrawString(_font, msg2, new Vector2(cx - size2.X / 2, cy),      Color.White);
                _spriteBatch.DrawString(_font, msg3, new Vector2(cx - size3.X / 2, cy + 50), Color.LightGray);
            }
            else
            {
                // ── Player ship ───────────────────────────────────────────────
                _spriteBatch.Draw(_textureAtlas, _playerShip.Rectangle,
                    new Rectangle(0, 0, 256, 256), Color.White);

                // ── Energy orbs ───────────────────────────────────────────────
                foreach (EnergyOrb orb in _orbsList)
                    _spriteBatch.Draw(_textureAtlas, orb.Rectangle,
                        new Rectangle(512, 0, 256, 256), Color.OrangeRed);

                // ── Obstacles (circle or square) ──────────────────────────────
                foreach (ICollidable obstacle in _obstaclesList)
                {
                    if (obstacle.Shapes is Circle circle)
                    {
                        Rectangle r = new Rectangle(
                            (int)(circle.Centre.X - circle.Radius),
                            (int)(circle.Centre.Y - circle.Radius),
                            (int)circle.Radius * 2,
                            (int)circle.Radius * 2);
                        _spriteBatch.Draw(_textureAtlas, r,
                            new Rectangle(256, 0, 256, 256), Color.White);
                    }
                    else if (obstacle.Shapes is Square square)
                    {
                        Rectangle r = new Rectangle(
                            (int)square.Position.X,
                            (int)square.Position.Y,
                            (int)square.Size,
                            (int)square.Size);
                        _spriteBatch.Draw(_textureAtlas, r,
                            new Rectangle(256, 256, 256, 256), Color.White);
                    }
                }

                // ── Tonaltin (discs) ──────────────────────────────────────────
                foreach (Disc disc in _discsList)
                {
                    Circle circle = disc.Shapes as Circle;
                    if (circle == null) continue;
                    Rectangle r = new Rectangle(
                        (int)(disc.Position.X - circle.Radius),
                        (int)(disc.Position.Y - circle.Radius),
                        (int)circle.Radius * 2,
                        (int)circle.Radius * 2);
                    _spriteBatch.Draw(_textureAtlas, r,
                        new Rectangle(512, 0, 256, 256), Color.Gold);
                }

                // ── HUD ───────────────────────────────────────────────────────
                _spriteBatch.DrawString(_font, $"Score: {_score}", new Vector2(10, 10),  Color.White);
                _spriteBatch.DrawString(_font, $"Lives: {_lives}", new Vector2(10, 40),  Color.White);
            }

            _spriteBatch.End();
            base.Draw(gameTime);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Spawn a Tonaltin at a random X along the top of the screen.</summary>
        private void SpawnTonaltin()
        {
            int maxX = GraphicsDevice.Viewport.Width - Disc.DISC_RADIUS;
            float x  = _rng.Next(Disc.DISC_RADIUS, maxX);
            var disc = new Disc(new Vector2(x, -Disc.DISC_RADIUS), this);
            // Give it a gentle initial downward nudge
            disc.Velocity = new Vector2(0, SPAWN_SPEED);
            _discsList.Add(disc);
        }

        /// <summary>Remove orbs that have left the viewport.</summary>
        private void RemoveOrbs()
        {
            int vw = GraphicsDevice.Viewport.Width;
            int vh = GraphicsDevice.Viewport.Height;
            for (int i = _orbsList.Count - 1; i >= 0; --i)
            {
                var p = _orbsList[i].Position;
                if (p.X < 0 || p.X > vw || p.Y < 0 || p.Y > vh)
                {
                    Components.Remove(_orbsList[i]);
                    _orbsList.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Remove discs that have gone out of bounds.
        /// Side exits → +1 score. Bottom exit → −1 life (game over when 0).
        /// </summary>
        private void RemoveDiscs()
        {
            int vw = GraphicsDevice.Viewport.Width;
            int vh = GraphicsDevice.Viewport.Height;

            for (int i = _discsList.Count - 1; i >= 0; --i)
            {
                Disc disc = _discsList[i];
                Vector2 pos = disc.Position;

                bool offSide   = pos.X < -Disc.DISC_RADIUS || pos.X > vw + Disc.DISC_RADIUS;
                bool offBottom = pos.Y > vh + Disc.DISC_RADIUS;

                if (offSide)
                {
                    _score++;
                    _discsList.RemoveAt(i);
                }
                else if (offBottom)
                {
                    _lives--;
                    _discsList.RemoveAt(i);
                    if (_lives <= 0)
                        _gameOver = true;
                }
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PlayerShip
    // ═════════════════════════════════════════════════════════════════════════
    public class PlayerShip : GameComponent
    {
        private Rectangle _rectangle;
        public Rectangle Rectangle => _rectangle;
        private double _lastFireTime = 0;
        private const double FireCooldown = 0.1;

        public PlayerShip(Game game) : base(game)
        {
            _rectangle = new Rectangle(10, Game.GraphicsDevice.Viewport.Height - 10 - 75, 75, 75);
        }

        public void MoveSideways(int amount)
        {
            _rectangle.X += amount;
            if (_rectangle.X < 10) _rectangle.X = 10;
            if (_rectangle.X > Game.GraphicsDevice.Viewport.Width - 10 - _rectangle.Width)
                _rectangle.X = Game.GraphicsDevice.Viewport.Width - 10 - _rectangle.Width;
        }

        public EnergyOrb FireEnergyOrb(GameTime gameTime)
        {
            double currentTime = gameTime.TotalGameTime.TotalSeconds;
            if (currentTime - _lastFireTime < FireCooldown) return null;
            _lastFireTime = currentTime;
            return new EnergyOrb(
                new Vector2(_rectangle.Center.X, _rectangle.Center.Y - _rectangle.Height / 2),
                new Vector2(0, -200),
                Game);
        }

        public override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.A)) MoveSideways(-8);
            if (Keyboard.GetState().IsKeyDown(Keys.D)) MoveSideways(8);
            base.Update(gameTime);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // EnergyOrb
    // ═════════════════════════════════════════════════════════════════════════
    public class EnergyOrb : GameComponent, ICollidable
    {
        public Vector2 Position;
        public Vector2 Velocity;
        private Vector2 _previousPosition;
        const int ORB_RADIUS = 5;

        public Rectangle Rectangle => new Rectangle(
            (int)(Position.X - ORB_RADIUS), (int)(Position.Y - ORB_RADIUS),
            ORB_RADIUS * 2, ORB_RADIUS * 2);

        public Shape Shapes
        {
            get => new Circle(Position, ORB_RADIUS);
            set => throw new NotSupportedException("EnergyOrb.Shapes is derived from Position.");
        }

        public EnergyOrb(Vector2 position, Vector2 velocity, Game game) : base(game)
        {
            Position = position;
            Velocity = velocity;
            _previousPosition = position;
        }

        public override void Update(GameTime gameTime)
        {
            _previousPosition = Position;
            Position += (float)gameTime.ElapsedGameTime.TotalSeconds * Velocity;
            base.Update(gameTime);
        }

        public bool CollidesWith(ICollidable other)
        {
            if (other?.Shapes == null) return false;
            return Shapes.Intersects(other.Shapes);
        }

        public bool CollidesWith(ICollidable other, ref Vector2 collisionNormal)
        {
            if (other?.Shapes == null) return false;
            return Shapes.Intersects(other.Shapes, ref collisionNormal);
        }

        public void RevertToPreviousPosition() => Position = _previousPosition;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Obstacle  — shape-agnostic via double dispatch
    // ═════════════════════════════════════════════════════════════════════════
    public class Obstacle : ICollidable
    {
        public Shape Shapes { get; set; }

        public Obstacle(Shape shape)
        {
            Shapes = shape ?? throw new ArgumentNullException(nameof(shape));
        }

        // Uses double dispatch: Shape.Intersects handles all shape-pair combinations
        public bool CollidesWith(ICollidable other)
        {
            if (other?.Shapes == null || Shapes == null) return false;
            return Shapes.Intersects(other.Shapes);
        }

        public bool CollidesWith(ICollidable other, ref Vector2 collisionNormal)
        {
            if (other?.Shapes == null || Shapes == null) return false;
            return Shapes.Intersects(other.Shapes, ref collisionNormal);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Disc (Tonaltin)
    // ═════════════════════════════════════════════════════════════════════════
    public class Disc : PhysicsObject, ICollidable
    {
        public const int DISC_RADIUS = 16;
        public const int DISC_MASS   = 100;

        private readonly Circle _circle;

        public Disc(Vector2 position, Game game) : base(DISC_MASS, position, game)
        {
            _circle = new Circle(position, DISC_RADIUS);
        }

        public Shape Shapes => _circle;

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            _circle.Position = Position;
        }

        public bool CollidesWith(ICollidable other)
            => Shapes.Intersects(other.Shapes);

        public bool CollidesWith(ICollidable other, ref Vector2 collisionNormal)
            => Shapes.Intersects(other.Shapes, ref collisionNormal);
    }
}
