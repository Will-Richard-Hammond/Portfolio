namespace MGGameLibrary.Graphs
{
    /// <summary>
    /// A generic finite state machine backed by a SparseGraph.
    /// TNode must implement IState; TEdge must implement IStateTransition.
    /// </summary>
    public class FiniteStateMachine<TNode, TEdge>
        where TNode : IState
        where TEdge : IStateTransition
    {
        private SparseGraph<TNode, TEdge> _fsm = new();

        /// <summary>The state the machine is currently in.</summary>
        public TNode CurrentState { get; private set; }

        public FiniteStateMachine(SparseGraph<TNode, TEdge> fsm, TNode currentState)
        {
            _fsm = fsm;
            CurrentState = currentState;
        }

        /// <summary>
        /// Checks each outgoing transition from the current state.
        /// If one fires, exits the current state, moves to the new state, and enters it.
        /// Then calls OnUpdate on the (potentially new) current state.
        /// </summary>
        public void Update(float seconds)
        {
            var edges = _fsm.GetEdges(CurrentState);

            foreach ((TEdge, TNode) edge in edges)
            {
                if (edge.Item1.ToTransition())
                {
                    CurrentState.OnExit();
                    CurrentState = edge.Item2;
                    CurrentState.OnEnter();
                }
            }

            if (CurrentState != null)
            {
                CurrentState.OnUpdate(seconds);
            }
        }
    }
}
