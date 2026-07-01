namespace MGGameLibrary.Graphs
{
    /// <summary>
    /// Fires once when the associated Timer has finished counting down.
    /// </summary>
    public class TimedTransition : IStateTransition
    {
        private readonly Timer _timer;

        public TimedTransition(Timer timer) => _timer = timer;

        public bool ToTransition() => _timer.IsFinished;
    }
}
