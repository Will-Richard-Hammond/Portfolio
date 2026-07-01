using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace OpenGL_Game.Components
{
    class ComponentCollisionSphere : IComponent
    {
        float radius;

        // Radius in world units (treated as centered on the entity's ComponentPosition)
        public ComponentCollisionSphere(float radius)
        {
            this.radius = radius;
        }

        public float Radius
        {
            get { return radius; }
            set { radius = value; }
        }

        // Return the proper flag so systems / registries can find sphere components fast
        public ComponentTypes ComponentType
        {
            get { return ComponentTypes.COMPONENT_COLLISION_SPHERE; }
        }
    }   
}
