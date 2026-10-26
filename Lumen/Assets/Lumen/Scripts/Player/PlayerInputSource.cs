using UnityEngine;
using UnityEngine.InputSystem;

namespace Lumen.Player
{
    /// <summary>
    /// Abstraction over "who is controlling the flame". The keyboard/gamepad and the
    /// autopilot bot both implement it, which is how automated playtests drive the real game.
    /// </summary>
    public interface IPlayerInput
    {
        Vector2 Move { get; }
        bool Dim { get; }
        bool Sprint { get; }
        bool FlarePressed { get; }   // true only on the frame it was pressed
        bool UseItemPressed { get; } // true only on the frame it was pressed
        void Tick();
    }

    public class DeviceInput : IPlayerInput
    {
        public Vector2 Move { get; private set; }
        public bool Dim { get; private set; }
        public bool Sprint { get; private set; }
        public bool FlarePressed { get; private set; }
        public bool UseItemPressed { get; private set; }

        public void Tick()
        {
            Vector2 move = Vector2.zero;
            bool dim = false, sprint = false, flare = false, use = false;

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1;
                sprint |= kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                dim |= kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
                flare |= kb.spaceKey.wasPressedThisFrame;
                use |= kb.eKey.wasPressedThisFrame;
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                Vector2 stick = pad.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.04f) move += stick;
                move += pad.dpad.ReadValue();
                dim |= pad.leftTrigger.isPressed;
                sprint |= pad.rightTrigger.isPressed;
                flare |= pad.buttonSouth.wasPressedThisFrame;
                use |= pad.buttonNorth.wasPressedThisFrame;
            }

            Move = Vector2.ClampMagnitude(move, 1f);
            Dim = dim;
            Sprint = sprint && !dim; // sneaking wins over running
            FlarePressed = flare;
            UseItemPressed = use;
        }
    }
}
