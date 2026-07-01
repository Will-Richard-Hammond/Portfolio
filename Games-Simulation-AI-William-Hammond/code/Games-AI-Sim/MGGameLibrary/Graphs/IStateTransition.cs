namespace MGGameLibrary.Graphs
{
    /// <summary>
    /// Represents a transition edge in a finite state machine.
    /// </summary>
    public interface IStateTransition
    {
        /// <summary>Returns true when this transition should fire.</summary>
        bool ToTransition();
    }
}
