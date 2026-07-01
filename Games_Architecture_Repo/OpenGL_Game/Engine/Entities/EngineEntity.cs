using System.Collections.Generic;
using System.Diagnostics;
using OpenGL_Game.Engine.Components;

namespace OpenGL_Game.Engine.Entities
{
    class EngineEntity
    {
        readonly List<IEngineComponent> components = new();
        EngineComponentType mask;

        public EngineEntity(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public EngineComponentType Mask => mask;

        public IReadOnlyList<IEngineComponent> Components => components;

        public void AddComponent(IEngineComponent component)
        {
            Debug.Assert(component != null, "Component cannot be null");
            components.Add(component);
            mask |= component.ComponentType;
        }

        public T GetComponent<T>() where T : class, IEngineComponent
        {
            foreach (var component in components)
            {
                if (component is T typed)
                    return typed;
            }

            return null;
        }
    }
}