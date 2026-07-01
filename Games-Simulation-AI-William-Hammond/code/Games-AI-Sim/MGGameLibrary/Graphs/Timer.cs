using System;

namespace MGGameLibrary.Graphs
{
    /// <summary>
    /// Simple countdown timer. Call Update each frame; check IsFinished to see if time is up.
    /// </summary>
    public class Timer
    {
        public float Duration { get; }
        private float _elapsed;

        public float Elapsed       => _elapsed;
        public float TimeRemaining => MathF.Max(0f, Duration - _elapsed);
        public bool  IsFinished    => _elapsed >= Duration;

        public Timer(float duration)
        {
            Duration = duration;
            _elapsed = 0f;
        }

        public void Update(float seconds) => _elapsed += seconds;
        public void Reset()               => _elapsed  = 0f;
    }
}
