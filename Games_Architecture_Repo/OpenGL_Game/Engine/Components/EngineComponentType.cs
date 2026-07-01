namespace OpenGL_Game.Engine.Components
{
    [System.Flags]
    enum EngineComponentType
    {
        None = 0,
        Transform = 1 << 0,
        Velocity = 1 << 1,
        CollisionAabb = 1 << 2,
        Tag = 1 << 3,
        Render = 1 << 4,
        Scale = 1 << 5,
        Audio = 1 << 6,
    }
}