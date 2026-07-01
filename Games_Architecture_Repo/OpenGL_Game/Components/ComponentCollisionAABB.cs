using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace OpenGL_Game.Components
{
    // Axis-aligned bounding box stored as local min/max offsets from the entity position
    class ComponentCollisionAABB : IComponent
    {
        Vector3 localMin;
        Vector3 localMax;

        public ComponentCollisionAABB(Vector3 localMin, Vector3 localMax)
        {
            this.localMin = localMin;
            this.localMax = localMax;
        }

        public Vector3 LocalMin
        {
            get { return localMin; }
            set { localMin = value; }
        }

        public Vector3 LocalMax
        {
            get { return localMax; }
            set { localMax = value; }
        }

        public ComponentTypes ComponentType
        {
            get { return ComponentTypes.COMPONENT_COLLISION_AABB; }
        }
    }
}

