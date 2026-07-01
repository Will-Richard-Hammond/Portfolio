using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenGL_Game.Components;
using OpenGL_Game.Engine.Components;
using OpenGL_Game.Engine.Entities;
using OpenGL_Game.Engine.Managers;
using OpenGL_Game.Managers;
using OpenGL_Game.OBJLoader;

namespace OpenGL_Game.Engine.Systems
{
    class RenderSystem : EntityEngineSystem
    {
        readonly System.Func<(Matrix4 view, Matrix4 projection)?> cameraMatricesProvider;

        // Shader instances are expensive (GL.CreateProgram + GL.CreateShader each time).
        // Cache one per entity name so they are created once and reused every frame.
        readonly Dictionary<string, ComponentShader> shaderCache = new();

        public RenderSystem(System.Func<(Matrix4 view, Matrix4 projection)?> cameraMatricesProvider)
        {
            this.cameraMatricesProvider = cameraMatricesProvider;
        }

        protected override void OnUpdate(EngineEntity entity, float deltaTime)
        {
            var cameraMatrices = cameraMatricesProvider?.Invoke();
            if (!cameraMatrices.HasValue)
                return;

            var transform = entity.GetComponent<TransformComponent>();
            var render = entity.GetComponent<RenderComponent>();
            if (transform == null || render == null)
                return;

            Matrix4 model = Matrix4.CreateTranslation(transform.Position);
            var scale = entity.GetComponent<ScaleComponent>();
            if (scale != null)
                model = Matrix4.CreateScale(scale.Scale) * model;

            // Apply rotation if it is not the identity quaternion.
            if (transform.Rotation != Quaternion.Identity)
            {
                Matrix4 rot = Matrix4.CreateFromQuaternion(transform.Rotation);
                // TRS order: Scale * Rotation * Translation
                model = Matrix4.CreateScale(scale?.Scale ?? Vector3.One)
                      * rot
                      * Matrix4.CreateTranslation(transform.Position);
            }

            // Geometry: use ResourceManager cache — LoadGeometry returns the same
            // Geometry instance (with its VAO/VBOs) for the same path every frame.
            Geometry geometry = ResourceManager.LoadGeometry(render.GeometryPath);

            // Shader: create once per entity and reuse — creating a new ComponentShader
            // every frame calls GL.CreateProgram and GL.CreateShader, leaking GPU objects.
            if (!shaderCache.TryGetValue(entity.Name, out ComponentShader shader))
            {
                shader = render.ShaderKey == "Skybox"
                    ? new ComponentShaderSkybox(render.TexturePath)
                    : new ComponentShaderDefault();
                shaderCache[entity.Name] = shader;
            }

            if (shader is ComponentShaderSkybox skyboxShader)
            {
                skyboxShader.RenderSkybox(model, cameraMatrices.Value.view, cameraMatrices.Value.projection, geometry);
            }
            else
            {
                shader.ApplyShader(model, cameraMatrices.Value.view, cameraMatrices.Value.projection, geometry);
                geometry.Render(shader.UniformDiffuse);
                GL.UseProgram(0);
            }
        }
    }
}
