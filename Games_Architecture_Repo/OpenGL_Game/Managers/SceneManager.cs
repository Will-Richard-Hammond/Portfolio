using System;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenGL_Game.GameRefactor.Scenes;
using OpenGL_Game.Scenes;

namespace OpenGL_Game.Managers
{
    delegate void SceneDelegate(FrameEventArgs e);
    delegate void KeyboardDelegate(KeyboardKeyEventArgs e);
    delegate void MouseDelegate(MouseButtonEventArgs e);
    delegate void MouseMoveDelegate(MouseMoveEventArgs e);
    delegate void TextInputDelegate(TextInputEventArgs e);

    class SceneManager : GameWindow
    {
        Scene scene;
        public static int width = 1200, height = 800;
        public static int windowXPos = 200, windowYPos = 80;

        ALDevice alDevice;
        ALContext alContext;

        public SceneDelegate renderer;
        public SceneDelegate updater;
        public KeyboardDelegate keyboardDownDelegate;
        public KeyboardDelegate keyboardUpDelegate;
        public MouseDelegate mouseDelegate;
        public MouseDelegate mouseUpDelegate;
        public MouseMoveDelegate mouseMoveDelegate;
        public TextInputDelegate textInputDelegate;

        public SceneManager() : base(GameWindowSettings.Default, new NativeWindowSettings()
        {
            ClientSize = (width, height),
            Location = (windowXPos, windowYPos)
        })
        {
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
            keyboardDownDelegate?.Invoke(e);
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);
            keyboardUpDelegate?.Invoke(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            mouseDelegate?.Invoke(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            mouseUpDelegate?.Invoke(e);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);
            mouseMoveDelegate?.Invoke(e);
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);
            textInputDelegate?.Invoke(e);
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            // Initialize OpenAL device and context before any scene creates audio resources.
            alDevice = ALC.OpenDevice(null);
            alContext = ALC.CreateContext(alDevice, (int[])null);
            ALC.MakeContextCurrent(alContext);

            GL.Enable(EnableCap.DepthTest);
            GL.DepthMask(true);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            GUI.SetUpGUI(width, height);

            StartMenu();
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            // Release the OpenAL context and device.
            ALC.MakeContextCurrent(ALContext.Null);
            ALC.DestroyContext(alContext);
            ALC.CloseDevice(alDevice);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            updater?.Invoke(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            renderer?.Invoke(e);

            GL.Flush();
            SwapBuffers();
        }

        public void StartNewGame()
        {
            if (scene != null) scene.Close();
            GUI.SetUpGUI(width, height);
            scene = new RefactorGameScene(this);
        }

        public void StartLegacyGame()
        {
            if (scene != null) scene.Close();
            GUI.SetUpGUI(width, height);
            scene = new GameScene(this);
        }

        public void StartMenu()
        {
            if (scene != null) scene.Close();
            GUI.SetUpGUI(width, height);
            scene = new MainMenuScene(this);
        }

        public void StartGameOver()
        {
            if (scene != null) scene.Close();
            GUI.SetUpGUI(width, height);
            scene = new GameOverScene(this);
        }

        public static int WindowWidth => width;
        public static int WindowHeight => height;

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, e.Width, e.Height);
            SceneManager.width = e.Width;
            SceneManager.height = e.Height;

            GUI.SetUpGUI(e.Width, e.Height);
        }

        public void ChangeScene(Scene.SceneTypes sceneType)
        {
            switch (sceneType)
            {
                case Scene.SceneTypes.SCENE_MAIN_MENU:
                    StartMenu();
                    break;
                case Scene.SceneTypes.SCENE_GAME:
                    StartNewGame();
                    break;
                case Scene.SceneTypes.SCENE_GAME_OVER:
                    StartGameOver();
                    break;
            }
        }
    }
}