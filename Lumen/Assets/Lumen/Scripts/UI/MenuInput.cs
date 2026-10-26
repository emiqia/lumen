using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Lumen.UI
{
    /// <summary>One frame of menu/meta input (keyboard + gamepad), read in a single place.</summary>
    public readonly struct MenuInput
    {
        public readonly bool Confirm, Back, Pause, Retry, Up, Down, Left, Right;

        MenuInput(bool confirm, bool back, bool pause, bool retry, bool up, bool down, bool left, bool right)
        {
            Confirm = confirm; Back = back; Pause = pause; Retry = retry;
            Up = up; Down = down; Left = left; Right = right;
        }

        static Vector2 lastStick;

        public static MenuInput Read()
        {
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            bool K(System.Func<Keyboard, KeyControl> key) => kb != null && key(kb).wasPressedThisFrame;
            bool P(System.Func<Gamepad, ButtonControl> button) => pad != null && button(pad).wasPressedThisFrame;

            // The stick acts like a d-pad: one step per push past the threshold.
            Vector2 stick = pad != null ? pad.leftStick.ReadValue() : Vector2.zero;
            bool StickEdge(Vector2 dir) => Vector2.Dot(stick, dir) > 0.6f && Vector2.Dot(lastStick, dir) <= 0.6f;
            bool sUp = StickEdge(Vector2.up), sDown = StickEdge(Vector2.down), sLeft = StickEdge(Vector2.left), sRight = StickEdge(Vector2.right);
            lastStick = stick;

            return new MenuInput(
                confirm: K(k => k.enterKey) || K(k => k.numpadEnterKey) || K(k => k.spaceKey) || P(p => p.startButton) || P(p => p.buttonSouth),
                back: K(k => k.escapeKey) || K(k => k.backspaceKey) || P(p => p.buttonEast) || P(p => p.selectButton),
                pause: K(k => k.escapeKey) || P(p => p.startButton),
                // Gamepad retries through GameOverConfirm (Start only): A is the flare and gets mashed while dying.
                retry: K(k => k.rKey),
                up: K(k => k.upArrowKey) || K(k => k.wKey) || P(p => p.dpad.up) || sUp,
                down: K(k => k.downArrowKey) || K(k => k.sKey) || P(p => p.dpad.down) || sDown,
                left: K(k => k.leftArrowKey) || K(k => k.aKey) || P(p => p.dpad.left) || sLeft,
                right: K(k => k.rightArrowKey) || K(k => k.dKey) || P(p => p.dpad.right) || sRight);
        }

        /// <summary>Game-over confirm: keyboard Enter, gamepad Start only (never A, the flare button).</summary>
        public static bool GameOverConfirm()
        {
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            return (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) ||
                   (pad != null && pad.startButton.wasPressedThisFrame);
        }
    }
}
