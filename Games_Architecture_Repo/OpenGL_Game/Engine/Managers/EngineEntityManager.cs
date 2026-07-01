using System.Collections.Generic;
using OpenGL_Game.Engine.Entities;

namespace OpenGL_Game.Engine.Managers
{
    class EngineEntityManager
    {
        readonly List<EngineEntity> entities = new();
        readonly EngineEntityRegistry registry = new();

        public void AddEntity(EngineEntity entity)
        {
            entities.Add(entity);
            registry.Register(entity);
        }

        public IReadOnlyList<EngineEntity> Entities() => entities;

        public EngineEntity FindEntity(string name) => registry.Find(name);

        public void Clear()
        {
            entities.Clear();
            registry.Clear();
        }
    }
}