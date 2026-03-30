using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace Darkness.Game.Input
{
    public class InputManager
    {
        private KeyboardState _currentKeyboardState;
        private KeyboardState _lastKeyboardState;
        private readonly Func<KeyboardState> _keyboardStateGetter;

        private TouchCollection _currentTouchState;
        private TouchCollection _lastTouchState;
        private readonly Func<TouchCollection> _touchStateGetter;

        public InputManager(Func<KeyboardState>? keyboardStateGetter = null, Func<TouchCollection>? touchStateGetter = null)
        {
            _keyboardStateGetter = keyboardStateGetter ?? Keyboard.GetState;
            _touchStateGetter = touchStateGetter ?? SafeGetTouchState;
        }

        private TouchCollection SafeGetTouchState()
        {
            try
            {
                return TouchPanel.GetState();
            }
            catch
            {
                return new TouchCollection();
            }
        }

        public void Update()
        {
            _lastKeyboardState = _currentKeyboardState;
            _currentKeyboardState = _keyboardStateGetter();

            _lastTouchState = _currentTouchState;
            _currentTouchState = _touchStateGetter();
        }

        public bool IsKeyJustPressed(Keys key)
        {
            return _currentKeyboardState.IsKeyDown(key) && _lastKeyboardState.IsKeyUp(key);
        }

        public bool IsKeyDown(Keys key)
        {
            return _currentKeyboardState.IsKeyDown(key);
        }

        public bool IsTouchJustPressed()
        {
            return _currentTouchState.Any(t => t.State == TouchLocationState.Pressed);
        }

        public bool IsTouchJustReleased()
        {
            return _currentTouchState.Any(t => t.State == TouchLocationState.Released);
        }

        public bool IsTouched()
        {
            return _currentTouchState.Any(t => t.State == TouchLocationState.Pressed || t.State == TouchLocationState.Moved);
        }

        public bool IsRegionJustPressed(Rectangle region)
        {
            return _currentTouchState.Any(t => t.State == TouchLocationState.Pressed && region.Contains(t.Position));
        }

        public bool IsRegionTouched(Rectangle region)
        {
            return _currentTouchState.Any(t => (t.State == TouchLocationState.Pressed || t.State == TouchLocationState.Moved) && region.Contains(t.Position));
        }

        public TouchCollection GetActiveTouches()
        {
            return _currentTouchState;
        }

        public Vector2 GetTouchPosition()
        {
            if (_currentTouchState.Count > 0)
            {
                return _currentTouchState[0].Position;
            }
            return Vector2.Zero;
        }
    }
}
