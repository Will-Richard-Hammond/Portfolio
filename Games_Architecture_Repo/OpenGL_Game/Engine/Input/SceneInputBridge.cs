using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenGL_Game.GameRefactor.Managers;
using OpenGL_Game.Managers;

namespace OpenGL_Game.Engine.Input
{
    class SceneInputBridge
    {
        readonly SceneManager sceneManager;
        readonly InputManager inputManager;

        public SceneInputBridge(SceneManager sceneManager, InputManager inputManager)
        {
            this.sceneManager = sceneManager;
            this.inputManager = inputManager;
        }

        public void Attach()
        {
            sceneManager.keyboardDownDelegate += OnKeyDown;
            sceneManager.keyboardUpDelegate += OnKeyUp;
            sceneManager.mouseMoveDelegate += OnMouseMove;
            sceneManager.mouseDelegate += OnMouseDown;
        }

        public void Detach()
        {
            sceneManager.keyboardDownDelegate -= OnKeyDown;
            sceneManager.keyboardUpDelegate -= OnKeyUp;
            sceneManager.mouseMoveDelegate -= OnMouseMove;
            sceneManager.mouseDelegate -= OnMouseDown;
        }

        void OnKeyDown(KeyboardKeyEventArgs e) => inputManager.OnKeyDown(e.Key);
        void OnKeyUp(KeyboardKeyEventArgs e) => inputManager.OnKeyUp(e.Key);
        void OnMouseMove(MouseMoveEventArgs e) => inputManager.OnMouseMove(e);

        void OnMouseDown(MouseButtonEventArgs e)
        {
            if (inputManager is GameInputManager gameInput && e.Button == MouseButton.Left)
                gameInput.RequestFire();
        }
    }
}