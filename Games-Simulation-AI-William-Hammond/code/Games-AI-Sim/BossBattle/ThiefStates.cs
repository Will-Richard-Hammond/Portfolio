using MGGameLibrary.Graphs;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace BossBattle
{
    public sealed class PatrolState : IState
    {
        private readonly TempleThief _thief;
        private readonly IReadOnlyList<Vector2> _waypoints;
        private readonly float _waypointRadius;
        private int _currentIndex;

        public PatrolState(TempleThief thief, IReadOnlyList<Vector2> waypoints, float waypointRadius = 20f)
        {
            _thief = thief;
            _waypoints = waypoints;
            _waypointRadius = waypointRadius;
        }

        public void OnEnter()
        {
            _thief.SetStateLabel("Searching", "Patrol");
        }

        public void OnExit() { }

        public void OnUpdate(float seconds)
        {
            if (_waypoints.Count == 0)
            {
                _thief.TargetPosition = _thief.Position;
                return;
            }

            Vector2 target = _waypoints[_currentIndex];
            _thief.TargetPosition = target;

            if (Vector2.Distance(_thief.Position, target) < _waypointRadius)
                _currentIndex = (_currentIndex + 1) % _waypoints.Count;
        }
    }

    public sealed class SeekRelicState : IState
    {
        private readonly TempleThief _thief;
        private readonly Func<Vector2> _getRelicPosition;

        public SeekRelicState(TempleThief thief, Func<Vector2> getRelicPosition)
        {
            _thief = thief;
            _getRelicPosition = getRelicPosition;
        }

        public void OnEnter()
        {
            _thief.SetStateLabel("Searching", "SeekRelic");
        }

        public void OnExit() { }

        public void OnUpdate(float seconds)
        {
            _thief.TargetPosition = _getRelicPosition();
        }
    }

    public sealed class StealRelicState : IState
    {
        private readonly TempleThief _thief;
        private readonly Timer _stealTimer;
        private bool _attemptedSteal;

        public bool StealComplete => _stealTimer.IsFinished;

        public StealRelicState(TempleThief thief, float stealDuration)
        {
            _thief = thief;
            _stealTimer = new Timer(stealDuration);
        }

        public void OnEnter()
        {
            _thief.SetStateLabel("Stealing", "StealRelic");
            _stealTimer.Reset();
            _attemptedSteal = false;
            _thief.StopMoving();
        }

        public void OnExit() { }

        public void OnUpdate(float seconds)
        {
            _stealTimer.Update(seconds);
            _thief.TargetPosition = _thief.Position;
            _thief.StopMoving();

            if (!_attemptedSteal && _stealTimer.IsFinished)
            {
                _attemptedSteal = true;
                _thief.TryStealRelic();
            }
        }
    }

    public sealed class EscapeState : IState
    {
        private readonly TempleThief _thief;
        private readonly Vector2 _escapePosition;

        public EscapeState(TempleThief thief, Vector2 escapePosition)
        {
            _thief = thief;
            _escapePosition = escapePosition;
        }

        public void OnEnter()
        {
            _thief.SetStateLabel("Stealing", "Escape");
        }

        public void OnExit() { }

        public void OnUpdate(float seconds)
        {
            _thief.TargetPosition = _escapePosition;
        }
    }

    public sealed class FleePlayerState : IState
    {
        private readonly TempleThief _thief;
        private readonly Func<Vector2> _getPlayerPosition;
        private readonly float _fleeDistance;

        public FleePlayerState(TempleThief thief, Func<Vector2> getPlayerPosition, float fleeDistance = 300f)
        {
            _thief = thief;
            _getPlayerPosition = getPlayerPosition;
            _fleeDistance = fleeDistance;
        }

        public void OnEnter()
        {
            _thief.SetStateLabel("Defensive", "FleePlayer");
        }

        public void OnExit() { }

        public void OnUpdate(float seconds)
        {
            Vector2 playerPos = _getPlayerPosition();
            Vector2 awayDir = _thief.Position - playerPos;

            if (awayDir.LengthSquared() > 0.01f)
                awayDir = Vector2.Normalize(awayDir);
            else
                awayDir = Vector2.UnitX;

            _thief.TargetPosition = _thief.Position + awayDir * _fleeDistance;
        }
    }

    public sealed class StunnedState : IState
    {
        private readonly TempleThief _thief;
        private readonly float _stunDuration;
        private float _stunTimer;

        public StunnedState(TempleThief thief, float stunDuration)
        {
            _thief = thief;
            _stunDuration = stunDuration;
        }

        public void OnEnter()
        {
            _thief.SetStateLabel("Disabled", "Stunned");
            _stunTimer = _stunDuration;
            _thief.AgentData.IsStunned = true;
            _thief.AgentData.IsStunFinished = false;
            _thief.StopMoving();
        }

        public void OnExit()
        {
            _thief.AgentData.IsStunned = false;
            _thief.AgentData.IsStunFinished = false;
        }

        public void OnUpdate(float seconds)
        {
            _stunTimer -= seconds;
            _thief.TargetPosition = _thief.Position;
            _thief.StopMoving();

            if (_stunTimer <= 0f)
                _thief.AgentData.IsStunFinished = true;
        }
    }
}
