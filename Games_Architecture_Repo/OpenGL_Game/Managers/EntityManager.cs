using System.Collections.Generic;
using OpenGL_Game.Systems;
using OpenGL_Game.Objects;
using OpenGL_Game.Components;
using System.Linq;

namespace OpenGL_Game.Managers
{
    class EntityManager
    {
        List<Entity> entityList;
        // Registry lists for quick lookups (avoid scanning all entities each frame)
        List<Entity> collisionLineEntities = new List<Entity>();

        public EntityManager()
        {
            entityList = new List<Entity>();
        }

        public void AddEntity(Entity entity)
        {
            //Entity result = FindEntity(entity.Name);
            //Debug.Assert(result == null, "Entity '" + entity.Name + "' already exists");
            entityList.Add(entity);

            //Register entity in relevant registries based on its components.
            //This avoids scanning all entities every frame when only a small subset
            //is needed (e.g., for line/line collision detection).
            RegisterEntityToRegistries(entity);
        }

        void RegisterEntityToRegistries(Entity entity)
        {
            //If entity has a ComponentCollisionLine, add to collisionLineEntities
            if (entity.Components.Any(c => c is ComponentCollisionLine))
            {
                if (!collisionLineEntities.Contains(entity))
                    collisionLineEntities.Add(entity);
            }

            //Future:register other component-based registries here (AABB, sphere, etc.)
        }

        //Expose registry accessors for systems that want the prefiltered lists
        public List<Entity> EntitiesWithCollisionLine()
        {
            return collisionLineEntities;
        }

        private Entity FindEntity(string name)
        {
            return entityList.Find(delegate(Entity e)
            {
                return e.Name == name;
            }
            );
        }

        public List<Entity> Entities()
        {
            return entityList;
        }

        public void CloseAllComponents()
        {
            foreach (var entity in entityList)
            {
                foreach (var component in entity.Components)
                {
                    if (component is ICloseableComponent closeable)
                    {
                        try
                        {
                            closeable.Close();
                        }
                        catch { }
                    }
                }
            }
        }
    }
}
