using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenGL_Game.Engine.Components;
using OpenGL_Game.Engine.Entities;
using OpenGL_Game.Engine.Managers;

namespace OpenGL_Game.GameRefactor.Services
{
    class EngineEntityQueryService
    {
        readonly EngineEntityManager entityManager;

        public EngineEntityQueryService(EngineEntityManager entityManager)
        {
            this.entityManager = entityManager;
        }

        public EngineEntity Find(string name) => entityManager.FindEntity(name);

        public Vector3? GetPosition(string name)
        {
            var entity = Find(name);
            return entity?.GetComponent<TransformComponent>()?.Position;
        }

        public void SetPosition(string name, Vector3 position)
        {
            var entity = Find(name);
            var transform = entity?.GetComponent<TransformComponent>();
            if (transform != null)
                transform.Position = position;
        }

        public IEnumerable<EngineEntity> FindByPrefix(string prefix)
        {
            foreach (var entity in entityManager.Entities())
            {
                if (entity.Name.StartsWith(prefix))
                    yield return entity;
            }
        }
    }
}
