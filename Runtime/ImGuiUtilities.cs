using ImGuiNET;
using UnityEngine;
using Vector2N = System.Numerics.Vector2;

namespace UnityEssentials
{
    public static class ImGuiUtilities
    {
        public static Camera ResolveCamera(Camera explicitCamera = null)
        {
            if (explicitCamera != null)
                return explicitCamera;

            if (Camera.current != null)
                return Camera.current;

            return Camera.main;
        }

        public static void GetDisplaySize(out float width, out float height)
        {
            var io = ImGui.GetIO();
            width = io.DisplaySize.X > 0f ? io.DisplaySize.X : Screen.width;
            height = io.DisplaySize.Y > 0f ? io.DisplaySize.Y : Screen.height;
        }

        public static float GetDisplayHeight()
        {
            GetDisplaySize(out _, out var height);
            return height;
        }

        public static bool TryWorldToImGuiScreen(Camera camera, Vector3 world, out Vector2N screen)
        {
            screen = default;
            if (camera == null)
                return false;

            var p = camera.WorldToScreenPoint(world);
            if (p.z <= 0f)
                return false;

            var displayHeight = GetDisplayHeight();
            screen = new Vector2N(p.x, displayHeight - p.y);
            return true;
        }
    }
}