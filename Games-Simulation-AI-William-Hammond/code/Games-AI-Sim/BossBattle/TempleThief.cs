using MGGameLibrary.Abstract;
using MGGameLibrary.Graphs;
using MGGameLibrary.Interfaces;
using MGGameLibrary.Shapes;
using MGGameLibrary.Steering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace BossBattle
{
    public class TempleThief : PhysicsObject, ICollidable
    {
        public const float Radius = 14f;
        public const float MoveSpeed = 120f;
        public const float Mass = 2f;
        private const float MaxForce = 400f;
        private const float RelicViewRange = 520f;
        private const float StealRange = 26f;
        private const float PlayerThreatRange = 86f;
        private const float ExitRange = 28f;
        private const float StunSeconds = 2f;
        private const float BoulderDeathDelaySeconds = 0.45f;
        private const float BoulderImpulseScale = 0.30f;
        private const float MinimumLethalBoulderSpeed = 45f;
        private const float MinimumBoulderImpulseSpeed = 160f;
        private const float MaximumBoulderImpulseSpeed = 780f;
        private const float DyingLinearDamping = 0.94f;
        public const int StunsBeforeRespawn = 3;

        public Circle Circle { get; }
        public Shape Shapes => Circle;
        public ThiefAgent AgentData { get; } = new();

        public Vector2 Heading { get; private set; } = Vector2.UnitX;
        public string CurrentStateName { get; private set; } = "Patrol";
        public string CurrentGroupName { get; private set; } = "Searching";
        public Vector2 TargetPosition { get; set; }
        public Vector2 ExitPosition { get; }
        public float Speed { get; }
        public int StunHits { get; private set; }
        public int Deaths { get; private set; }
        public string LastRespawnReason { get; private set; } = string.Empty;

        public bool HasRelic
        {
            get => AgentData.HasRelic;
            set => AgentData.HasRelic = value;
        }

        public bool HasReachedExit => AgentData.HasReachedExit;
        public bool IsStunned => AgentData.IsStunned;
        public bool IsDying => _deathTimer > 0f;

        private readonly Agent _proxy;
        private readonly MutableTarget _seekTarget;
        private readonly TruncatedSumSteeringBehaviour _steering;
        private readonly List<Rectangle> _walls;
        private readonly Func<Vector2> _getRelicPosition;
        private readonly Func<bool> _isRelicAvailable;
        private readonly Func<Vector2> _getPlayerPosition;
        private readonly Func<TempleThief, bool> _tryStealRelic;
        private readonly Vector2 _spawnPosition;
        private readonly List<Vector2> _patrolWaypoints;
        private readonly GridPathfinder _pathfinder;
        private readonly int _worldWidth;
        private readonly int _worldHeight;
        private List<Vector2> _currentPath = new();
        private Vector2 _lastPathGoal;
        private Vector2 _lastPathStart;
        private int _currentPathIndex;
        private float _pathRefreshTimer;
        private const float PathRefreshSeconds = 0.20f;
        private const float PathGoalRecalcDistance = 20f;
        private const float PathStartRecalcDistance = 30f;
        private const float PathWaypointRadius = 22f;
        public Vector2 PathTarget { get; private set; }
        public int CurrentPathNodeCount => _currentPath.Count;
        private Vector2 _lastProgressPosition;
        private float _stuckTimer;
        private const float StuckRepathSeconds = 0.45f;
        private const float StuckMinimumProgress = 4f;
        private FiniteStateMachine<IState, IStateTransition> _fsm;
        private float _deathTimer;
        private string _pendingDeathReason = string.Empty;

        private readonly Color _rimColour = new Color(220, 100, 60);
        private readonly Color _bodyColour = new Color(160, 40, 40);
        private Texture2D _texture;

        public TempleThief(
            Vector2 position,
            Vector2 exitPosition,
            IReadOnlyList<Vector2> patrolWaypoints,
            Func<Vector2> getRelicPosition,
            Func<bool> isRelicAvailable,
            Func<Vector2> getPlayerPosition,
            Func<TempleThief, bool> tryStealRelic,
            IEnumerable<Rectangle> walls,
            Game game,
            float speed = MoveSpeed)
            : base(Mass, position, game)
        {
            _spawnPosition = position;
            _patrolWaypoints = new List<Vector2>(patrolWaypoints);

            ExitPosition = exitPosition;
            Speed = speed;
            Circle = new Circle(position, Radius);
            TargetPosition = _patrolWaypoints.Count > 0 ? _patrolWaypoints[0] : getRelicPosition();

            _walls = new List<Rectangle>(walls);
            GetWorldSizeFromWalls(_walls, out _worldWidth, out _worldHeight);
            _pathfinder = new GridPathfinder(_worldWidth, _worldHeight, 24, _walls, Radius + 2f);
            PathTarget = TargetPosition;
            _lastPathGoal = TargetPosition;
            _lastPathStart = position;
            _lastProgressPosition = position;
            _getRelicPosition = getRelicPosition;
            _isRelicAvailable = isRelicAvailable;
            _getPlayerPosition = getPlayerPosition;
            _tryStealRelic = tryStealRelic;

            _seekTarget = new MutableTarget(TargetPosition);
            var seek = new SeekBehaviour(_seekTarget);

            // Pathfinding now handles wall navigation. The older whisker avoidance
            // fought against A* in corridors, so thieves steer only toward the
            // current safe path waypoint.
            _steering = new TruncatedSumSteeringBehaviour(
                new List<SteeringBehaviour> { seek },
                MaxForce);

            _proxy = new Agent(position, 0f, game, new NoOpBehaviour(), Speed);
            _fsm = BuildFiniteStateMachine(_patrolWaypoints);
            _fsm.CurrentState.OnEnter();
        }

        public void SetStateLabel(string groupName, string stateName)
        {
            CurrentGroupName = groupName;
            CurrentStateName = stateName;
        }

        public void StopMoving()
        {
            Velocity = Vector2.Zero;
        }

        public void Stun()
        {
            if (IsDying) return;

            StunHits++;

            if (StunHits >= StunsBeforeRespawn)
            {
                KillAndRespawn("Overloaded by 3 stun bolts");
                return;
            }

            AgentData.IsHitByProjectile = true;
            AgentData.IsStunned = true;
            AgentData.IsStunFinished = false;
            StopMoving();
        }

        public void HitByBoulder(Vector2 boulderPosition, Vector2 boulderVelocity, float boulderMass)
        {
            if (IsDying) return;

            _pendingDeathReason = "Launched by rolling boulder";
            LastRespawnReason = _pendingDeathReason;
            StunHits = 0;
            AgentData.IsHitByProjectile = false;
            AgentData.IsStunned = false;
            AgentData.IsStunFinished = false;
            AgentData.HasReachedExit = false;

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
                impactDirection = Heading.LengthSquared() > 0.001f ? Heading : Vector2.UnitX;

            impactDirection.Normalize();

            // The same boulder impact produces a larger velocity change on a
            // lighter thief than it does on a heavier enemy. This makes the mass
            // difference visible before the enemy respawns.
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

        public void KillAndRespawn(string reason)
        {
            _deathTimer = 0f;
            _pendingDeathReason = string.Empty;
            Deaths++;
            LastRespawnReason = reason;
            StunHits = 0;

            Position = _spawnPosition;
            Velocity = Vector2.Zero;
            Heading = Vector2.UnitX;
            TargetPosition = _patrolWaypoints.Count > 0
                ? _patrolWaypoints[0]
                : _getRelicPosition();
            PathTarget = TargetPosition;
            _currentPath.Clear();
            _currentPathIndex = 0;
            _pathRefreshTimer = 0f;
            _lastPathStart = Position;
            _lastPathGoal = TargetPosition;
            _lastProgressPosition = Position;
            _stuckTimer = 0f;
            Circle.Position = Position;

            ResetAgentData();

            _fsm = BuildFiniteStateMachine(_patrolWaypoints);
            _fsm.CurrentState.OnEnter();
        }

        private void ResetAgentData()
        {
            AgentData.CanSeeRelic = false;
            AgentData.IsRelicAvailable = false;
            AgentData.IsCloseToRelic = false;
            AgentData.HasRelic = false;
            AgentData.IsPlayerNearby = false;
            AgentData.IsHitByProjectile = false;
            AgentData.IsStunned = false;
            AgentData.IsStunFinished = false;
            AgentData.HasReachedExit = false;
        }

        public void TryStealRelic()
        {
            if (HasRelic || !_isRelicAvailable()) return;

            if (_tryStealRelic(this))
                HasRelic = true;
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
                    KillAndRespawn(string.IsNullOrWhiteSpace(_pendingDeathReason)
                        ? "Crushed by rolling boulder"
                        : _pendingDeathReason);

                return;
            }

            UpdateSensors();
            _fsm.Update(deltaTime);

            // IsHitByProjectile is a one-frame event flag. The state machine has
            // now had a chance to read it, so clear it for the next frame.
            AgentData.IsHitByProjectile = false;

            Vector2 steeringTarget = ResolvePathTarget(deltaTime, TargetPosition);
            bool shouldMove = !AgentData.IsStunned &&
                              Vector2.DistanceSquared(Position, steeringTarget) > 16f;

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

        public void Draw(SpriteBatch spriteBatch)
        {
            EnsureTexture(spriteBatch.GraphicsDevice);

            Vector2 origin = new Vector2(_texture.Width / 2f, _texture.Height / 2f);
            float rimScale = (Radius * 2f) / _texture.Width;
            float bodyScale = (Radius * 2f - 5f) / _texture.Width;

            Color rim = IsDying ? new Color(255, 170, 80) :
                        IsStunned ? new Color(120, 120, 150) : _rimColour;
            Color body = HasRelic ? new Color(190, 145, 35) : _bodyColour;
            if (IsDying)
                body = new Color(115, 65, 35);
            else if (IsStunned)
                body = new Color(85, 85, 110);

            spriteBatch.Draw(_texture, Position, null, rim,
                0f, origin, rimScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(_texture, Position, null, body,
                0f, origin, bodyScale, SpriteEffects.None, 0f);
        }

        private FiniteStateMachine<IState, IStateTransition> BuildFiniteStateMachine(
            IReadOnlyList<Vector2> patrolWaypoints)
        {
            var patrol = new PatrolState(this, patrolWaypoints);
            var seekRelic = new SeekRelicState(this, _getRelicPosition);
            var stealRelic = new StealRelicState(this, 0.65f);
            var escape = new EscapeState(this, ExitPosition);
            var flee = new FleePlayerState(this, _getPlayerPosition);
            var stunned = new StunnedState(this, StunSeconds);

            var graph = new SparseGraph<IState, IStateTransition>();

            // Patrol behaviour
            graph.AddEdge(patrol, new HitByProjectileTransition(AgentData), stunned);
            graph.AddEdge(patrol, new CanSeeRelicTransition(AgentData), seekRelic);

            // The relic is now the thief's main objective. Player avoidance is
            // only used as a fallback when there is no stealable relic available,
            // so the player can pressure thieves but cannot completely drag them
            // away from the objective.
            graph.AddEdge(patrol, new FuncTransition(() =>
                AgentData.IsPlayerNearby &&
                !AgentData.IsRelicAvailable &&
                !AgentData.HasRelic &&
                !AgentData.IsHitByProjectile &&
                !AgentData.IsStunned), flee);

            // Relic chase behaviour. Thieves no longer abandon the relic just
            // because the player is nearby.
            graph.AddEdge(seekRelic, new HitByProjectileTransition(AgentData), stunned);
            graph.AddEdge(seekRelic, new CloseToRelicTransition(AgentData), stealRelic);
            graph.AddEdge(seekRelic, new FuncTransition(() => !AgentData.IsRelicAvailable && !AgentData.HasRelic), patrol);

            // Stealing behaviour. The steal attempt now finishes even if the
            // player is close; the player must actually shoot/stun the thief to
            // interrupt it.
            graph.AddEdge(stealRelic, new HitByProjectileTransition(AgentData), stunned);
            graph.AddEdge(stealRelic, new HasRelicTransition(AgentData), escape);
            graph.AddEdge(stealRelic, new FuncTransition(() => !AgentData.IsRelicAvailable && !AgentData.HasRelic), patrol);

            // Escape behaviour
            graph.AddEdge(escape, new HitByProjectileTransition(AgentData), stunned);
            graph.AddEdge(escape, new FuncTransition(() => !AgentData.HasRelic && AgentData.IsRelicAvailable), seekRelic);
            graph.AddEdge(escape, new FuncTransition(() => !AgentData.HasRelic && !AgentData.IsRelicAvailable), patrol);

            // Defensive behaviour. If the relic becomes available again,
            // immediately return to the objective instead of waiting until the
            // player has moved away.
            graph.AddEdge(flee, new HitByProjectileTransition(AgentData), stunned);
            graph.AddEdge(flee, new FuncTransition(() => AgentData.IsRelicAvailable), seekRelic);
            graph.AddEdge(flee, new FuncTransition(() => !AgentData.IsPlayerNearby && !AgentData.IsRelicAvailable), patrol);

            // Disabled behaviour
            graph.AddEdge(stunned, new FuncTransition(() => AgentData.IsStunFinished && AgentData.HasRelic), escape);
            graph.AddEdge(stunned, new FuncTransition(() => AgentData.IsStunFinished && AgentData.IsRelicAvailable), seekRelic);
            graph.AddEdge(stunned, new FuncTransition(() => AgentData.IsStunFinished && !AgentData.IsRelicAvailable), patrol);

            return new FiniteStateMachine<IState, IStateTransition>(graph, patrol);
        }

        private void UpdateSensors()
        {
            Vector2 relicPosition = _getRelicPosition();
            Vector2 playerPosition = _getPlayerPosition();

            AgentData.IsRelicAvailable = _isRelicAvailable();
            AgentData.IsCloseToRelic = AgentData.IsRelicAvailable &&
                                       Vector2.Distance(Position, relicPosition) <= StealRange;
            AgentData.IsPlayerNearby = Vector2.Distance(Position, playerPosition) <= PlayerThreatRange;
            AgentData.HasReachedExit = HasRelic &&
                                       Vector2.Distance(Position, ExitPosition) <= ExitRange;

            float relicDistance = Vector2.Distance(Position, relicPosition);
            AgentData.CanSeeRelic = AgentData.IsRelicAvailable &&
                                    relicDistance <= RelicViewRange &&
                                    !IsLineBlockedByWall(Position, relicPosition);
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
                MathHelper.Clamp(point.X, 36f, _worldWidth - 36f),
                MathHelper.Clamp(point.Y, 36f, _worldHeight - 36f));
        }

        private bool HasClearPath(Vector2 start, Vector2 end)
        {
            foreach (Rectangle wall in _walls)
            {
                if (IsBorderWall(wall)) continue;

                Rectangle inflated = InflateRectangle(wall, (int)MathF.Ceiling(Radius + 7f));
                if (LineIntersectsRect(start, end, inflated))
                    return false;
            }

            return true;
        }

        private bool IsLineBlockedByWall(Vector2 start, Vector2 end)
        {
            foreach (Rectangle wall in _walls)
            {
                // Border walls should block movement, but they should not stop
                // the AI from noticing the relic from inside the temple room.
                if (IsBorderWall(wall))
                    continue;

                if (LineIntersectsRect(start, end, wall))
                    return true;
            }

            return false;
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

        private static List<ICollidable> BuildPillarCollidables(
            IEnumerable<Rectangle> walls, int maxPillarDim = 100)
        {
            var list = new List<ICollidable>();

            foreach (Rectangle w in walls)
            {
                if (IsBorderWall(w)) continue;

                float major = MathF.Max(w.Width, w.Height);
                float minor = MathF.Min(w.Width, w.Height);
                int samples = Math.Max(1, (int)MathF.Ceiling(major / 48f));
                float radius = MathF.Max(18f, minor * 0.5f + 12f);

                for (int i = 0; i < samples; i++)
                {
                    float t = (i + 0.5f) / samples;
                    Vector2 centre = w.Width >= w.Height
                        ? new Vector2(MathHelper.Lerp(w.Left, w.Right, t), w.Center.Y)
                        : new Vector2(w.Center.X, MathHelper.Lerp(w.Top, w.Bottom, t));

                    list.Add(new CircleCollidable(new Circle(centre, radius)));
                }
            }

            return list;
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

        private sealed class CircleCollidable : ICollidable
        {
            private readonly Circle _shape;
            public Shape Shapes => _shape;
            public CircleCollidable(Circle shape) => _shape = shape;
            public bool CollidesWith(ICollidable other)
                => other?.Shapes != null && _shape.Intersects(other.Shapes);
            public bool CollidesWith(ICollidable other, ref Vector2 collisionNormal)
                => other?.Shapes != null && _shape.Intersects(other.Shapes, ref collisionNormal);
        }
    }
}
