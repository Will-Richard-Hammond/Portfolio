using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace BossBattle
{
    public class Game1 : Game
    {
        private enum ScreenState
        {
            Title,
            Game,
            Pause,
            GameOver,
            Win
        }

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private SpriteFont _font;
        private Texture2D _pixel;
        private Texture2D _circle;

        private const int DesignScreenW = 800;
        private const int DesignScreenH = 600;
        private readonly int ScreenW;
        private readonly int ScreenH;

        private Vector2 _relicHome;
        private Vector2 _relicCentre;
        private const float RelicRadius = 14f;
        private const float RelicReturnSpeed = 85f;
        private float _relicPulse;
        private TempleThief _relicCarrier;

        private readonly List<Rectangle> _walls = new();

        private PlayerGuardian _player;
        private const float PlayerRadius = 16f;
        private const float PlayerSpeed = 220f;

        private readonly List<PhysicsBoulder> _boulders = new();
        private readonly List<TempleProjectile> _projectiles = new();
        private readonly List<TempleThief> _thieves = new();
        private readonly List<JuggernautThief> _juggernauts = new();
        private readonly List<Vector2> _juggernautSpawnPoints = new();

        private const float FireCooldown = 0.25f;
        private float _fireCooldownTimer = 0f;
        private const float ProjectileSpeed = 500f;
        private const float BoulderKillSpeed = 45f;

        private const float RoundDuration = 60f;
        private float _roundTimer = RoundDuration;

        private ScreenState _screenState = ScreenState.Title;
        private string _endMessage = string.Empty;

        private MouseState _prevMouse;
        private KeyboardState _prevKeyboard;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);

            DisplayMode display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            ScreenW = Math.Max(DesignScreenW, display.Width);
            ScreenH = Math.Max(DesignScreenH, display.Height);

            _graphics.PreferredBackBufferWidth = ScreenW;
            _graphics.PreferredBackBufferHeight = ScreenH;
            _graphics.IsFullScreen = true;
            _graphics.HardwareModeSwitch = false;

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            ResetGame();
            base.Initialize();
        }

        private void ResetGame()
        {
            _walls.Clear();
            _boulders.Clear();
            _projectiles.Clear();
            _thieves.Clear();
            _juggernauts.Clear();
            _juggernautSpawnPoints.Clear();

            _relicHome = new Vector2(ScreenW / 2f, ScreenH / 2f);
            _relicCentre = _relicHome;
            _relicCarrier = null;
            _relicPulse = 0f;
            _roundTimer = RoundDuration;
            _fireCooldownTimer = 0f;
            _endMessage = string.Empty;

            BuildTempleRoom();
            BuildJuggernautSpawnPoints();

            _player = new PlayerGuardian(
                ScalePoint(400f, 516f),
                PlayerRadius, PlayerSpeed, _walls, this);

            // Boulders sit in open lanes. Fewer boulders keeps the level readable
            // while still giving the player physics hazards to knock around.
            SpawnBoulder(ScalePoint(175f, 300f));
            SpawnBoulder(ScalePoint(625f, 300f));
            SpawnBoulder(ScalePoint(400f, 165f));
            SpawnBoulder(ScalePoint(400f, 435f));

            SpawnThieves();
            SpawnJuggernauts();
        }

        private void SpawnBoulder(Vector2 position)
        {
            _boulders.Add(new PhysicsBoulder(position, _walls, this));
        }

        private void SpawnThieves()
        {
            AddThief(
                ScalePoint(72f, 72f),
                ScalePoint(44f, 72f),
                new List<Vector2>
                {
                    ScalePoint(72f, 170f),
                    ScalePoint(220f, 250f),
                    ScalePoint(350f, 300f),
                    ScalePoint(400f, 300f),
                },
                112f);

            AddThief(
                ScalePoint(728f, 72f),
                ScalePoint(756f, 72f),
                new List<Vector2>
                {
                    ScalePoint(728f, 170f),
                    ScalePoint(580f, 250f),
                    ScalePoint(450f, 300f),
                    ScalePoint(400f, 300f),
                },
                124f);

            AddThief(
                ScalePoint(72f, 528f),
                ScalePoint(44f, 528f),
                new List<Vector2>
                {
                    ScalePoint(72f, 430f),
                    ScalePoint(220f, 350f),
                    ScalePoint(350f, 300f),
                    ScalePoint(400f, 300f),
                },
                132f);

            AddThief(
                ScalePoint(728f, 528f),
                ScalePoint(756f, 528f),
                new List<Vector2>
                {
                    ScalePoint(728f, 430f),
                    ScalePoint(580f, 350f),
                    ScalePoint(450f, 300f),
                    ScalePoint(400f, 300f),
                },
                118f);
        }

        private void SpawnJuggernauts()
        {
            Vector2 spawn = GetBestJuggernautSpawnPosition();

            _juggernauts.Add(new JuggernautThief(
                spawn,
                () => _player.Position,
                GetBestJuggernautSpawnPosition,
                _walls,
                this,
                102f));
        }

        private void AddThief(Vector2 spawn, Vector2 exit, IReadOnlyList<Vector2> patrolPath, float speed)
        {
            _thieves.Add(new TempleThief(
                spawn,
                exit,
                patrolPath,
                () => _relicCentre,
                () => _relicCarrier == null,
                () => _player.Position,
                TryStealRelic,
                _walls,
                this,
                speed));
        }

        private bool TryStealRelic(TempleThief thief)
        {
            if (_screenState != ScreenState.Game) return false;
            if (_relicCarrier != null) return false;

            _relicCarrier = thief;
            thief.HasRelic = true;
            _relicCentre = thief.Position;
            return true;
        }

        private void DropRelic(TempleThief thief)
        {
            if (_relicCarrier != thief) return;

            thief.HasRelic = false;
            _relicCarrier = null;
            _relicCentre = KeepInsideRoom(thief.Position);
        }

        private Vector2 KeepInsideRoom(Vector2 point)
        {
            return new Vector2(
                MathHelper.Clamp(point.X, 40f, ScreenW - 40f),
                MathHelper.Clamp(point.Y, 40f, ScreenH - 40f));
        }

        private Vector2 ScalePoint(float x, float y)
        {
            return new Vector2(
                x * ScreenW / DesignScreenW,
                y * ScreenH / DesignScreenH);
        }

        private Rectangle ScaleRect(int x, int y, int width, int height)
        {
            int scaledX = (int)MathF.Round(x * ScreenW / (float)DesignScreenW);
            int scaledY = (int)MathF.Round(y * ScreenH / (float)DesignScreenH);
            int scaledW = Math.Max(1, (int)MathF.Round(width * ScreenW / (float)DesignScreenW));
            int scaledH = Math.Max(1, (int)MathF.Round(height * ScreenH / (float)DesignScreenH));

            return new Rectangle(scaledX, scaledY, scaledW, scaledH);
        }

        private void BuildTempleRoom()
        {
            const int t = 24;

            // Border walls
            _walls.Add(new Rectangle(0, 0, ScreenW, t));
            _walls.Add(new Rectangle(0, ScreenH - t, ScreenW, t));
            _walls.Add(new Rectangle(0, 0, t, ScreenH));
            _walls.Add(new Rectangle(ScreenW - t, 0, t, ScreenH));

            // Lower-density corridor walls. The shorter segments leave wider
            // gaps so the A* path nodes do not force thieves to skim the walls.
            _walls.Add(ScaleRect(125, 112, 175, 16));
            _walls.Add(ScaleRect(500, 112, 175, 16));
            _walls.Add(ScaleRect(130, 195, 110, 16));
            _walls.Add(ScaleRect(560, 195, 110, 16));

            _walls.Add(ScaleRect(130, 389, 110, 16));
            _walls.Add(ScaleRect(560, 389, 110, 16));
            _walls.Add(ScaleRect(125, 472, 175, 16));
            _walls.Add(ScaleRect(500, 472, 175, 16));

            // Central relic chamber hints at a room without closing it in.
            // The vertical side walls were removed because they made the thieves
            // clip against the entrances too often.
            _walls.Add(ScaleRect(318, 240, 54, 14));
            _walls.Add(ScaleRect(428, 240, 54, 14));
            _walls.Add(ScaleRect(318, 346, 54, 14));
            _walls.Add(ScaleRect(428, 346, 54, 14));

            // Smaller pillars keep route choices without overcrowding the lanes.
            const int s = 24;
            _walls.Add(ScaleRect(252, 288, s, s));
            _walls.Add(ScaleRect(524, 288, s, s));
        }

        private void BuildJuggernautSpawnPoints()
        {
            // The juggernaut no longer has one fixed spawn. On every respawn it
            // chooses the valid point furthest from the player, which prevents
            // repeated stun-locking when the player happens to be near the old
            // fixed top-middle spawn.
            _juggernautSpawnPoints.Clear();
            _juggernautSpawnPoints.Add(ScalePoint(72f, 72f));
            _juggernautSpawnPoints.Add(ScalePoint(728f, 72f));
            _juggernautSpawnPoints.Add(ScalePoint(72f, 528f));
            _juggernautSpawnPoints.Add(ScalePoint(728f, 528f));
            _juggernautSpawnPoints.Add(ScalePoint(400f, 72f));
            _juggernautSpawnPoints.Add(ScalePoint(400f, 528f));
            _juggernautSpawnPoints.Add(ScalePoint(72f, 300f));
            _juggernautSpawnPoints.Add(ScalePoint(728f, 300f));
        }

        private Vector2 GetBestJuggernautSpawnPosition()
        {
            Vector2 playerPosition = _player?.Position ?? new Vector2(ScreenW * 0.5f, ScreenH * 0.86f);
            Vector2 bestSpawn = ScalePoint(400f, 72f);
            float bestDistanceSquared = float.MinValue;

            foreach (Vector2 spawn in _juggernautSpawnPoints)
            {
                if (!IsCircleSpawnClear(spawn, JuggernautThief.Radius + 3f))
                    continue;

                float distanceSquared = Vector2.DistanceSquared(spawn, playerPosition);
                if (distanceSquared > bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    bestSpawn = spawn;
                }
            }

            return bestSpawn;
        }

        private bool IsCircleSpawnClear(Vector2 centre, float radius)
        {
            if (centre.X - radius < 24f || centre.X + radius > ScreenW - 24f ||
                centre.Y - radius < 24f || centre.Y + radius > ScreenH - 24f)
                return false;

            foreach (Rectangle wall in _walls)
            {
                if (CircleIntersectsRect(centre, radius, wall))
                    return false;
            }

            foreach (PhysicsBoulder boulder in _boulders)
            {
                float minimumDistance = radius + PhysicsBoulder.Radius + 8f;
                if (Vector2.DistanceSquared(centre, boulder.Position) < minimumDistance * minimumDistance)
                    return false;
            }

            return true;
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _font = Content.Load<SpriteFont>("GameFont");

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _circle = CreateCircleTexture(128);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboard = Keyboard.GetState();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                WasKeyPressed(Keys.Q, keyboard))
                Exit();

            switch (_screenState)
            {
                case ScreenState.Title:
                    if (WasKeyPressed(Keys.Enter, keyboard))
                    {
                        ResetGame();
                        _screenState = ScreenState.Game;
                    }
                    break;

                case ScreenState.Game:
                    if (WasKeyPressed(Keys.Escape, keyboard))
                    {
                        _screenState = ScreenState.Pause;
                    }
                    else
                    {
                        UpdateGameplay(gameTime);
                    }
                    break;

                case ScreenState.Pause:
                    if (WasKeyPressed(Keys.Enter, keyboard) || WasKeyPressed(Keys.Escape, keyboard))
                        _screenState = ScreenState.Game;
                    break;

                case ScreenState.GameOver:
                case ScreenState.Win:
                    if (WasKeyPressed(Keys.Enter, keyboard))
                    {
                        ResetGame();
                        _screenState = ScreenState.Game;
                    }
                    else if (WasKeyPressed(Keys.Escape, keyboard))
                    {
                        _screenState = ScreenState.Title;
                    }
                    break;
            }

            _prevMouse = Mouse.GetState();
            _prevKeyboard = keyboard;

            base.Update(gameTime);
        }

        private bool WasKeyPressed(Keys key, KeyboardState current)
        {
            return current.IsKeyDown(key) && !_prevKeyboard.IsKeyDown(key);
        }

        private void UpdateGameplay(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _relicPulse += dt;
            _roundTimer = MathF.Max(0f, _roundTimer - dt);

            _player.Update(gameTime);

            if (_fireCooldownTimer > 0f)
                _fireCooldownTimer -= dt;

            HandleFiring();

            foreach (var proj in _projectiles)
                proj.Update(dt);

            foreach (var boulder in _boulders)
                boulder.Update(dt);

            foreach (var thief in _thieves)
                thief.Update(dt);

            foreach (var juggernaut in _juggernauts)
                juggernaut.Update(dt);

            HandleProjectileWallCollisions();
            HandleProjectileBoulderCollisions();
            HandleProjectileThiefCollisions();
            HandleProjectileJuggernautCollisions();
            HandleBoulderBoulderCollisions();
            HandleBoulderThiefCollisions();
            HandleBoulderJuggernautCollisions();
            HandleJuggernautPlayerCollisions();

            _projectiles.RemoveAll(p => !p.IsActive);

            UpdateRelicPosition(dt);
            CheckEndConditions();
        }

        private void UpdateRelicPosition(float deltaTime)
        {
            if (_relicCarrier != null)
            {
                _relicCentre = _relicCarrier.Position + new Vector2(0f, -22f);
                return;
            }

            Vector2 toHome = _relicHome - _relicCentre;
            float distance = toHome.Length();
            float step = RelicReturnSpeed * deltaTime;

            if (distance <= step || distance <= 0.001f)
            {
                _relicCentre = _relicHome;
                return;
            }

            _relicCentre += Vector2.Normalize(toHome) * step;
        }

        private void CheckEndConditions()
        {
            foreach (var thief in _thieves)
            {
                if (thief.HasRelic && thief.HasReachedExit)
                {
                    _endMessage = "A thief escaped with the relic.";
                    _screenState = ScreenState.GameOver;
                    return;
                }
            }

            if (_roundTimer <= 0f && _relicCarrier == null)
            {
                _endMessage = "You protected the relic until sunrise.";
                _screenState = ScreenState.Win;
            }
        }

        private void HandleFiring()
        {
            if (_fireCooldownTimer > 0f || _player.IsStunned) return;

            MouseState ms = Mouse.GetState();
            KeyboardState ks = Keyboard.GetState();

            bool mouseClicked = ms.LeftButton == ButtonState.Pressed &&
                                _prevMouse.LeftButton == ButtonState.Released;
            bool spacePressed = ks.IsKeyDown(Keys.Space) &&
                                !_prevKeyboard.IsKeyDown(Keys.Space);

            if (!mouseClicked && !spacePressed) return;

            Vector2 direction = GetDirectionFromPlayerToMouse(ms);

            Vector2 spawnPos = _player.Position +
                               direction * (PlayerRadius + TempleProjectile.Radius + 2f);

            var proj = new TempleProjectile(
                spawnPos,
                direction * ProjectileSpeed,
                ScreenW, ScreenH, this);

            _projectiles.Add(proj);
            _fireCooldownTimer = FireCooldown;
        }

        private Vector2 GetDirectionFromPlayerToMouse(MouseState mouseState)
        {
            Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);
            Vector2 direction = mousePos - _player.Position;

            if (direction.LengthSquared() <= 0.0001f)
                return _player.FacingDirection;

            direction.Normalize();
            return direction;
        }

        private void HandleProjectileWallCollisions()
        {
            foreach (var proj in _projectiles)
            {
                if (!proj.IsActive) continue;

                foreach (Rectangle wall in _walls)
                {
                    if (CircleIntersectsRect(proj.Position, TempleProjectile.Radius, wall))
                    {
                        proj.Deactivate();
                        break;
                    }
                }
            }
        }

        private void HandleProjectileBoulderCollisions()
        {
            foreach (var proj in _projectiles)
            {
                if (!proj.IsActive) continue;

                foreach (var boulder in _boulders)
                {
                    if (!proj.Circle.IntersectsCircle(boulder.Circle)) continue;

                    Vector2 pushDir = proj.Velocity;
                    if (pushDir == Vector2.Zero)
                        pushDir = boulder.Position - proj.Position;

                    if (pushDir != Vector2.Zero)
                        pushDir = Vector2.Normalize(pushDir);

                    const float KickScale = 42f;
                    float kickSpeed = proj.Velocity.Length()
                                      * (TempleProjectile.Mass / PhysicsBoulder.Mass)
                                      * KickScale;
                    boulder.Velocity += pushDir * kickSpeed;

                    proj.Deactivate();
                    break;
                }
            }
        }

        private void HandleProjectileThiefCollisions()
        {
            foreach (var proj in _projectiles)
            {
                if (!proj.IsActive) continue;

                foreach (var thief in _thieves)
                {
                    if (!proj.Circle.IntersectsCircle(thief.Circle)) continue;

                    proj.Deactivate();

                    if (thief.HasRelic)
                        DropRelic(thief);

                    thief.Stun();
                    break;
                }
            }
        }

        private void HandleProjectileJuggernautCollisions()
        {
            foreach (var proj in _projectiles)
            {
                if (!proj.IsActive) continue;

                foreach (var juggernaut in _juggernauts)
                {
                    if (!proj.Circle.IntersectsCircle(juggernaut.Circle)) continue;

                    proj.Deactivate();
                    juggernaut.Stun();
                    break;
                }
            }
        }

        private void HandleJuggernautPlayerCollisions()
        {
            foreach (var juggernaut in _juggernauts)
            {
                if (juggernaut.IsStunned) continue;

                if (!juggernaut.Circle.IntersectsCircle(_player.Shape)) continue;

                _player.Stun(1.35f);
                juggernaut.Respawn("Stunned the player");
            }
        }

        private void HandleBoulderThiefCollisions()
        {
            foreach (var boulder in _boulders)
            {
                // Only rolling boulders are lethal. This stops thieves instantly
                // dying by brushing against a completely still obstacle.
                if (boulder.Velocity.LengthSquared() < BoulderKillSpeed * BoulderKillSpeed)
                    continue;

                foreach (var thief in _thieves)
                {
                    if (thief.IsDying) continue;
                    if (!boulder.Circle.IntersectsCircle(thief.Circle)) continue;

                    if (thief.HasRelic)
                        DropRelic(thief);

                    thief.HitByBoulder(boulder.Position, boulder.Velocity, PhysicsBoulder.Mass);
                    boulder.Velocity *= 0.82f;
                }
            }
        }

        private void HandleBoulderJuggernautCollisions()
        {
            foreach (var boulder in _boulders)
            {
                if (boulder.Velocity.LengthSquared() < BoulderKillSpeed * BoulderKillSpeed)
                    continue;

                foreach (var juggernaut in _juggernauts)
                {
                    if (juggernaut.IsDying) continue;
                    if (!boulder.Circle.IntersectsCircle(juggernaut.Circle)) continue;

                    juggernaut.HitByBoulder(boulder.Position, boulder.Velocity, PhysicsBoulder.Mass);
                    boulder.Velocity *= 0.68f;
                }
            }
        }

        private void HandleBoulderBoulderCollisions()
        {
            for (int i = 0; i < _boulders.Count - 1; i++)
            {
                for (int j = i + 1; j < _boulders.Count; j++)
                {
                    PhysicsBoulder a = _boulders[i];
                    PhysicsBoulder b = _boulders[j];

                    Vector2 delta = b.Position - a.Position;
                    float dist = delta.Length();
                    float minDist = PhysicsBoulder.Radius * 2f;

                    if (dist >= minDist || dist < 1e-6f) continue;

                    Vector2 normal = delta / dist;

                    float overlap = (minDist - dist) * 0.5f;
                    a.Position -= normal * overlap;
                    b.Position += normal * overlap;
                    a.Circle.Position = a.Position;
                    b.Circle.Position = b.Position;

                    float aN = Vector2.Dot(a.Velocity, normal);
                    float bN = Vector2.Dot(b.Velocity, normal);

                    if (aN - bN <= 0f) continue;

                    a.Velocity += (bN - aN) * normal;
                    b.Velocity += (aN - bN) * normal;
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(40, 34, 28));

            _spriteBatch.Begin();

            DrawFloor();
            DrawExitZones();
            DrawWalls();
            DrawRelic();

            foreach (var boulder in _boulders)
                boulder.Draw(_spriteBatch);

            _player.Draw(_spriteBatch);

            foreach (var thief in _thieves)
                thief.Draw(_spriteBatch);

            foreach (var juggernaut in _juggernauts)
                juggernaut.Draw(_spriteBatch);

            DrawThiefPathTargets();

            foreach (var proj in _projectiles)
                proj.Draw(_spriteBatch);

            DrawDebugText();
            DrawScreenOverlay();

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawFloor()
        {
            Color line = new Color(55, 47, 38);
            const int tile = 40;

            for (int x = 0; x <= ScreenW; x += tile)
                _spriteBatch.Draw(_pixel, new Rectangle(x, 0, 1, ScreenH), line);

            for (int y = 0; y <= ScreenH; y += tile)
                _spriteBatch.Draw(_pixel, new Rectangle(0, y, ScreenW, 1), line);
        }

        private void DrawExitZones()
        {
            Color exitFill = new Color(60, 95, 65) * 0.75f;
            Color exitEdge = new Color(120, 180, 120);

            foreach (var thief in _thieves)
            {
                Rectangle zone = new Rectangle(
                    (int)thief.ExitPosition.X - 14,
                    (int)thief.ExitPosition.Y - 14,
                    28,
                    28);

                _spriteBatch.Draw(_pixel, zone, exitFill);
                _spriteBatch.Draw(_pixel, new Rectangle(zone.X, zone.Y, zone.Width, 2), exitEdge);
                _spriteBatch.Draw(_pixel, new Rectangle(zone.X, zone.Bottom - 2, zone.Width, 2), exitEdge);
                _spriteBatch.Draw(_pixel, new Rectangle(zone.X, zone.Y, 2, zone.Height), exitEdge);
                _spriteBatch.Draw(_pixel, new Rectangle(zone.Right - 2, zone.Y, 2, zone.Height), exitEdge);
            }
        }

        private void DrawWalls()
        {
            Color stone = new Color(90, 78, 62);
            Color edge = new Color(120, 105, 84);

            foreach (Rectangle w in _walls)
            {
                _spriteBatch.Draw(_pixel, w, stone);
                _spriteBatch.Draw(_pixel, new Rectangle(w.X, w.Y, w.Width, 3), edge);
            }
        }

        private void DrawRelic()
        {
            float pulse = 0.5f + 0.5f * MathF.Sin(_relicPulse * 2f);
            float radius = RelicRadius + pulse * 2.5f;

            Vector2 origin = new Vector2(_circle.Width / 2f, _circle.Height / 2f);

            float glowScale = (radius * 2.4f) / _circle.Width;
            _spriteBatch.Draw(_circle, _relicCentre, null,
                new Color(255, 215, 90) * (0.20f + 0.15f * pulse),
                0f, origin, glowScale, SpriteEffects.None, 0f);

            float bodyScale = (radius * 2f) / _circle.Width;
            _spriteBatch.Draw(_circle, _relicCentre, null,
                new Color(235, 195, 70),
                0f, origin, bodyScale, SpriteEffects.None, 0f);

            float coreScale = (radius * 1.1f) / _circle.Width;
            _spriteBatch.Draw(_circle, _relicCentre - new Vector2(radius * 0.25f, radius * 0.25f), null,
                new Color(255, 240, 180) * 0.8f,
                0f, origin, coreScale, SpriteEffects.None, 0f);
        }

        private void DrawThiefPathTargets()
        {
            Color pathColour = new Color(90, 180, 220) * 0.7f;

            foreach (var thief in _thieves)
            {
                if (thief.CurrentPathNodeCount <= 0) continue;

                Rectangle marker = new Rectangle(
                    (int)thief.PathTarget.X - 4,
                    (int)thief.PathTarget.Y - 4,
                    8,
                    8);

                _spriteBatch.Draw(_pixel, marker, pathColour);
            }

            Color juggernautPathColour = new Color(180, 90, 230) * 0.75f;
            foreach (var juggernaut in _juggernauts)
            {
                if (juggernaut.CurrentPathNodeCount <= 0) continue;

                Rectangle marker = new Rectangle(
                    (int)juggernaut.PathTarget.X - 5,
                    (int)juggernaut.PathTarget.Y - 5,
                    10,
                    10);

                _spriteBatch.Draw(_pixel, marker, juggernautPathColour);
            }
        }

        private void DrawDebugText()
        {
            if (_font == null) return;

            _spriteBatch.DrawString(_font, "Temple Relic Defence",
                new Vector2(40, 28), new Color(230, 215, 170),
                0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);

            _spriteBatch.DrawString(_font,
                "WASD: move   LMB / Space: fire bolt   Avoid the juggernaut   ESC: pause   Q: quit",
                new Vector2(40, 58), new Color(170, 160, 140),
                0f, Vector2.Zero, 0.4f, SpriteEffects.None, 0f);

            string relicStatus = _relicCarrier == null
                ? "Relic: dropped / available"
                : "Relic: carried by thief";

            string playerStatus = _player.IsStunned
                ? $"Player stunned: {_player.StunTimeRemaining:0.0}s"
                : "Player active";

            _spriteBatch.DrawString(_font,
                $"Timer: {_roundTimer:0.0}s   Projectiles: {_projectiles.Count}   {playerStatus}   {relicStatus}",
                new Vector2(40, 80), new Color(145, 180, 145),
                0f, Vector2.Zero, 0.4f, SpriteEffects.None, 0f);

            for (int i = 0; i < _thieves.Count; i++)
            {
                TempleThief thief = _thieves[i];
                _spriteBatch.DrawString(_font,
                    $"Thief {i + 1}: {thief.CurrentGroupName} / {thief.CurrentStateName}  Path nodes: {thief.CurrentPathNodeCount}  Stuns: {thief.StunHits}/3  Deaths: {thief.Deaths}" +
                    (thief.HasRelic ? " / HAS RELIC" : string.Empty),
                    new Vector2(40, 102 + i * 18), new Color(220, 140, 100),
                    0f, Vector2.Zero, 0.38f, SpriteEffects.None, 0f);
            }

            for (int i = 0; i < _juggernauts.Count; i++)
            {
                JuggernautThief juggernaut = _juggernauts[i];
                _spriteBatch.DrawString(_font,
                    $"Juggernaut {i + 1}: {juggernaut.CurrentGroupName} / {juggernaut.CurrentStateName}  Path nodes: {juggernaut.CurrentPathNodeCount}  Stuns: {juggernaut.StunHits}/3  Respawns: {juggernaut.Respawns}",
                    new Vector2(40, 102 + (_thieves.Count + i) * 18), new Color(185, 120, 230),
                    0f, Vector2.Zero, 0.38f, SpriteEffects.None, 0f);
            }
        }

        private void DrawScreenOverlay()
        {
            if (_font == null) return;

            if (_screenState == ScreenState.Game)
                return;

            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenW, ScreenH),
                new Color(0, 0, 0) * 0.58f);

            switch (_screenState)
            {
                case ScreenState.Title:
                    DrawCentredText("TEMPLE RELIC DEFENCE", 205f, 0.82f, new Color(255, 220, 120));
                    DrawCentredText("Protect the relic from the AI thieves", 270f, 0.48f, new Color(230, 220, 190));
                    DrawCentredText("Press ENTER to start", 330f, 0.45f, new Color(170, 220, 170));
                    DrawCentredText("Press Q to quit", 372f, 0.38f, new Color(170, 160, 140));
                    break;

                case ScreenState.Pause:
                    DrawCentredText("PAUSED", 245f, 0.8f, new Color(255, 220, 120));
                    DrawCentredText("Press ENTER or ESC to resume", 315f, 0.45f, new Color(230, 220, 190));
                    DrawCentredText("Press Q to quit", 357f, 0.38f, new Color(170, 160, 140));
                    break;

                case ScreenState.GameOver:
                    DrawCentredText("GAME OVER", 225f, 0.8f, new Color(255, 120, 100));
                    DrawCentredText(_endMessage, 290f, 0.45f, new Color(230, 220, 190));
                    DrawCentredText("ENTER: retry   ESC: title   Q: quit", 345f, 0.42f, new Color(170, 220, 170));
                    break;

                case ScreenState.Win:
                    DrawCentredText("YOU WIN", 225f, 0.8f, new Color(140, 230, 150));
                    DrawCentredText(_endMessage, 290f, 0.45f, new Color(230, 220, 190));
                    DrawCentredText("ENTER: play again   ESC: title   Q: quit", 345f, 0.42f, new Color(170, 220, 170));
                    break;
            }
        }

        private void DrawCentredText(string text, float y, float scale, Color colour)
        {
            Vector2 size = _font.MeasureString(text) * scale;
            Vector2 pos = new Vector2((ScreenW - size.X) * 0.5f, y);
            _spriteBatch.DrawString(_font, text, pos, colour,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private static bool CircleIntersectsRect(Vector2 centre, float radius, Rectangle rect)
        {
            float cx = MathHelper.Clamp(centre.X, rect.Left, rect.Right);
            float cy = MathHelper.Clamp(centre.Y, rect.Top, rect.Bottom);
            float dx = centre.X - cx;
            float dy = centre.Y - cy;
            return dx * dx + dy * dy <= radius * radius;
        }

        private Texture2D CreateCircleTexture(int diameter)
        {
            Texture2D tex = new Texture2D(GraphicsDevice, diameter, diameter);
            Color[] data = new Color[diameter * diameter];
            float r = diameter / 2f;
            Vector2 c = new Vector2(r, r);

            for (int y = 0; y < diameter; y++)
                for (int x = 0; x < diameter; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                    float a = MathHelper.Clamp(r - dist, 0f, 1f);
                    data[y * diameter + x] = new Color(a, a, a, a);
                }

            tex.SetData(data);
            return tex;
        }
    }
}
