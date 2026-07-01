using OpenTK.Mathematics;

namespace OpenGL_Game.Engine.Components
{
    class VelocityComponent : IEngineComponent
    {
        public Vector3 Velocity { get; set; }

        public VelocityComponent(Vector3 velocity)
        {
            Velocity = velocity;
        }

        public EngineComponentType ComponentType => EngineComponentType.Velocity;
    }
}