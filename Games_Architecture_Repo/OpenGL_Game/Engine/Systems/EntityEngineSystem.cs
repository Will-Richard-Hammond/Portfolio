using OpenGL_Game.Engine.Entities;
using OpenGL_Game.Engine.Managers;

namespace OpenGL_Game.Engine.Systems
{
    abstract class EntityEngineSystem : EngineSystem
    {
        public override void Update(EngineEntityManager entityManager, float deltaTime)
        {
            foreach (var entity in entityManager.Entities())
                OnUpdate(entity, deltaTime);
        }

        protected abstract void OnUpdate(EngineEntity entity, float deltaTime);
    }
}
