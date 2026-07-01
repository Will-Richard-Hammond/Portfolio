using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace OpenGL_Game.Components
{
    // Simple scale-only component
    class ComponentScale : IComponent
    {
        public Vector3 Scale { get; set; }

        public ComponentScale(float uniform) : this(uniform, uniform, uniform) { }

        public ComponentScale(float x, float y, float z)
        {
            Scale = new Vector3(x, y, z);
        }

        public ComponentScale(Vector3 scale)
        {
            Scale = scale;
        }

        // Returning COMPONENT_NONE so existing mask logic is unaffected.
        public ComponentTypes ComponentType { get { return ComponentTypes.COMPONENT_NONE; } }
    }
}
