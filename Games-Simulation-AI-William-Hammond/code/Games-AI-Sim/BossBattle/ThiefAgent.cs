namespace BossBattle
{
    /// <summary>
    /// Sensor/memory data for one TempleThief. The FSM transitions read these
    /// booleans to decide when the thief should change state.
    /// </summary>
    public class ThiefAgent
    {
        public bool CanSeeRelic { get; set; }
        public bool IsRelicAvailable { get; set; }
        public bool IsCloseToRelic { get; set; }
        public bool HasRelic { get; set; }
        public bool IsPlayerNearby { get; set; }
        public bool IsHitByProjectile { get; set; }
        public bool IsStunned { get; set; }
        public bool IsStunFinished { get; set; }
        public bool HasReachedExit { get; set; }
    }
}
