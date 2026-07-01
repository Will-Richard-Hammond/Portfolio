namespace OpenGL_Game.Engine.Components
{
    class GameplayTagComponent : IEngineComponent
    {
        public GameplayTagComponent(string tag)
        {
            Tag = tag;
        }

        public string Tag { get; set; }
        public EngineComponentType ComponentType => EngineComponentType.Tag;
    }
}
