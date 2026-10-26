using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Lumen.UI
{
    /// <summary>One frame of menu/meta input (keyboard + gamepad), read in a single place.</summary>
    public readonly struct MenuInput
    {
        public readonly bool Confirm, Back, Pause, Language, MainMenu, Retry, Shake, Brightness, Colorblind;

        MenuInput(bool confirm, bool back, bool pause, bool language, bool mainMenu, bool retry, bool shake, bool brightness, bool colorblind)
        {
            Confirm = confirm; Back = back; Pause = pause; Language = language;
            MainMenu = mainMenu; Retry = retry; Shake = shake; Brightness = brightness; Colorblind = colorblind;
        }

        public static MenuInput Read()
        {
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            bool K(System.Func<Keyboard, KeyControl> key) => kb != null && key(kb).wasPressedThisFrame;
            bool P(System.Func<Gamepad, ButtonControl> button) => pad != null && button(pad).wasPressedThisFrame;

            return new MenuInput(
                confirm: K(k => k.enterKey) || K(k => k.numpadEnterKey) || P(p => p.startButton),
                back: K(k => k.escapeKey) || P(p => p.selectButton),
                pause: K(k => k.escapeKey) || P(p => p.startButton) || P(p => p.selectButton),
                language: K(k => k.lKey),
                mainMenu: K(k => k.qKey) || P(p => p.buttonNorth),
                // Gamepad retries with Start (Confirm): A is the flare and gets mashed while dying.
                retry: K(k => k.rKey),
                shake: K(k => k.tKey) || P(p => p.leftShoulder),
                brightness: K(k => k.bKey) || P(p => p.rightShoulder),
                colorblind: K(k => k.cKey) || P(p => p.buttonWest));
        }
    }
}
