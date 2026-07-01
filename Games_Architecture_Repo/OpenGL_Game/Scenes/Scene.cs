using OpenTK.Windowing.Common;
using OpenGL_Game.Managers;

namespace OpenGL_Game.Scenes
{
    abstract class Scene
    {
        protected SceneManager sceneManager;
        public enum SceneTypes
        {
            SCENE_NONE,
            SCENE_MAIN_MENU,
            SCENE_GAME,
            SCENE_GAME_OVER
        }
        public Scene(SceneManager sceneManager)
        {
            this.sceneManager = sceneManager;
        }

        public abstract void Render(FrameEventArgs e);

        public abstract void Update(FrameEventArgs e);

        public abstract void Close();
    }
}
