using MGGameLibrary.Graphs;
using System;

namespace BossBattle
{
    /// <summary>
    /// Small reusable transition that lets the thief FSM use readable lambda
    /// conditions without creating a new class for every tiny rule.
    /// </summary>
    public sealed class FuncTransition : IStateTransition
    {
        private readonly Func<bool> _condition;
        public FuncTransition(Func<bool> condition) => _condition = condition;
        public bool ToTransition() => _condition();
    }

    public sealed class CanSeeRelicTransition : IStateTransition
    {
        private readonly ThiefAgent _agent;
        public CanSeeRelicTransition(ThiefAgent agent) => _agent = agent;
        public bool ToTransition()
        {
            return _agent.CanSeeRelic &&
                   _agent.IsRelicAvailable &&
                   !_agent.IsHitByProjectile &&
                   !_agent.IsStunned;
        }
    }

    public sealed class CloseToRelicTransition : IStateTransition
    {
        private readonly ThiefAgent _agent;
        public CloseToRelicTransition(ThiefAgent agent) => _agent = agent;
        public bool ToTransition()
        {
            return _agent.IsCloseToRelic &&
                   _agent.IsRelicAvailable &&
                   !_agent.HasRelic &&
                   !_agent.IsHitByProjectile &&
                   !_agent.IsStunned;
        }
    }

    public sealed class HasRelicTransition : IStateTransition
    {
        private readonly ThiefAgent _agent;
        public HasRelicTransition(ThiefAgent agent) => _agent = agent;
        public bool ToTransition()
        {
            return _agent.HasRelic &&
                   !_agent.IsHitByProjectile &&
                   !_agent.IsStunned;
        }
    }

    public sealed class PlayerNearbyTransition : IStateTransition
    {
        private readonly ThiefAgent _agent;
        public PlayerNearbyTransition(ThiefAgent agent) => _agent = agent;
        public bool ToTransition()
        {
            return _agent.IsPlayerNearby &&
                   !_agent.HasRelic &&
                   !_agent.IsHitByProjectile &&
                   !_agent.IsStunned;
        }
    }

    public sealed class HitByProjectileTransition : IStateTransition
    {
        private readonly ThiefAgent _agent;
        public HitByProjectileTransition(ThiefAgent agent) => _agent = agent;
        public bool ToTransition() => _agent.IsHitByProjectile;
    }

    public sealed class StunFinishedTransition : IStateTransition
    {
        private readonly ThiefAgent _agent;
        public StunFinishedTransition(ThiefAgent agent) => _agent = agent;
        public bool ToTransition() => _agent.IsStunFinished;
    }

    public sealed class ReachedExitTransition : IStateTransition
    {
        private readonly ThiefAgent _agent;
        public ReachedExitTransition(ThiefAgent agent) => _agent = agent;
        public bool ToTransition() => _agent.HasReachedExit;
    }
}
