using OpenTK.Mathematics;
using OpenGL_Game.Engine.Components;
using OpenGL_Game.Engine.Entities;
using OpenGL_Game.Engine.Managers;

namespace OpenGL_Game.GameRefactor.Factories
{
    class RefactorEntityFactory
    {
        readonly EngineEntityManager engineEntityManager;

        public RefactorEntityFactory(EngineEntityManager engineEntityManager)
        {
            this.engineEntityManager = engineEntityManager;
        }

        public EngineEntity CreateRenderable(
            string name,
            Vector3 position,
            string geometryPath,
            Vector3? scale = null,
            string shaderKey = null,
            string texturePath = null,
            string gameplayTag = null,
            (Vector3 min, Vector3 max)? aabb = null)
        {
            var entity = new EngineEntity(name);
            entity.AddComponent(new TransformComponent(position));
            entity.AddComponent(new RenderComponent(geometryPath, shaderKey, texturePath));
            entity.AddComponent(new TagComponent(name));
            if (!string.IsNullOrWhiteSpace(gameplayTag))
                entity.AddComponent(new GameplayTagComponent(gameplayTag));
            if (scale.HasValue)
                entity.AddComponent(new ScaleComponent(scale.Value));
            if (aabb.HasValue)
                entity.AddComponent(new AabbCollisionComponent(aabb.Value.min, aabb.Value.max));

            engineEntityManager.AddEntity(entity);
            return entity;
        }
    }
}
