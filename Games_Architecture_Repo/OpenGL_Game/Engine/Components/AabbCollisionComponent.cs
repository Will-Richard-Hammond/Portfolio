using OpenTK.Mathematics;

namespace OpenGL_Game.Engine.Components
{
    class AabbCollisionComponent : IEngineComponent
    {
        public Vector3 LocalMin { get; set; }
        public Vector3 LocalMax { get; set; }

        public AabbCollisionComponent(Vector3 localMin, Vector3 localMax)
        {
            LocalMin = localMin;
            LocalMax = localMax;
        }

        public EngineComponentType ComponentType => EngineComponentType.CollisionAabb;
    }
}