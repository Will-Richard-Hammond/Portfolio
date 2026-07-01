namespace OpenGL_Game.GameRefactor.Config
{
    class GameSettings
    {
        public float MouseSensitivity { get; set; } = 0.0025f;
        public float MoveSpeed { get; set; } = 3.0f;
        public int MaxLives { get; set; } = 3;
        public float DroneSpeed { get; set; } = 1.4f;
        public float DroneTouchDistance { get; set; } = 0.75f;
        public float DroneDisableDuration { get; set; } = 5f;
        public float WeaponRange { get; set; } = 8f;
        public float WeaponHitRadius { get; set; } = 0.65f;
        public int BaseDroneHealth { get; set; } = 3;
        public float BirdseyeFovDegrees { get; set; } = 103f;
        public float NormalFovDegrees { get; set; } = 103f;
    }
}