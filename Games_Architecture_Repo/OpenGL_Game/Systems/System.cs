using OpenGL_Game.Components;
using OpenGL_Game.Objects;
using System.Collections.Generic;

namespace OpenGL_Game.Systems
{
    abstract class System
    {
        public IComponent GetComponent(Entity entity, ComponentTypes componentType)
        {
            List<IComponent> components = entity.Components;

            IComponent iComponent = components.Find(delegate (IComponent component)
            {
                return component.ComponentType == componentType;
            });

            return iComponent;
        }

        // Called per-entity (existing behaviour)
        public abstract void OnAction(Entity entity);

        // Optional override for systems that need the whole entity list (pairwise tests etc).
        // Added so derived systems can provide list-level behaviour without compile errors.
        public virtual void OnAction(List<Entity> entities) { }

        // Property signatures: 
        public string Name
        {
            get;
        }
    }
}
