using OpenTK.Mathematics;

namespace OpenGL_Game.Engine.Components
{
    class ScaleComponent : IEngineComponent
    {
        public ScaleComponent(float x, float y, float z)
        {
            Scale = new Vector3(x, y, z);
        }

        public ScaleComponent(Vector3 scale)
        {
            Scale = scale;
        }

        public Vector3 Scale { get; set; }
        public EngineComponentType ComponentType => EngineComponentType.Scale;
    }
}
