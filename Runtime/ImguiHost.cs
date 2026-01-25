using System;
using ImGuiNET;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Minimal ImGui host component.
    /// 
    /// Scene setup is manual: place this component in the scene and reference it from an HDRP Custom Pass.
    /// Rendering is performed by that Custom Pass by calling <see cref="Render"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ImguiHost : MonoBehaviour
    {
        [Tooltip("Only this camera renders the overlay (must match the camera used by the HDRP Custom Pass).")]
        [SerializeField] private Camera targetCamera;

        private IntPtr _context;
        private float _lastTime;
        private readonly ImguiRenderer _renderer = new();

        private void OnEnable()
        {
            if (_context != IntPtr.Zero)
                return;

            _context = ImGui.CreateContext();
            ImGui.SetCurrentContext(_context);

            var io = ImGui.GetIO();
            io.Fonts.Clear();
            io.Fonts.AddFontDefault();
            io.Fonts.Build();

            _renderer.EnsureResources();
            _lastTime = Time.realtimeSinceStartup;
        }

        private void OnDisable()
        {
            if (_context != IntPtr.Zero)
            {
                try { ImGui.DestroyContext(_context); }
                catch { /* Unity may be shutting down; ignore cleanup errors. */ }
                _context = IntPtr.Zero;
            }

            _renderer.Dispose();
            ImguiTextureRegistry.Clear();
        }

        /// <summary>
        /// Renders ImGui for the given camera.
        /// This is called from an HDRP Custom Pass; <paramref name="cam"/> must match <see cref="targetCamera"/>.
        /// </summary>
        internal void Render(UnityEngine.Rendering.CommandBuffer cmd, Camera cam)
        {
            if (_context == IntPtr.Zero)
                return;

            if (targetCamera == null || cam != targetCamera)
                return;

            ImGui.SetCurrentContext(_context);

            var io = ImGui.GetIO();
            io.DeltaTime = Mathf.Max(0.0001f, Time.realtimeSinceStartup - _lastTime);
            _lastTime = Time.realtimeSinceStartup;
            io.DisplaySize = new System.Numerics.Vector2(cam.pixelWidth, cam.pixelHeight);

            ImguiInput.UpdateIo(io);

            ImGui.NewFrame();
            
            // Example window to verify rendering and input.
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(20, 20), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(240, 90), ImGuiCond.Always);
            ImGui.Begin("ImGui", ImGuiWindowFlags.NoResize);
            ImGui.TextUnformatted("Hello ImGui");
            ImGui.Text($"Frame: {Time.frameCount}");
            ImGui.End();

            ImGui.Render();
            var drawData = ImGui.GetDrawData();
            _renderer.RenderDrawData(drawData, cmd);
        }
    }
}
