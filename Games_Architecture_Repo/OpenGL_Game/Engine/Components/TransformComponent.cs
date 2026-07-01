using OpenTK.Mathematics;

namespace OpenGL_Game.Engine.Components
{
    class TransformComponent : IEngineComponent
    {
        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; } = Vector3.One;

        /// <summary>
        /// World-space orientation. Defaults to identity so existing entities
        /// that never set this are completely unaffected.
        /// </summary>
        public Quaternion Rotation { get; set; } = Quaternion.Identity;

        public TransformComponent(Vector3 position)
        {
            Position = position;
        }

        public EngineComponentType ComponentType => EngineComponentType.Transform;
    }
}