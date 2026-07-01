using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using OpenGL_Game.OBJLoader;

namespace OpenGL_Game.Components
{
    class ComponentShaderNoLights : ComponentShader
    {
        public ComponentShaderNoLights() : base("Shaders/text.vert", "Shaders/text.frag")
        {
        }

        public override void ApplyShader(Matrix4 model, Geometry geometry)
        {
            base.ApplyShader(model, geometry);
            // For no-light shader, set a default diffuse uniform
            GL.Uniform3(uniform_diffuse, new OpenTK.Mathematics.Vector3(1.0f, 1.0f, 1.0f));
        }
    }
}
