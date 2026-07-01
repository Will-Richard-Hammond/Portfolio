using System;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using OpenGL_Game.OBJLoader;
using OpenGL_Game.Scenes;

namespace OpenGL_Game.Components
{
    // Base shader component
    class ComponentShader : IComponent
    {
        protected int pgmID = 0;
        protected int uniform_stex = 0;
        protected int uniform_mmodelviewproj = 0;
        protected int uniform_mmodel = 0;
        protected int uniform_diffuse = 0;

        public ComponentShader(string vertFile, string fragFile)
        {
            pgmID = GL.CreateProgram();
            LoadShader(vertFile, ShaderType.VertexShader, pgmID, out int vsID);
            LoadShader(fragFile, ShaderType.FragmentShader, pgmID, out int fsID);
            GL.LinkProgram(pgmID);

            GL.GetProgram(pgmID, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(pgmID);
                System.Console.WriteLine(infoLog);
            }

            uniform_stex = GL.GetUniformLocation(pgmID, "s_texture");
            uniform_mmodelviewproj = GL.GetUniformLocation(pgmID, "ModelViewProjMat");
            uniform_mmodel = GL.GetUniformLocation(pgmID, "ModelMat");
            uniform_diffuse = GL.GetUniformLocation(pgmID, "v_diffuse");
        }

        void LoadShader(String filename, ShaderType type, int program, out int address)
        {
            address = GL.CreateShader(type);
            using (System.IO.StreamReader sr = new System.IO.StreamReader(filename))
            {
                GL.ShaderSource(address, sr.ReadToEnd());
            }
            GL.CompileShader(address);

            GL.GetShader(address, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(address);
                System.Console.WriteLine(infoLog);
            }

            GL.AttachShader(program, address);
        }

        public virtual ComponentTypes ComponentType { get { return ComponentTypes.COMPONENT_SHADER; } }

        public int ProgramID { get { return pgmID; } }
        public int UniformDiffuse { get { return uniform_diffuse; } }

        public virtual void ApplyShader(Matrix4 model, Geometry geometry)
        {
            var scene = GameScene.gameInstance;
            if (scene?.camera == null)
                return;

            ApplyShader(model, scene.camera.view, scene.camera.projection, geometry);
        }

        public virtual void ApplyShader(Matrix4 model, Matrix4 view, Matrix4 projection, Geometry geometry)
        {
            // default behaviour: bind program and set common uniforms
            GL.UseProgram(pgmID);
            GL.Uniform1(uniform_stex, 0);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.UniformMatrix4(uniform_mmodel, false, ref model);
            // use fully qualified reference to GameScene
            Matrix4 modelViewProjection = model * view * projection;
            GL.UniformMatrix4(uniform_mmodelviewproj, false, ref modelViewProjection);
            // leave program bound for geometry.Render to use
        }
    }
}
