using OpenTK.Windowing.Common;
using OpenGL_Game.Managers;

namespace OpenGL_Game.Engine.Loop
{
    class EngineGameLoop
    {
        readonly SceneManager sceneManager;
        readonly EngineScene scene;

        public EngineGameLoop(SceneManager sceneManager, EngineScene scene)
        {
            this.sceneManager = sceneManager;
            this.scene = scene;
            sceneManager.renderer = scene.Render;
            sceneManager.updater = scene.Update;
        }

        public void Close()
        {
            scene.Close();
        }
    }
}