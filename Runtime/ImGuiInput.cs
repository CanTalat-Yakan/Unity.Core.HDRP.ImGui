using ImGuiNET;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityEssentials
{
    /// <summary>
    /// Feeds Unity Input System state into ImGui IO.
    /// Requires the new Input System package.
    /// </summary>
    internal static class ImGuiInput
    {
        public static void UpdateIo(ImGuiIOPtr io)
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;

            if (mouse != null)
            {
                // Mouse position: Unity bottom-left -> ImGui top-left
                var pos = mouse.position.ReadValue();
                var flippedY = Screen.height - pos.y;
                io.AddMousePosEvent(pos.x, flippedY);

                io.AddMouseButtonEvent(0, mouse.leftButton.isPressed);
                io.AddMouseButtonEvent(1, mouse.rightButton.isPressed);
                io.AddMouseButtonEvent(2, mouse.middleButton.isPressed);

                // Scroll (already frame-delta in Input System)
                var scroll = mouse.scroll.ReadValue();
                io.AddMouseWheelEvent(scroll.x, scroll.y);
            }

            if (keyboard == null)
                return;

            // Modifiers
            var ctrl =
                keyboard.leftCtrlKey.isPressed ||
                keyboard.rightCtrlKey.isPressed;

            var alt =
                keyboard.leftAltKey.isPressed ||
                keyboard.rightAltKey.isPressed;

            var shift =
                keyboard.leftShiftKey.isPressed ||
                keyboard.rightShiftKey.isPressed;

            var super =
                keyboard.leftMetaKey.isPressed ||
                keyboard.rightMetaKey.isPressed;

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

            // Shortcuts
            io.AddKeyEvent(ImGuiKey.A, keyboard.aKey.isPressed);
            io.AddKeyEvent(ImGuiKey.C, keyboard.cKey.isPressed);
            io.AddKeyEvent(ImGuiKey.V, keyboard.vKey.isPressed);
            io.AddKeyEvent(ImGuiKey.X, keyboard.xKey.isPressed);
            io.AddKeyEvent(ImGuiKey.Y, keyboard.yKey.isPressed);
            io.AddKeyEvent(ImGuiKey.Z, keyboard.zKey.isPressed);

            // Text input (characters)
            // ImGui requires this for text fields.
            foreach (var c in Keyboard.current.allKeys)
            {
                if (c.wasPressedThisFrame && c.keyCode >= Key.A && c.keyCode <= Key.Z)
                {
                    var ch = (char)('a' + (c.keyCode - Key.A));
                    if (shift) ch = char.ToUpperInvariant(ch);
                    io.AddInputCharacter(ch);
                }
            }
        }
    }
}
