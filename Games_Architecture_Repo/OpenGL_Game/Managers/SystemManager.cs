using System;
using System.Collections.Generic;
using OpenGL_Game.Systems;
using OpenGL_Game.Objects;

namespace OpenGL_Game.Managers
{
    class SystemManager
    {
        List<Systems.System> systemList = new List<Systems.System>();

        public SystemManager()
        {
        }

        public void ActionSystems(EntityManager entityManager)
        {
            List<Entity> entityList = entityManager.Entities() ?? new List<Entity>();

            foreach (Systems.System system in systemList)
            {
                try
                {
                    // Choose the most appropriate entity list to pass to the system.
                    // Use registries in EntityManager to avoid scanning all entities when possible.
                    List<Entity> listToPass = entityList;

                    // Example: if the system is the line/line collision detector, pass the pre-registered list.
                    if (system is Systems.SystemCollisionLineLine)
                    {
                        listToPass = entityManager.EntitiesWithCollisionLine();
                    }

                    // Call list-level action first for systems that need the whole (or filtered) entity list
                    system.OnAction(listToPass);

                    // Then call per-entity behaviour (existing behaviour)
                    foreach (Entity entity in entityList)
                    {
                        system.OnAction(entity);
                    }
                }
                catch (Exception ex)
                {
                    // Catch exceptions so one failing system doesn't stop others and to surface runtime errors
                    // In production builds consider logging to a file or telemetry instead of Console.
                    Console.WriteLine($"[SystemManager] Exception in system '{system?.Name ?? "NULL"}': {ex}");
                }
            }
        }

        public void AddSystem(Systems.System system)
        {
            //ISystem result = FindSystem(system.Name);
            //Debug.Assert(result != null, "System '" + system.Name + "' already exists");
            systemList.Add(system);
        }

        private Systems.System FindSystem(string name)
        {
            return systemList.Find(delegate(Systems.System system)
            {
                return system.Name == name;
            }
            );
        }
    }
}
