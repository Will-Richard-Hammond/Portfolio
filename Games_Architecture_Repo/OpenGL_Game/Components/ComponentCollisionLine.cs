using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace OpenGL_Game.Components
{
    // Line segment stored as local endpoints (relative to entity position).
    // Systems will project into 2D (X,Z) for segment intersection tests.
    class ComponentCollisionLine : IComponent
    {
        Vector3 localStart;
        Vector3 localEnd;

        public ComponentCollisionLine(Vector3 localStart, Vector3 localEnd)
        {
            this.localStart = localStart;
            this.localEnd = localEnd;
        }

        public Vector3 LocalStart
        {
            get { return localStart; }
            set { localStart = value; }
        }

        public Vector3 LocalEnd
        {
            get { return localEnd; }
            set { localEnd = value; }
        }

        public ComponentTypes ComponentType
        {
            get { return ComponentTypes.COMPONENT_COLLISION_LINE; }
        }
    }
}

