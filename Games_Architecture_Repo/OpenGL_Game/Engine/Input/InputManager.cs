using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace OpenGL_Game.Engine.Input
{
    abstract class InputManager
    {
        protected readonly bool[] keyStates = new bool[512];
        protected float mouseDeltaX;
        protected float mouseDeltaY;

        public virtual void OnKeyDown(Keys key)
        {
            int index = (int)key;
            if (index >= 0 && index < keyStates.Length)
                keyStates[index] = true;
        }

        public virtual void OnKeyUp(Keys key)
        {
            int index = (int)key;
            if (index >= 0 && index < keyStates.Length)
                keyStates[index] = false;
        }

        public virtual void OnMouseMove(MouseMoveEventArgs e)
        {
            mouseDeltaX += e.Delta.X;
            mouseDeltaY += e.Delta.Y;
        }

        public bool IsKeyDown(Keys key)
        {
            int index = (int)key;
            return index >= 0 && index < keyStates.Length && keyStates[index];
        }

        public float ConsumeMouseDeltaX()
        {
            float value = mouseDeltaX;
            mouseDeltaX = 0f;
            return value;
        }

        public float ConsumeMouseDeltaY()
        {
            float value = mouseDeltaY;
            mouseDeltaY = 0f;
            return value;
        }
    }
}