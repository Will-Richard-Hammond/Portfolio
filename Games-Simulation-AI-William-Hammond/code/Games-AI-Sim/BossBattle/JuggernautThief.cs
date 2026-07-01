using MGGameLibrary.Abstract;
using MGGameLibrary.Interfaces;
using MGGameLibrary.Shapes;
using MGGameLibrary.Steering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace BossBattle
{
    /// <summary>
    /// Heavy thief variant that ignores the relic and hunts the player instead.
    /// When it reaches the player, Game1 stuns the player and respawns this enemy
    /// at the safest available spawn point so it cannot sit on top of the player
    /// and chain-stun them forever.
    /// </summary>
    public sealed class JuggernautThief : PhysicsObject, ICollidable
    {
        public const float Radius = 21f;
        public const float MoveSpeed = 102f;

        public const float Mass = 5f;
        private const float MaxForce = 520f;
        private const float StunSeconds = 1.25f;
        private const float BoulderDeathDelaySeconds = 0.60f;
        private const float BoulderImpulseScale = 0.30f;
        private const float MinimumLethalBoulderSpeed = 45f;
        private const float MinimumBoulderImpulseSpeed = 110f;
        private const float MaximumBoulderImpulseSpeed = 500f;
        private const float DyingLinearDamping = 0.955f;
        private const float PathRefreshSeconds = 0.16f;
        private const float PathGoalRecalcDistance = 16f;
        private const float PathStartRecalcDistance = 30f;
        private const float PathWaypointRadius = 26f;
        private const float StuckRepathSeconds = 0.35f;
        private const float StuckMinimumProgress = 3f;
        public const int StunsBeforeRespawn = 3;

        public Circle Circle { get; }
        public Shape Shapes => Circle;
        public Vector2 Heading { get; private set; } = Vector2.UnitY;
        public Vector2 TargetPosition { get; private set; }
        public Vector2 PathTarget { get; private set; }
        public int CurrentPathNodeCount => _currentPath.Count;
        public bool IsDying => _deathTimer > 0f;
        public bool IsStunned => _stunTimer > 0f || IsDying;
        public int StunHits { get; private set; }
        public int Respawns { get; private set; }
        public string LastRespawnReason { get; private set; } = string.Empty;
        public string CurrentGroupName { get; private set; } = "Hunter";
        public string CurrentStateName { get; private set; } = "ChasePlayer";
        public float Speed { get; }
        public Vector2 LastSpawnPosition { get; private set; }

        private readonly List<Rectangle> _walls;
        private readonly Func<Vector2> _getPlayerPosition;
        private readonly Func<Vector2> _getRespawnPosition;
        private readonly Agent _proxy;
        private readonly MutableTarget _seekTarget;
        private readonly TruncatedSumSteeringBehaviour _steering;
        private readonly GridPathfinder _pathfinder;
        private readonly int _worldWidth;
        private readonly int _worldHeight;

        private List<Vector2> _currentPath = new();
        private Vector2 _lastPathGoal;
        private Vector2 _lastPathStart;
        private Vector2 _lastProgressPosition;
        private int _currentPathIndex;
        private float _pathRefreshTimer;
        private float _stuckTimer;
        private float _stunTimer;
        private float _deathTimer;
        private string _pendingDeathReason = string.Empty;

        private Texture2D _texture;
        private readonly Color _rimColour = new Color(105, 45, 145);
        private readonly Color _bodyColour = new Color(60, 25, 95);
        private readonly Color _eyeColour = new Color(255, 110, 90);

        public JuggernautThief(
            Vector2 position,
            Func<Vector2> getPlayerPosition,
            Func<Vector2> getRespawnPosition,
            IEnumerable<Rectangle> walls,
            Game game,
            float speed = MoveSpeed)
            : base(Mass, position, game)
        {
            _getPlayerPosition = getPlayerPosition;
            _getRespawnPosition = getRespawnPosition;
            _walls = new List<Rectangle>(walls);
            LastSpawnPosition = position;
            Speed = speed;

            Circle = new Circle(position, Radius);
            TargetPosition = getPlayerPosition();
            PathTarget = TargetPosition;
            _lastPathGoal = TargetPosition;
            _lastPathStart = position;
            _lastProgressPosition = position;

            GetWorldSizeFromWalls(_walls, out _worldWidth, out _worldHeight);
            _pathfinder = new GridPathfinder(_worldWidth, _worldHeight, 24, _walls, Radius + 3f);

            _seekTarget = new MutableTarget(TargetPosition);
            var seek = new SeekBehaviour(_seekTarget);
            _steering = new TruncatedSumSteeringBehaviour(
                new List<SteeringBehaviour> { seek },
                MaxForce);

            _proxy = new Agent(position, 0f, game, new NoOpBehaviour(), Speed);
        }

        public void Stun()
        {
            if (IsDying) return;

            StunHits++;

            if (StunHits >= StunsBeforeRespawn)
            {
                Respawn("Overloaded by 3 stun bolts");
                return;
            }

            _stunTimer = StunSeconds;
            CurrentGroupName = "Disabled";
            CurrentStateName = "Stunned";
            StopMoving();
        }

        public void HitByBoulder(Vector2 boulderPosition, Vector2 boulderVelocity, float boulderMass)
        {
            if (IsDying) return;

            _pendingDeathReason = "Launched by rolling boulder";
            LastRespawnReason = _pendingDeathReason;
            StunHits = 0;
            _stunTimer = 0f;
            CurrentGroupName = "Physics";
            CurrentStateName = "BoulderImpulse";

            TargetPosition = Position;
            PathTarget = Position;
            _currentPath.Clear();
            _currentPathIndex = 0;
            _pathRefreshTimer = 0f;
            _stuckTimer = 0f;

            Vector2 impactDirection = boulderVelocity;
            if (impactDirection.LengthSquared() < 1f)
                impactDirection = Position - boulderPosition;

            if (impactDirection.LengthSquared() < 0.001f)
                impactDirection = Heading.LengthSquared() > 0.001f ? Heading : Vector2.UnitY;

            impactDirection.Normalize();

            // The juggernaut has more mass than a normal thief, so the same
            // boulder momentum produces a smaller launch velocity.
            float boulderSpeed = MathF.Max(boulderVelocity.Length(), MinimumLethalBoulderSpeed);
            float impulseSpeed = boulderSpeed * (boulderMass / Mass) * BoulderImpulseScale;
            impulseSpeed = MathHelper.Clamp(
                impulseSpeed,
                MinimumBoulderImpulseSpeed,
                MaximumBoulderImpulseSpeed);

            Velocity = impactDirection * impulseSpeed;
            Heading = impactDirection;
            _deathTimer = BoulderDeathDelaySeconds;
        }

        public void Respawn(string reason)
        {
            _deathTimer = 0f;
            _pendingDeathReason = string.Empty;
            Respawns++;
            LastRespawnReason = reason;
            StunHits = 0;
            _stunTimer = 0f;

            Position = ClampInsideWorld(_getRespawnPosition());
            LastSpawnPosition = Position;
            Velocity = Vector2.Zero;
            Heading = Vector2.UnitY;
            Circle.Position = Position;

            TargetPosition = _getPlayerPosition();
            PathTarget = TargetPosition;
            _currentPath.Clear();
            _currentPathIndex = 0;
            _pathRefreshTimer = 0f;
            _lastPathStart = Position;
            _lastPathGoal = TargetPosition;
            _lastProgressPosition = Position;
            _stuckTimer = 0f;

            CurrentGroupName = "Hunter";
            CurrentStateName = "ChasePlayer";
        }

        public void StopMoving()
        {
            Velocity = Vector2.Zero;
        }

        public override void Update(float deltaTime)
        {
            if (IsDying)
            {
                _deathTimer -= deltaTime;

                base.Update(deltaTime);
                Velocity *= DyingLinearDamping;
                Circle.Position = Position;

                if (Velocity.LengthSquared() > 1f)
                    Heading = Vector2.Normalize(Velocity);

                if (CollidesWithAnyWall())
                {
                    RevertToPreviousPosition();
                    Circle.Position = Position;
                    Velocity *= -0.35f;
                }

                if (_deathTimer <= 0f)
                    Respawn(string.IsNullOrWhiteSpace(_pendingDeathReason)
                        ? "Crushed by rolling boulder"
                        : _pendingDeathReason);

                return;
            }

            if (_stunTimer > 0f)
            {
                _stunTimer -= deltaTime;
                if (_stunTimer < 0f)
                    _stunTimer = 0f;

                StopMoving();
                TargetPosition = Position;

                if (_stunTimer <= 0f)
                {
                    CurrentGroupName = "Hunter";
                    CurrentStateName = "ChasePlayer";
                }

                return;
            }

            TargetPosition = _getPlayerPosition();
            Vector2 steeringTarget = ResolvePathTarget(deltaTime, TargetPosition);
            bool shouldMove = Vector2.DistanceSquared(Position, steeringTarget) > 16f;

            if (shouldMove)
                ApplySteeringVelocity(steeringTarget);
            else
                Velocity = Vector2.Zero;

            base.Update(deltaTime);
            Circle.Position = Position;

            if (Velocity.LengthSquared() > 1f)
                Heading = Vector2.Normalize(Velocity);

            bool hitWall = CollidesWithAnyWall();
            if (hitWall)
            {
                RevertToPreviousPosition();
                Circle.Position = Position;
                Velocity = Vector2.Zero;
                ForceRepath(TargetPosition);
            }

            UpdateStuckRecovery(deltaTime, shouldMove && !hitWall);
        }

        public bool CollidesWith(ICollidable other)
        {
            if (other?.Shapes == null) return false;
            return Circle.Intersects(other.Shapes);
        }

        public bool CollidesWith(ICollidable other, ref Vector2 collisionNormal)
        {
            if (other?.Shapes == null) return false;
            return Circle.Intersects(other.Shapes, ref collisionNormal);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            EnsureTexture(spriteBatch.GraphicsDevice);

            Vector2 origin = new Vector2(_texture.Width / 2f, _texture.Height / 2f);
            float rimScale = (Radius * 2f) / _texture.Width;
            float bodyScale = (Radius * 2f - 7f) / _texture.Width;

            Color rim = IsDying ? new Color(255, 160, 90) :
                        IsStunned ? new Color(120, 120, 150) : _rimColour;
            Color body = IsDying ? new Color(95, 55, 35) :
                         IsStunned ? new Color(75, 75, 105) : _bodyColour;

            spriteBatch.Draw(_texture, Position, null, rim,
                0f, origin, rimScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(_texture, Position, null, body,
                0f, origin, bodyScale, SpriteEffects.None, 0f);

            Vector2 eyeOffset = Heading.LengthSquared() > 0.01f
                ? Vector2.Normalize(Heading) * Radius * 0.38f
                : Vector2.Zero;

            Rectangle eye = new Rectangle(
                (int)(Position.X + eyeOffset.X) - 4,
                (int)(Position.Y + eyeOffset.Y) - 4,
                8,
                8);

            spriteBatch.Draw(_texture, eye, IsStunned ? new Color(120, 120, 140) : _eyeColour);
        }

        private Vector2 ResolvePathTarget(float deltaTime, Vector2 goal)
        {
            goal = ClampInsideWorld(goal);
            _pathRefreshTimer -= deltaTime;

            if (HasClearPath(Position, goal))
            {
                _currentPath.Clear();
                _currentPathIndex = 0;
                PathTarget = goal;
                return PathTarget;
            }

            bool goalMoved = Vector2.DistanceSquared(goal, _lastPathGoal) >
                             PathGoalRecalcDistance * PathGoalRecalcDistance;
            bool startMoved = Vector2.DistanceSquared(Position, _lastPathStart) >
                              PathStartRecalcDistance * PathStartRecalcDistance;

            if (_currentPath.Count == 0 || goalMoved || startMoved || _pathRefreshTimer <= 0f)
            {
                _currentPath = _pathfinder.FindPath(Position, goal);
                _currentPathIndex = _currentPath.Count > 1 ? 1 : 0;
                _lastPathStart = Position;
                _lastPathGoal = goal;
                _pathRefreshTimer = PathRefreshSeconds;
            }

            while (_currentPath.Count > 0 &&
                   _currentPathIndex < _currentPath.Count - 1 &&
                   Vector2.DistanceSquared(Position, _currentPath[_currentPathIndex]) <
                   PathWaypointRadius * PathWaypointRadius)
            {
                _currentPathIndex++;
            }

            SkipVisiblePathNodes();

            if (_currentPath.Count == 0)
            {
                PathTarget = goal;
            }
            else
            {
                _currentPathIndex = Math.Clamp(_currentPathIndex, 0, _currentPath.Count - 1);
                PathTarget = _currentPath[_currentPathIndex];
            }

            return PathTarget;
        }

        private void ApplySteeringVelocity(Vector2 steeringTarget)
        {
            _seekTarget.TargetPosition = steeringTarget;

            _proxy.Position = Position;
            if (Velocity.LengthSquared() > 0.01f)
                _proxy.Heading = MathF.Atan2(Velocity.Y, Velocity.X) + MathF.PI / 2f;

            Vector2 force = _steering.CalculateSteeringForce(_proxy);
            Velocity = force.LengthSquared() > 0.01f
                ? Vector2.Normalize(force) * Speed
                : Vector2.Zero;
        }

        private void ForceRepath(Vector2 goal)
        {
            _currentPath = _pathfinder.FindPath(Position, ClampInsideWorld(goal));
            _currentPathIndex = _currentPath.Count > 1 ? 1 : 0;
            _lastPathStart = Position;
            _lastPathGoal = goal;
            _pathRefreshTimer = PathRefreshSeconds;
            _lastProgressPosition = Position;
            _stuckTimer = 0f;
            SkipVisiblePathNodes();
        }

        private void UpdateStuckRecovery(float deltaTime, bool shouldCheck)
        {
            if (!shouldCheck)
            {
                _stuckTimer = 0f;
                _lastProgressPosition = Position;
                return;
            }

            _stuckTimer += deltaTime;
            if (_stuckTimer < StuckRepathSeconds)
                return;

            float movedSq = Vector2.DistanceSquared(Position, _lastProgressPosition);
            if (movedSq < StuckMinimumProgress * StuckMinimumProgress)
            {
                ForceRepath(TargetPosition);

                if (_currentPath.Count > 0 && _currentPathIndex < _currentPath.Count - 1)
                    _currentPathIndex++;
            }

            _lastProgressPosition = Position;
            _stuckTimer = 0f;
        }

        private void SkipVisiblePathNodes()
        {
            while (_currentPath.Count > 0 &&
                   _currentPathIndex < _currentPath.Count - 1 &&
                   HasClearPath(Position, _currentPath[_currentPathIndex + 1]))
            {
                _currentPathIndex++;
            }
        }

        private bool CollidesWithAnyWall()
        {
            foreach (Rectangle wall in _walls)
                if (CircleIntersectsRect(Circle, wall))
                    return true;
            return false;
        }

        private Vector2 ClampInsideWorld(Vector2 point)
        {
            return new Vector2(
                MathHelper.Clamp(point.X, 40f, _worldWidth - 40f),
                MathHelper.Clamp(point.Y, 40f, _worldHeight - 40f));
        }

        private bool HasClearPath(Vector2 start, Vector2 end)
        {
            foreach (Rectangle wall in _walls)
            {
                if (IsBorderWall(wall)) continue;

                Rectangle inflated = InflateRectangle(wall, (int)MathF.Ceiling(Radius + 9f));
                if (LineIntersectsRect(start, end, inflated))
                    return false;
            }

            return true;
        }

        private static bool LineIntersectsRect(Vector2 a, Vector2 b, Rectangle rect)
        {
            if (rect.Contains((int)a.X, (int)a.Y) || rect.Contains((int)b.X, (int)b.Y)) return true;

            Vector2 topLeft = new Vector2(rect.Left, rect.Top);
            Vector2 topRight = new Vector2(rect.Right, rect.Top);
            Vector2 bottomLeft = new Vector2(rect.Left, rect.Bottom);
            Vector2 bottomRight = new Vector2(rect.Right, rect.Bottom);

            return LinesIntersect(a, b, topLeft, topRight) ||
                   LinesIntersect(a, b, topRight, bottomRight) ||
                   LinesIntersect(a, b, bottomRight, bottomLeft) ||
                   LinesIntersect(a, b, bottomLeft, topLeft);
        }

        private static bool LinesIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float denominator = ((d.Y - c.Y) * (b.X - a.X)) -
                                ((d.X - c.X) * (b.Y - a.Y));

            if (MathF.Abs(denominator) < 0.0001f)
                return false;

            float ua = (((d.X - c.X) * (a.Y - c.Y)) -
                        ((d.Y - c.Y) * (a.X - c.X))) / denominator;
            float ub = (((b.X - a.X) * (a.Y - c.Y)) -
                        ((b.Y - a.Y) * (a.X - c.X))) / denominator;

            return ua >= 0f && ua <= 1f && ub >= 0f && ub <= 1f;
        }

        private static bool CircleIntersectsRect(Circle circle, Rectangle rect)
        {
            float cx = MathHelper.Clamp(circle.Centre.X, rect.Left, rect.Right);
            float cy = MathHelper.Clamp(circle.Centre.Y, rect.Top, rect.Bottom);
            float dx = circle.Centre.X - cx;
            float dy = circle.Centre.Y - cy;
            return dx * dx + dy * dy <= circle.Radius * circle.Radius;
        }

        private static Rectangle InflateRectangle(Rectangle rect, int amount)
        {
            return new Rectangle(
                rect.X - amount,
                rect.Y - amount,
                rect.Width + amount * 2,
                rect.Height + amount * 2);
        }

        private static void GetWorldSizeFromWalls(IEnumerable<Rectangle> walls,
                                                  out int width, out int height)
        {
            width = 800;
            height = 600;

            foreach (Rectangle wall in walls)
            {
                width = Math.Max(width, wall.Right);
                height = Math.Max(height, wall.Bottom);
            }
        }

        private static bool IsBorderWall(Rectangle wall)
        {
            return wall.Width >= 700 || wall.Height >= 500;
        }

        private void EnsureTexture(GraphicsDevice device)
        {
            if (_texture != null) return;

            const int diameter = 64;
            _texture = new Texture2D(device, diameter, diameter);
            Color[] data = new Color[diameter * diameter];

            float r = diameter / 2f;
            Vector2 c = new Vector2(r, r);

            for (int py = 0; py < diameter; py++)
                for (int px = 0; px < diameter; px++)
                {
                    float dist = Vector2.Distance(new Vector2(px + 0.5f, py + 0.5f), c);
                    float alpha = MathHelper.Clamp(r - dist, 0f, 1f);
                    data[py * diameter + px] = new Color(alpha, alpha, alpha, alpha);
                }

            _texture.SetData(data);
        }

        private sealed class MutableTarget : ITargetable
        {
            public Vector2 TargetPosition { get; set; }
            public MutableTarget(Vector2 pos) => TargetPosition = pos;
        }

        private sealed class NoOpBehaviour : SteeringBehaviour
        {
            public override Vector2 CalculateSteeringForce(Agent agent) => Vector2.Zero;
        }
    }
}
