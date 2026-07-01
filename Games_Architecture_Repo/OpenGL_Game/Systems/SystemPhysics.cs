// [REFACTOR] Superseded by Engine.Systems.MovementSystem.
// MovementSystem iterates EngineEntity objects with TransformComponent + VelocityComponent
// and applies velocity * dt each frame via EngineSystemManager.UpdateAll().
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
    class SystemPhysics : System
    {
        const ComponentTypes MASK = (ComponentTypes.COMPONENT_POSITION | ComponentTypes.COMPONENT_VELOCITY);

        // [REFACTOR] Shader fields and constructor shader setup below are superseded by
        // Engine.Systems.RenderSystem which caches one ComponentShader per entity.
        protected int pgmID;
        protected int vsID;
        protected int fsID;
        protected int uniform_stex;
        protected int uniform_mmodelviewproj;
        protected int uniform_mmodel;
        protected int uniform_diffuse;

        public SystemPhysics()
        {
            // [REFACTOR] Shader program creation here is legacy-only.
            // RenderSystem in the engine layer owns shader lifetime via shaderCache.
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
            get { return "SystemPhysics"; }
        }

        // [REFACTOR] OnAction body superseded by Engine.Systems.MovementSystem.Update(),
        // which applies VelocityComponent.Velocity * dt to TransformComponent.Position.
        public override void OnAction(Entity entity)
        {
            /*
            ComponentPosition position = null;
            ComponentVelocity velocity = null;
            if ((entity.Mask & MASK) == MASK)
            {
                foreach (var component in entity.Components)
                {
                    if (component is ComponentPosition pos) position = pos;
                    else if (component is ComponentVelocity vel) velocity = vel;
                }
                if (position != null && velocity != null)
                    Motion(position, velocity);
            }
            */
        }

        // [REFACTOR] Superseded by MovementSystem — transform.Position += velocity.Velocity * deltaTime.
        /*
        public void Motion(ComponentPosition position, ComponentVelocity velocity)
        {
            float dt = GameScene.dt;
            position.Position += velocity.Velocity * dt;
        }
        */
    }
}
