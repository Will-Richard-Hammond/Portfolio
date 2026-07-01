using System.Collections.Generic;
using OpenGL_Game.Engine.Systems;

namespace OpenGL_Game.Engine.Managers
{
    class EngineSystemManager
    {
        readonly List<EngineSystem> systems = new();

        public void AddSystem(EngineSystem system)
        {
            systems.Add(system);
        }

        public EngineSystem FindSystem(string name)
        {
            foreach (var system in systems)
            {
                if (system.Name == name)
                    return system;
            }

            return null;
        }

        public void UpdateAll(EngineEntityManager entityManager, float deltaTime)
        {
            foreach (var system in systems)
                system.Update(entityManager, deltaTime);
        }

        public void Clear()
        {
            systems.Clear();
        }
    }
}