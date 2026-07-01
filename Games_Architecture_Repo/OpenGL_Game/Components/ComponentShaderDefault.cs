using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using OpenGL_Game.OBJLoader;

namespace OpenGL_Game.Components
{
    class ComponentShaderDefault : ComponentShader
    {
        public ComponentShaderDefault() : base("Shaders/single-light.vert", "Shaders/single-light.frag")
        {
        }

        public override void ApplyShader(Matrix4 model, Geometry geometry)
        {
            base.ApplyShader(model, geometry);
            // Could set additional uniforms specific to this shader
            // GL.Uniform3(uniform_diffuse, new OpenTK.Mathematics.Vector3(1.0f,1.0f,1.0f));
        }
    }
}
