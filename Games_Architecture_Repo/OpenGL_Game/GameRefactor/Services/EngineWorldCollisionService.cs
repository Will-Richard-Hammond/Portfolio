using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenGL_Game.Engine.Components;
using OpenGL_Game.Engine.Entities;
using OpenGL_Game.Engine.Managers;

namespace OpenGL_Game.GameRefactor.Services
{
    class EngineWorldCollisionService
    {
        readonly EngineEntityManager entityManager;

        public EngineWorldCollisionService(EngineEntityManager entityManager)
        {
            this.entityManager = entityManager;
        }

        public Vector3 ResolvePlayerMovement(string playerEntityName, Vector3 previousPosition, Vector3 desiredPosition)
        {
            var player = entityManager.FindEntity(playerEntityName);
            var playerAabb = player?.GetComponent<AabbCollisionComponent>();
            if (playerAabb == null)
                return desiredPosition;

            Vector3 resolved = previousPosition;
            resolved.X = desiredPosition.X;
            resolved = ResolveAxis(playerEntityName, resolved, previousPosition, playerAabb, true);
            Vector3 afterX = resolved;
            resolved.Z = desiredPosition.Z;
            resolved = ResolveAxis(playerEntityName, resolved, afterX, playerAabb, false);
            resolved.Y = previousPosition.Y;
            return resolved;
        }

        Vector3 ResolveAxis(string playerEntityName, Vector3 desiredPosition, Vector3 previousPosition, AabbCollisionComponent playerAabb, bool resolveX)
        {
            const float skin = 0.001f;
            foreach (var entity in entityManager.Entities())
            {
                if (entity.Name == playerEntityName || entity.Name == "Floor" || entity.Name == "Skybox")
                    continue;
                if (entity.Name.StartsWith("Drone_") || entity.Name.StartsWith("PowerUp_"))
                    continue;

                var transform = entity.GetComponent<TransformComponent>();
                var aabb = entity.GetComponent<AabbCollisionComponent>();
                if (transform == null || aabb == null)
                    continue;
                if (!IntersectsAabb(desiredPosition, playerAabb, transform.Position, aabb))
                    continue;

                Vector3 wallMin = transform.Position + aabb.LocalMin;
                Vector3 wallMax = transform.Position + aabb.LocalMax;
                if (resolveX)
                {
                    if (desiredPosition.X > previousPosition.X)
                        desiredPosition.X = wallMin.X - playerAabb.LocalMax.X - skin;
                    else if (desiredPosition.X < previousPosition.X)
                        desiredPosition.X = wallMax.X - playerAabb.LocalMin.X + skin;
                }
                else
                {
                    if (desiredPosition.Z > previousPosition.Z)
                        desiredPosition.Z = wallMin.Z - playerAabb.LocalMax.Z - skin;
                    else if (desiredPosition.Z < previousPosition.Z)
                        desiredPosition.Z = wallMax.Z - playerAabb.LocalMin.Z + skin;
                }
            }

            return desiredPosition;
        }

        static bool IntersectsAabb(Vector3 firstPosition, AabbCollisionComponent firstAabb, Vector3 secondPosition, AabbCollisionComponent secondAabb)
        {
            Vector3 firstMin = firstPosition + firstAabb.LocalMin;
            Vector3 firstMax = firstPosition + firstAabb.LocalMax;
            Vector3 secondMin = secondPosition + secondAabb.LocalMin;
            Vector3 secondMax = secondPosition + secondAabb.LocalMax;
            return firstMax.X > secondMin.X && firstMin.X < secondMax.X &&
                   firstMax.Y > secondMin.Y && firstMin.Y < secondMax.Y &&
                   firstMax.Z > secondMin.Z && firstMin.Z < secondMax.Z;
        }
    }
}
