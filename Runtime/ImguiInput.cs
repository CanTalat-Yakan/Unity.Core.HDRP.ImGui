using ImGuiNET;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Feeds Unity input state into ImGui IO.
    /// </summary>
    internal static class ImguiInput
    {
        public static void UpdateIo(ImGuiIOPtr io)
        {
            // Mouse position: Unity's Input.mousePosition is bottom-left origin.
            // ImGui expects top-left origin.
            var mousePos = Input.mousePosition;
            var flippedY = Screen.height - mousePos.y;
            io.AddMousePosEvent(mousePos.x, flippedY);

            io.AddMouseButtonEvent(0, Input.GetMouseButton(0));
            io.AddMouseButtonEvent(1, Input.GetMouseButton(1));
            io.AddMouseButtonEvent(2, Input.GetMouseButton(2));

            var scroll = Input.mouseScrollDelta;
            io.AddMouseWheelEvent(scroll.x, scroll.y);

            // Modifier keys.
            var ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            var alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var super = Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand) ||
                        Input.GetKey(KeyCode.LeftWindows) || Input.GetKey(KeyCode.RightWindows);

            io.AddKeyEvent(ImGuiKey.ModCtrl, ctrl);
            io.AddKeyEvent(ImGuiKey.ModAlt, alt);
            io.AddKeyEvent(ImGuiKey.ModShift, shift);
            io.AddKeyEvent(ImGuiKey.ModSuper, super);

            // Minimal keyboard mapping for basic navigation and common shortcuts.
            io.AddKeyEvent(ImGuiKey.Tab, Input.GetKey(KeyCode.Tab));
            io.AddKeyEvent(ImGuiKey.LeftArrow, Input.GetKey(KeyCode.LeftArrow));
            io.AddKeyEvent(ImGuiKey.RightArrow, Input.GetKey(KeyCode.RightArrow));
            io.AddKeyEvent(ImGuiKey.UpArrow, Input.GetKey(KeyCode.UpArrow));
            io.AddKeyEvent(ImGuiKey.DownArrow, Input.GetKey(KeyCode.DownArrow));
            io.AddKeyEvent(ImGuiKey.PageUp, Input.GetKey(KeyCode.PageUp));
            io.AddKeyEvent(ImGuiKey.PageDown, Input.GetKey(KeyCode.PageDown));
            io.AddKeyEvent(ImGuiKey.Home, Input.GetKey(KeyCode.Home));
            io.AddKeyEvent(ImGuiKey.End, Input.GetKey(KeyCode.End));
            io.AddKeyEvent(ImGuiKey.Insert, Input.GetKey(KeyCode.Insert));
            io.AddKeyEvent(ImGuiKey.Delete, Input.GetKey(KeyCode.Delete));
            io.AddKeyEvent(ImGuiKey.Backspace, Input.GetKey(KeyCode.Backspace));
            io.AddKeyEvent(ImGuiKey.Space, Input.GetKey(KeyCode.Space));
            io.AddKeyEvent(ImGuiKey.Enter, Input.GetKey(KeyCode.Return));
            io.AddKeyEvent(ImGuiKey.Escape, Input.GetKey(KeyCode.Escape));

            io.AddKeyEvent(ImGuiKey.A, Input.GetKey(KeyCode.A));
            io.AddKeyEvent(ImGuiKey.C, Input.GetKey(KeyCode.C));
            io.AddKeyEvent(ImGuiKey.V, Input.GetKey(KeyCode.V));
            io.AddKeyEvent(ImGuiKey.X, Input.GetKey(KeyCode.X));
            io.AddKeyEvent(ImGuiKey.Y, Input.GetKey(KeyCode.Y));
            io.AddKeyEvent(ImGuiKey.Z, Input.GetKey(KeyCode.Z));
        }
    }
}
