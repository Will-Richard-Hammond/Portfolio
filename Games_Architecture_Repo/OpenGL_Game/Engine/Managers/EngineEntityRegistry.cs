using System.Collections.Generic;
using System.Linq;
using OpenGL_Game.Engine.Entities;

namespace OpenGL_Game.Engine.Managers
{
    class EngineEntityRegistry
    {
        readonly Dictionary<string, EngineEntity> entitiesByName = new();

        public void Register(EngineEntity entity)
        {
            if (!string.IsNullOrWhiteSpace(entity.Name))
                entitiesByName[entity.Name] = entity;
        }

        public EngineEntity Find(string name)
        {
            return entitiesByName.TryGetValue(name, out var entity) ? entity : null;
        }

        public void Clear()
        {
            entitiesByName.Clear();
        }
    }
}
