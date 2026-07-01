// [REFACTOR] Superseded by Engine.Systems.RenderSystem.
// RenderSystem iterates EngineEntity objects, caches geometry via ResourceManager.LoadGeometry()
// and caches one ComponentShader per entity, then renders using camera matrices supplied by
// a delegate — eliminating the per-frame shader/geometry allocations that caused the memory leak.
// This class is retained so the legacy GameScene system list still compiles.

using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL;
using OpenGL_Game.Components;
using OpenGL_Game.OBJLoader;
using OpenGL_Game.Objects;
using OpenGL_Game.Scenes;
using OpenTK.Mathematics;

namespace OpenGL_Game.Systems
{
    class SystemRender : System
    {
        const ComponentTypes MASK = (ComponentTypes.COMPONENT_POSITION | ComponentTypes.COMPONENT_GEOMETRY);

        protected int pgmID;
        protected int vsID;
        protected int fsID;
        protected int uniform_stex;
        protected int uniform_mmodelviewproj;
        protected int uniform_mmodel;
        protected int uniform_diffuse;

        readonly Func<(Matrix4 view, Matrix4 projection)?> cameraMatricesProvider;

        public SystemRender(Func<(Matrix4 view, Matrix4 projection)?> cameraMatricesProvider = null)
        {
            this.cameraMatricesProvider = cameraMatricesProvider;

            pgmID = GL.CreateProgram();
            LoadShader("Shaders/single-light.vert", ShaderType.VertexShader, pgmID, out vsID);
            LoadShader("Shaders/single-light.frag", ShaderType.FragmentShader, pgmID, out fsID);
            GL.LinkProgram(pgmID);

            GL.GetProgram(pgmID, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(pgmID);
                Console.WriteLine(infoLog);
            }

            Console.WriteLine(GL.GetProgramInfoLog(pgmID));

            uniform_stex = GL.GetUniformLocation(pgmID, "s_texture");
            uniform_mmodelviewproj = GL.GetUniformLocation(pgmID, "ModelViewProjMat");
            uniform_mmodel = GL.GetUniformLocation(pgmID, "ModelMat");
            uniform_diffuse = GL.GetUniformLocation(pgmID, "v_diffuse");
        }

        void LoadShader(String filename, ShaderType type, int program, out int address)
        {
            address = GL.CreateShader(type);
            using (StreamReader sr = new StreamReader(filename))
            {
                GL.ShaderSource(address, sr.ReadToEnd());
            }
            GL.CompileShader(address);

            GL.GetShader(address, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(address);
                Console.WriteLine(infoLog);
            }

            GL.AttachShader(program, address);
        }

        public string Name
        {
            get { return "SystemRender"; }
        }

        // [REFACTOR] OnAction body superseded by Engine.Systems.RenderSystem.OnUpdate().
        // RenderSystem reads TransformComponent + RenderComponent + ScaleComponent from EngineEntity,
        // uses ResourceManager.LoadGeometry() for cached geometry, and shaderCache for cached shaders.
        public override void OnAction(Entity entity)
        {
            /*
            if ((entity.Mask & MASK) != MASK) return;

            var cameraMatrices = GetCameraMatrices();
            if (!cameraMatrices.HasValue) return;

            IComponent geometryComponent = GetComponent(entity, ComponentTypes.COMPONENT_GEOMETRY);
            Geometry geometry = ((ComponentGeometry)geometryComponent).Geometry();

            IComponent positionComponent = GetComponent(entity, ComponentTypes.COMPONENT_POSITION);
            Vector3 position = ((ComponentPosition)positionComponent).Position;

            Matrix4 view = cameraMatrices.Value.view;
            Matrix4 projection = cameraMatrices.Value.projection;

            Matrix4 model;
            var scaleComp = entity.Components.Find(c => c is ComponentScale) as ComponentScale;
            if (scaleComp != null)
                model = Matrix4.CreateScale(scaleComp.Scale) * Matrix4.CreateTranslation(position);
            else
                model = Matrix4.CreateTranslation(position);

            IComponent shaderComponent = GetComponent(entity, ComponentTypes.COMPONENT_SHADER);
            if (shaderComponent is ComponentShader compShader)
            {
                if (compShader is ComponentShaderSkybox skyShader)
                    skyShader.RenderSkybox(model, view, projection, geometry);
                else
                {
                    compShader.ApplyShader(model, view, projection, geometry);
                    geometry.Render(compShader.UniformDiffuse);
                    GL.UseProgram(0);
                }
            }
            else
            {
                Draw(model, view, projection, geometry);
            }
            */
        }

        // [REFACTOR] Superseded by RenderSystem camera delegate injection.
        /*
        (Matrix4 view, Matrix4 projection)? GetCameraMatrices()
        {
            if (cameraMatricesProvider != null)
                return cameraMatricesProvider();
            var scene = GameScene.gameInstance;
            if (scene?.camera == null) return null;
            return (scene.camera.view, scene.camera.projection);
        }
        */

        // [REFACTOR] Superseded by RenderSystem.OnUpdate() draw path.
        /*
        public void Draw(Matrix4 model, Matrix4 view, Matrix4 projection, Geometry geometry)
        {
            GL.UseProgram(pgmID);
            GL.Uniform1(uniform_stex, 0);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.UniformMatrix4(uniform_mmodel, false, ref model);
            Matrix4 modelViewProjection = model * view * projection;
            GL.UniformMatrix4(uniform_mmodelviewproj, false, ref modelViewProjection);
            geometry.Render(uniform_diffuse);
            GL.UseProgram(0);
        }
        */
    }
}
