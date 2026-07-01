using Microsoft.Xna.Framework.Input;

namespace MGGameLibrary.Graphs
{
    /// <summary>
    /// Fires once when the specified key transitions from not-pressed to pressed.
    /// </summary>
    public class KeyPressTransition : IStateTransition
    {
        private bool _oldKeyState;
        public Keys Key { get; private set; }

        public KeyPressTransition(Keys key)
        {
            Key          = key;
            _oldKeyState = Keyboard.GetState().IsKeyDown(Key);
        }

        public bool ToTransition()
        {
            if (!_oldKeyState && Keyboard.GetState().IsKeyDown(Key))
            {
                _oldKeyState = true;
                return true;
            }
            _oldKeyState = Keyboard.GetState().IsKeyDown(Key);
            return false;
        }
    }

    /// <summary>
    /// Fires once when the specified key transitions from pressed to not-pressed.
    /// </summary>
    public class KeyReleaseTransition : IStateTransition
    {
        private bool _oldKeyState;
        public Keys Key { get; private set; }

        public KeyReleaseTransition(Keys key)
        {
            Key          = key;
            _oldKeyState = Keyboard.GetState().IsKeyDown(Key);
        }

        public bool ToTransition()
        {
            if (_oldKeyState && !Keyboard.GetState().IsKeyDown(Key))
            {
                _oldKeyState = false;
                return true;
            }
            _oldKeyState = Keyboard.GetState().IsKeyDown(Key);
            return false;
        }
    }
}
