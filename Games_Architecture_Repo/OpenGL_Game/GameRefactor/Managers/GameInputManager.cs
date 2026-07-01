using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenGL_Game.Engine.Input;

namespace OpenGL_Game.GameRefactor.Managers
{
    class GameInputManager : InputManager
    {
        public bool FireRequested { get; private set; }
        public bool ToggleBirdseyeRequested { get; private set; }
        public bool TogglePauseRequested { get; private set; }
        public bool ToggleDroneMovementRequested { get; private set; }
        public bool ToggleWallCollisionRequested { get; private set; }

        public override void OnKeyDown(Keys key)
        {
            base.OnKeyDown(key);

            switch (key)
            {
                case Keys.B:
                    ToggleBirdseyeRequested = true;
                    break;
                case Keys.P:
                    TogglePauseRequested = true;
                    break;
                case Keys.M:
                    ToggleDroneMovementRequested = true;
                    break;
                case Keys.C:
                    ToggleWallCollisionRequested = true;
                    break;
            }
        }

        public void RequestFire()
        {
            FireRequested = true;
        }

        public void ClearFrameRequests()
        {
            FireRequested = false;
            ToggleBirdseyeRequested = false;
            TogglePauseRequested = false;
            ToggleDroneMovementRequested = false;
            ToggleWallCollisionRequested = false;
        }
    }
}