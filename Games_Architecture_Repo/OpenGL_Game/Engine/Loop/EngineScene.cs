using OpenTK.Windowing.Common;
using OpenGL_Game.Managers;

namespace OpenGL_Game.Engine.Loop
{
    abstract class EngineScene
    {
        protected readonly SceneManager sceneManager;

        protected EngineScene(SceneManager sceneManager)
        {
            this.sceneManager = sceneManager;
        }

        public abstract void Render(FrameEventArgs e);
        public abstract void Update(FrameEventArgs e);
        public abstract void Close();
    }
}