using System;
using ImGuiNET;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityEssentials
{
    /// <summary>
    /// Feeds Unity Input System state into ImGui IO.
    /// - Mouse (pos, buttons, wheel)
    /// - Keyboard (full key mapping)
    /// - Text input (proper IME/locale via onTextInput)
    /// - Optional gamepad navigation
    /// </summary>
    internal static class ImGuiInput
    {
        private static bool s_initialized;
        private static Action<char> s_textInputHandler;

        public static void Initialize(ImGuiIOPtr io)
        {
            if (s_initialized)
                return;

            // Text input (correct way; supports layout/IME/dead keys)
            s_textInputHandler = static ch =>
            {
                // ImGui ignores '\0'. Filter control chars except newline/tab if you want.
                if (ch == '\0') return;
                ImGui.GetIO().AddInputCharacter(ch);
            };

            if (Keyboard.current != null)
                Keyboard.current.onTextInput += s_textInputHandler;

            s_initialized = true;
        }

        public static void Shutdown()
        {
            if (!s_initialized)
                return;

            if (Keyboard.current != null && s_textInputHandler != null)
                Keyboard.current.onTextInput -= s_textInputHandler;

            s_textInputHandler = null;
            s_initialized = false;
        }

        public static void UpdateIo(ImGuiIOPtr io, bool enableGamepad = true)
        {
            if (!s_initialized)
                Initialize(io);

            UpdateMouse(io);
            UpdateKeyboard(io);
            if (enableGamepad)
                UpdateGamepad(io);
        }

        private static void UpdateMouse(ImGuiIOPtr io)
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            // Mouse position: Unity bottom-left -> ImGui top-left
            var pos = mouse.position.ReadValue();
            io.AddMousePosEvent(pos.x, Screen.height - pos.y);

            io.AddMouseButtonEvent(0, mouse.leftButton.isPressed);
            io.AddMouseButtonEvent(1, mouse.rightButton.isPressed);
            io.AddMouseButtonEvent(2, mouse.middleButton.isPressed);

            // Extra buttons (Mouse4 / Mouse5)
            if (mouse.backButton != null)
                io.AddMouseButtonEvent(3, mouse.backButton.isPressed);
            if (mouse.forwardButton != null)
                io.AddMouseButtonEvent(4, mouse.forwardButton.isPressed);

            // Scroll (Input System reports per-frame delta)
            var scroll = mouse.scroll.ReadValue();
            io.AddMouseWheelEvent(scroll.x, scroll.y);
        }

        private static void UpdateKeyboard(ImGuiIOPtr io)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            // Modifiers
            var ctrl = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
            var alt = keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed;
            var shift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            var super = keyboard.leftMetaKey.isPressed || keyboard.rightMetaKey.isPressed;

            io.AddKeyEvent(ImGuiKey.ModCtrl, ctrl);
            io.AddKeyEvent(ImGuiKey.ModAlt, alt);
            io.AddKeyEvent(ImGuiKey.ModShift, shift);
            io.AddKeyEvent(ImGuiKey.ModSuper, super);

            // Navigation / control
            io.AddKeyEvent(ImGuiKey.Tab, keyboard.tabKey.isPressed);
            io.AddKeyEvent(ImGuiKey.LeftArrow, keyboard.leftArrowKey.isPressed);
            io.AddKeyEvent(ImGuiKey.RightArrow, keyboard.rightArrowKey.isPressed);
            io.AddKeyEvent(ImGuiKey.UpArrow, keyboard.upArrowKey.isPressed);
            io.AddKeyEvent(ImGuiKey.DownArrow, keyboard.downArrowKey.isPressed);
            io.AddKeyEvent(ImGuiKey.PageUp, keyboard.pageUpKey.isPressed);
            io.AddKeyEvent(ImGuiKey.PageDown, keyboard.pageDownKey.isPressed);
            io.AddKeyEvent(ImGuiKey.Home, keyboard.homeKey.isPressed);
            io.AddKeyEvent(ImGuiKey.End, keyboard.endKey.isPressed);
            io.AddKeyEvent(ImGuiKey.Insert, keyboard.insertKey.isPressed);
            io.AddKeyEvent(ImGuiKey.Delete, keyboard.deleteKey.isPressed);
            io.AddKeyEvent(ImGuiKey.Backspace, keyboard.backspaceKey.isPressed);
            io.AddKeyEvent(ImGuiKey.Space, keyboard.spaceKey.isPressed);
            io.AddKeyEvent(ImGuiKey.Enter, keyboard.enterKey.isPressed);
            io.AddKeyEvent(ImGuiKey.Escape, keyboard.escapeKey.isPressed);

            // Letters
            io.AddKeyEvent(ImGuiKey.A, keyboard.aKey.isPressed);
            io.AddKeyEvent(ImGuiKey.B, keyboard.bKey.isPressed);
            io.AddKeyEvent(ImGuiKey.C, keyboard.cKey.isPressed);
            io.AddKeyEvent(ImGuiKey.D, keyboard.dKey.isPressed);
            io.AddKeyEvent(ImGuiKey.E, keyboard.eKey.isPressed);
            io.AddKeyEvent(ImGuiKey.F, keyboard.fKey.isPressed);
            io.AddKeyEvent(ImGuiKey.G, keyboard.gKey.isPressed);
            io.AddKeyEvent(ImGuiKey.H, keyboard.hKey.isPressed);
            io.AddKeyEvent(ImGuiKey.I, keyboard.iKey.isPressed);
            io.AddKeyEvent(ImGuiKey.J, keyboard.jKey.isPressed);
            io.AddKeyEvent(ImGuiKey.K, keyboard.kKey.isPressed);
            io.AddKeyEvent(ImGuiKey.L, keyboard.lKey.isPressed);
            io.AddKeyEvent(ImGuiKey.M, keyboard.mKey.isPressed);
            io.AddKeyEvent(ImGuiKey.N, keyboard.nKey.isPressed);
            io.AddKeyEvent(ImGuiKey.O, keyboard.oKey.isPressed);
            io.AddKeyEvent(ImGuiKey.P, keyboard.pKey.isPressed);
            io.AddKeyEvent(ImGuiKey.Q, keyboard.qKey.isPressed);
            io.AddKeyEvent(ImGuiKey.R, keyboard.rKey.isPressed);
            io.AddKeyEvent(ImGuiKey.S, keyboard.sKey.isPressed);
            io.AddKeyEvent(ImGuiKey.T, keyboard.tKey.isPressed);
            io.AddKeyEvent(ImGuiKey.U, keyboard.uKey.isPressed);
            io.AddKeyEvent(ImGuiKey.V, keyboard.vKey.isPressed);
            io.AddKeyEvent(ImGuiKey.W, keyboard.wKey.isPressed);
            io.AddKeyEvent(ImGuiKey.X, keyboard.xKey.isPressed);
            io.AddKeyEvent(ImGuiKey.Y, keyboard.yKey.isPressed);
            io.AddKeyEvent(ImGuiKey.Z, keyboard.zKey.isPressed);

            // Number row
            io.AddKeyEvent(ImGuiKey._0, keyboard.digit0Key.isPressed);
            io.AddKeyEvent(ImGuiKey._1, keyboard.digit1Key.isPressed);
            io.AddKeyEvent(ImGuiKey._2, keyboard.digit2Key.isPressed);
            io.AddKeyEvent(ImGuiKey._3, keyboard.digit3Key.isPressed);
            io.AddKeyEvent(ImGuiKey._4, keyboard.digit4Key.isPressed);
            io.AddKeyEvent(ImGuiKey._5, keyboard.digit5Key.isPressed);
            io.AddKeyEvent(ImGuiKey._6, keyboard.digit6Key.isPressed);
            io.AddKeyEvent(ImGuiKey._7, keyboard.digit7Key.isPressed);
            io.AddKeyEvent(ImGuiKey._8, keyboard.digit8Key.isPressed);
            io.AddKeyEvent(ImGuiKey._9, keyboard.digit9Key.isPressed);

            // Function keys
            io.AddKeyEvent(ImGuiKey.F1, keyboard.f1Key.isPressed);
            io.AddKeyEvent(ImGuiKey.F2, keyboard.f2Key.isPressed);
            io.AddKeyEvent(ImGuiKey.F3, keyboard.f3Key.isPressed);
            io.AddKeyEvent(ImGuiKey.F4, keyboard.f4Key.isPressed);
            io.AddKeyEvent(ImGuiKey.F5, keyboard.f5Key.isPressed);
            io.AddKeyEvent(ImGuiKey.F6, keyboard.f6Key.isPressed);
            io.AddKeyEvent(ImGuiKey.F7, keyboard.f7Key.isPressed);
            io.AddKeyEvent(ImGuiKey.F8, keyboard.f8Key.isPressed);
            io.AddKeyEvent(ImGuiKey.F9, keyboard.f9Key.isPressed);
            io.AddKeyEvent(ImGuiKey.F10, keyboard.f10Key.isPressed);
            io.AddKeyEvent(ImGuiKey.F11, keyboard.f11Key.isPressed);
            io.AddKeyEvent(ImGuiKey.F12, keyboard.f12Key.isPressed);

            // Punctuation / symbols (US layout key locations; text comes from onTextInput)
            io.AddKeyEvent(ImGuiKey.Apostrophe, keyboard.quoteKey.isPressed);
            io.AddKeyEvent(ImGuiKey.Comma, keyboard.commaKey.isPressed);
            io.AddKeyEvent(ImGuiKey.Minus, keyboard.minusKey.isPressed);
            io.AddKeyEvent(ImGuiKey.Period, keyboard.periodKey.isPressed);
            io.AddKeyEvent(ImGuiKey.Slash, keyboard.slashKey.isPressed);
            io.AddKeyEvent(ImGuiKey.Semicolon, keyboard.semicolonKey.isPressed);
            io.AddKeyEvent(ImGuiKey.Equal, keyboard.equalsKey.isPressed);
            io.AddKeyEvent(ImGuiKey.LeftBracket, keyboard.leftBracketKey.isPressed);
            io.AddKeyEvent(ImGuiKey.Backslash, keyboard.backslashKey.isPressed);
            io.AddKeyEvent(ImGuiKey.RightBracket, keyboard.rightBracketKey.isPressed);
            io.AddKeyEvent(ImGuiKey.GraveAccent, keyboard.backquoteKey.isPressed);

            // Locks / misc
            io.AddKeyEvent(ImGuiKey.CapsLock, keyboard.capsLockKey.isPressed);
            io.AddKeyEvent(ImGuiKey.ScrollLock, keyboard.scrollLockKey.isPressed);
            io.AddKeyEvent(ImGuiKey.NumLock, keyboard.numLockKey.isPressed);
            io.AddKeyEvent(ImGuiKey.PrintScreen, keyboard.printScreenKey.isPressed);
            io.AddKeyEvent(ImGuiKey.Pause, keyboard.pauseKey.isPressed);

            // Numpad
            io.AddKeyEvent(ImGuiKey.Keypad0, keyboard.numpad0Key.isPressed);
            io.AddKeyEvent(ImGuiKey.Keypad1, keyboard.numpad1Key.isPressed);
            io.AddKeyEvent(ImGuiKey.Keypad2, keyboard.numpad2Key.isPressed);
            io.AddKeyEvent(ImGuiKey.Keypad3, keyboard.numpad3Key.isPressed);
            io.AddKeyEvent(ImGuiKey.Keypad4, keyboard.numpad4Key.isPressed);
            io.AddKeyEvent(ImGuiKey.Keypad5, keyboard.numpad5Key.isPressed);
            io.AddKeyEvent(ImGuiKey.Keypad6, keyboard.numpad6Key.isPressed);
            io.AddKeyEvent(ImGuiKey.Keypad7, keyboard.numpad7Key.isPressed);
            io.AddKeyEvent(ImGuiKey.Keypad8, keyboard.numpad8Key.isPressed);
            io.AddKeyEvent(ImGuiKey.Keypad9, keyboard.numpad9Key.isPressed);

            io.AddKeyEvent(ImGuiKey.KeypadDivide, keyboard.numpadDivideKey.isPressed);
            io.AddKeyEvent(ImGuiKey.KeypadMultiply, keyboard.numpadMultiplyKey.isPressed);
            io.AddKeyEvent(ImGuiKey.KeypadSubtract, keyboard.numpadMinusKey.isPressed);
            io.AddKeyEvent(ImGuiKey.KeypadAdd, keyboard.numpadPlusKey.isPressed);
            io.AddKeyEvent(ImGuiKey.KeypadDecimal, keyboard.numpadPeriodKey.isPressed);
            io.AddKeyEvent(ImGuiKey.KeypadEnter, keyboard.numpadEnterKey.isPressed);
            io.AddKeyEvent(ImGuiKey.KeypadEqual, keyboard.numpadEqualsKey.isPressed);
        }

        private static void UpdateGamepad(ImGuiIOPtr io)
        {
            var gamepad = Gamepad.current;
            if (gamepad == null)
            {
                // Clear common nav keys so they don't stick when pad disconnects
                io.AddKeyEvent(ImGuiKey.GamepadDpadUp, false);
                io.AddKeyEvent(ImGuiKey.GamepadDpadDown, false);
                io.AddKeyEvent(ImGuiKey.GamepadDpadLeft, false);
                io.AddKeyEvent(ImGuiKey.GamepadDpadRight, false);
                io.AddKeyEvent(ImGuiKey.GamepadFaceDown, false);
                io.AddKeyEvent(ImGuiKey.GamepadFaceRight, false);
                io.AddKeyEvent(ImGuiKey.GamepadFaceLeft, false);
                io.AddKeyEvent(ImGuiKey.GamepadFaceUp, false);
                io.AddKeyEvent(ImGuiKey.GamepadL1, false);
                io.AddKeyEvent(ImGuiKey.GamepadR1, false);
                io.AddKeyEvent(ImGuiKey.GamepadL2, false);
                io.AddKeyEvent(ImGuiKey.GamepadR2, false);
                io.AddKeyEvent(ImGuiKey.GamepadL3, false);
                io.AddKeyEvent(ImGuiKey.GamepadR3, false);
                io.AddKeyEvent(ImGuiKey.GamepadStart, false);
                io.AddKeyEvent(ImGuiKey.GamepadBack, false);
                return;
            }

            io.AddKeyEvent(ImGuiKey.GamepadDpadUp, gamepad.dpad.up.isPressed);
            io.AddKeyEvent(ImGuiKey.GamepadDpadDown, gamepad.dpad.down.isPressed);
            io.AddKeyEvent(ImGuiKey.GamepadDpadLeft, gamepad.dpad.left.isPressed);
            io.AddKeyEvent(ImGuiKey.GamepadDpadRight, gamepad.dpad.right.isPressed);

            io.AddKeyEvent(ImGuiKey.GamepadFaceDown, gamepad.buttonSouth.isPressed); // A / Cross
            io.AddKeyEvent(ImGuiKey.GamepadFaceRight, gamepad.buttonEast.isPressed); // B / Circle
            io.AddKeyEvent(ImGuiKey.GamepadFaceLeft, gamepad.buttonWest.isPressed);  // X / Square
            io.AddKeyEvent(ImGuiKey.GamepadFaceUp, gamepad.buttonNorth.isPressed);   // Y / Triangle

            io.AddKeyEvent(ImGuiKey.GamepadL1, gamepad.leftShoulder.isPressed);
            io.AddKeyEvent(ImGuiKey.GamepadR1, gamepad.rightShoulder.isPressed);

            io.AddKeyEvent(ImGuiKey.GamepadL2, gamepad.leftTrigger.isPressed);
            io.AddKeyEvent(ImGuiKey.GamepadR2, gamepad.rightTrigger.isPressed);

            io.AddKeyEvent(ImGuiKey.GamepadL3, gamepad.leftStickButton.isPressed);
            io.AddKeyEvent(ImGuiKey.GamepadR3, gamepad.rightStickButton.isPressed);

            io.AddKeyEvent(ImGuiKey.GamepadStart, gamepad.startButton.isPressed);
            io.AddKeyEvent(ImGuiKey.GamepadBack, gamepad.selectButton.isPressed);
        }
    }
}
