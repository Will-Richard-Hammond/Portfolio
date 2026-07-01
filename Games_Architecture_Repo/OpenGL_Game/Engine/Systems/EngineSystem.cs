using OpenGL_Game.Engine.Managers;

namespace OpenGL_Game.Engine.Systems
{
    abstract class EngineSystem
    {
        public virtual string Name => GetType().Name;
        public abstract void Update(EngineEntityManager entityManager, float deltaTime);
    }
}