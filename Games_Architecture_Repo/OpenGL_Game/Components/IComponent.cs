using System;

namespace OpenGL_Game.Components
{
    [FlagsAttribute]
    enum ComponentTypes {
        COMPONENT_NONE     = 0,
	    COMPONENT_POSITION = 1 << 0,
        COMPONENT_GEOMETRY = 1 << 1,
        COMPONENT_VELOCITY = 1 << 2,
        COMPONENT_SHADER   = 1 << 3,
        COMPONENT_AUDIO    = 1 << 4,

        // Collision component flags (added)
        COMPONENT_COLLISION_SPHERE = 1 << 8,
        COMPONENT_COLLISION_AABB   = 1 << 9,
        COMPONENT_COLLISION_LINE   = 1 << 10
    }

    interface IComponent
    {
        ComponentTypes ComponentType
        {
            get;
        }
    }
}
