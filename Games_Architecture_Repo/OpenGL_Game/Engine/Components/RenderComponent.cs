namespace OpenGL_Game.Engine.Components
{
    class RenderComponent : IEngineComponent
    {
        public RenderComponent(string geometryPath, string shaderKey = null, string texturePath = null)
        {
            GeometryPath = geometryPath;
            ShaderKey = shaderKey;
            TexturePath = texturePath;
        }

        public string GeometryPath { get; }
        public string ShaderKey { get; }
        public string TexturePath { get; }
        public EngineComponentType ComponentType => EngineComponentType.Render;
    }
}
