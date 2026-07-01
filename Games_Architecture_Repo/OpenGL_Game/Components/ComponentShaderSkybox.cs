using System;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using OpenGL_Game.OBJLoader;
using OpenGL_Game.Managers;

namespace OpenGL_Game.Components
{
    class ComponentShaderSkybox : ComponentShader
    {
        private int skyTexture = 0;

        public ComponentShaderSkybox(string texturePath = null) : base("Shaders/skybox.vert", "Shaders/skybox.frag")
        {
            if (!string.IsNullOrEmpty(texturePath))
            {
                try
                {
                    skyTexture = ResourceManager.LoadTexture(texturePath);
                }
                catch (Exception)
                {
                    skyTexture = 0;
                }
            }
        }

        public void RenderSkybox(Matrix4 model, Matrix4 view, Matrix4 projection, Geometry geometry)
        {
            GL.GetInteger(GetPName.CullFaceMode, out int prevCull);
            GL.GetInteger(GetPName.DepthFunc, out int prevDepthFunc);

            GL.DepthFunc(DepthFunction.Lequal);
            GL.DepthMask(false);
            GL.CullFace(CullFaceMode.Front);

            GL.UseProgram(pgmID);
            GL.Uniform1(uniform_stex, 0);
            GL.ActiveTexture(TextureUnit.Texture0);

            view.M41 = 0f;
            view.M42 = 0f;
            view.M43 = 0f;

            Matrix4 modelNoTranslation = model;
            modelNoTranslation.M41 = 0f;
            modelNoTranslation.M42 = 0f;
            modelNoTranslation.M43 = 0f;

            GL.UniformMatrix4(uniform_mmodel, false, ref modelNoTranslation);
            Matrix4 modelViewProjection = modelNoTranslation * view * projection;
            GL.UniformMatrix4(uniform_mmodelviewproj, false, ref modelViewProjection);

            if (skyTexture != 0)
            {
                geometry.Render(uniform_diffuse, skyTexture);
            }
            else
            {
                geometry.Render(uniform_diffuse);
            }

            GL.UseProgram(0);

            GL.DepthMask(true);
            GL.CullFace((CullFaceMode)prevCull);
            GL.DepthFunc((DepthFunction)prevDepthFunc);
        }
    }
}
