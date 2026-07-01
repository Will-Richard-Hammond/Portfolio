using OpenGL_Game.Engine.Components;
using OpenGL_Game.Engine.Managers;

namespace OpenGL_Game.Engine.Systems
{
    class MovementSystem : EngineSystem
    {
        public override void Update(EngineEntityManager entityManager, float deltaTime)
        {
            foreach (var entity in entityManager.Entities())
            {
                var transform = entity.GetComponent<TransformComponent>();
                var velocity = entity.GetComponent<VelocityComponent>();
                if (transform == null || velocity == null)
                    continue;

                transform.Position += velocity.Velocity * deltaTime;
            }
        }
    }
}