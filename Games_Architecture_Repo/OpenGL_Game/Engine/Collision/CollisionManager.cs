using OpenTK.Mathematics;
using OpenGL_Game.Components;
using OpenGL_Game.Objects;

namespace OpenGL_Game.Engine.Collision
{
    abstract class CollisionManager
    {
        public virtual bool IntersectsAabb(
            Vector3 firstPosition,
            ComponentCollisionAABB firstAabb,
            Vector3 secondPosition,
            ComponentCollisionAABB secondAabb)
        {
            Vector3 firstMin = firstPosition + firstAabb.LocalMin;
            Vector3 firstMax = firstPosition + firstAabb.LocalMax;
            Vector3 secondMin = secondPosition + secondAabb.LocalMin;
            Vector3 secondMax = secondPosition + secondAabb.LocalMax;

            return firstMax.X > secondMin.X && firstMin.X < secondMax.X &&
                   firstMax.Y > secondMin.Y && firstMin.Y < secondMax.Y &&
                   firstMax.Z > secondMin.Z && firstMin.Z < secondMax.Z;
        }

        public abstract void HandleCollision(Entity first, Entity second);
    }
}