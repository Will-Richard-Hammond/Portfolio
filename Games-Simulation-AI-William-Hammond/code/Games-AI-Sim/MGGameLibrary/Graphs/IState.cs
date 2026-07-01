namespace MGGameLibrary.Graphs
{
    /// <summary>
    /// Represents a state in a finite state machine.
    /// </summary>
    public interface IState
    {
        /// <summary>Called once when this state is entered.</summary>
        void OnEnter();

        /// <summary>Called once when this state is exited.</summary>
        void OnExit();

        /// <summary>Called every update tick while this state is active.</summary>
        void OnUpdate(float seconds);
    }
}
