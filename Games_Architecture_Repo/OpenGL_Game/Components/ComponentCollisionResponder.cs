using System;

namespace OpenGL_Game.Components
{
    // Lightweight component that lets an entity own a collision response.
    // The responder is invoked by scene-level detection code:
    //   var responder = entity.Components.Find(c => c is ComponentCollisionResponder) as ComponentCollisionResponder;
    //   responder?.OnCollision(entity, otherEntity);
    class ComponentCollisionResponder : IComponent
    {
        public Action<object, object> OnCollision; // (selfEntity, otherEntity) - use object to avoid circular type refs

        public ComponentCollisionResponder(Action<object, object> onCollision)
        {
            OnCollision = onCollision;
        }

        public ComponentTypes ComponentType => ComponentTypes.COMPONENT_NONE;
    }
}
