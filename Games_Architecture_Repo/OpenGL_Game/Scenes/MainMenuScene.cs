using OpenTK.Graphics.OpenGL;
using OpenGL_Game.Managers;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SkiaSharp;

namespace OpenGL_Game.Scenes
{
    class MainMenuScene : Scene
    {
        public MainMenuScene(SceneManager sceneManager) : base(sceneManager)
        {
            sceneManager.Title = "Main Menu";
            sceneManager.renderer = Render;
            sceneManager.updater = Update;

            sceneManager.mouseDelegate += Mouse_BottonPressed;
            sceneManager.keyboardDownDelegate += Keyboard_KeyDown;

            GL.ClearColor(0.2f, 0.75f, 1.0f, 1.0f);
        }

        public override void Update(FrameEventArgs e)
        {
        }

        public override void Render(FrameEventArgs e)
        {
            GL.Viewport(0, 0, sceneManager.Size.X, sceneManager.Size.Y);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, sceneManager.Size.X, 0, sceneManager.Size.Y, -1, 1);

            SKPaint paint = new SKPaint();
            paint.TextSize = 90;
            paint.StrokeWidth = 2;
            paint.TextAlign = SKTextAlign.Center;
            paint.IsAntialias = true;
            paint.Color = SKColors.Yellow;
            paint.Style = SKPaintStyle.Fill;
            GUI.DrawText("Maze Drone Hunt", sceneManager.Size.X * 0.5f, 130, paint);
            paint.Color = SKColors.DarkBlue;
            paint.Style = SKPaintStyle.Stroke;
            GUI.DrawText("Maze Drone Hunt", sceneManager.Size.X * 0.5f, 130, paint);

            GUI.DrawText("Left Click = Start Game", sceneManager.Size.X * 0.5f - 140, 220, 28, 255, 255, 255);
            GUI.DrawText("WASD / Arrows = Move", sceneManager.Size.X * 0.5f - 140, 270, 24, 255, 255, 255);
            GUI.DrawText("Mouse = Look   Left Click = Fire", sceneManager.Size.X * 0.5f - 140, 305, 24, 255, 255, 255);
            GUI.DrawText("B = Birdseye   M = Drone Move   C = Wall Collision   P = Pause", sceneManager.Size.X * 0.5f - 260, 340, 24, 255, 255, 255);
            GUI.DrawText("Disable all drones to win. Lose all 3 lives and the game ends.", sceneManager.Size.X * 0.5f - 260, 390, 24, 255, 255, 255);

            GUI.Render();
        }

        public void Mouse_BottonPressed(MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Left)
                sceneManager.StartNewGame();
        }

        public void Keyboard_KeyDown(KeyboardKeyEventArgs e)
        {
            if (e.Key == Keys.Enter || e.Key == Keys.Space)
                sceneManager.StartNewGame();
        }

        public override void Close()
        {
            sceneManager.mouseDelegate -= Mouse_BottonPressed;
            sceneManager.keyboardDownDelegate -= Keyboard_KeyDown;
        }
    }
}