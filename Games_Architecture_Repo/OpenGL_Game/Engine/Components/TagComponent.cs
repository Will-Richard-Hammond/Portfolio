namespace OpenGL_Game.Engine.Components
{
    class TagComponent : IEngineComponent
    {
        public TagComponent(string value)
        {
            Value = value;
        }

        public string Value { get; set; }
        public EngineComponentType ComponentType => EngineComponentType.Tag;
    }
}
