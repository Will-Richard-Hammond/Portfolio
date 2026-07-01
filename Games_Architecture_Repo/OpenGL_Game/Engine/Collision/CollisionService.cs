using System.Collections.Generic;
using OpenGL_Game.Objects;

namespace OpenGL_Game.Engine.Collision
{
    class CollisionService
    {
        readonly CollisionManager collisionManager;

        public CollisionService(CollisionManager collisionManager)
        {
            this.collisionManager = collisionManager;
        }

        public void HandleCollisions(IEnumerable<(Entity first, Entity second)> collisions)
        {
            foreach (var collision in collisions)
                collisionManager.HandleCollision(collision.first, collision.second);
        }
    }
}