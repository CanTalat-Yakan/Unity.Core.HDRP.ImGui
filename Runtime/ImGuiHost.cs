using System;
using ImGuiNET;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Minimal ImGui host component.
    /// Referenced from an HDRP Custom Pass; rendering is performed by that Custom Pass via <see cref="Render"/>.
    /// This component auto-exists as a GlobalSingleton (no manual scene setup required).
    /// </summary>
    public sealed class ImGuiHost : GlobalSingleton<ImGuiHost>
    {
        private IntPtr _context;
        private float _lastTime;
        private readonly ImGuiRenderer _renderer = new();

        private void OnEnable() =>
            EnsureInitialized();

        private void OnDisable() =>
            Shutdown();

        protected override void OnDestroy()
        {
            Shutdown();
            base.OnDestroy();
        }

        private void EnsureInitialized()
        {
            if (_context != IntPtr.Zero)
                return;

            _context = ImGui.CreateContext();
            ImGui.SetCurrentContext(_context);

            var io = ImGui.GetIO();
            io.Fonts.Clear();
            io.Fonts.AddFontDefault();
            io.Fonts.Build();

            io.DeltaTime = Mathf.Max(0.0001f, Time.realtimeSinceStartup - _lastTime);
            _lastTime = Time.realtimeSinceStartup;
            io.DisplaySize = new System.Numerics.Vector2(1920, 1080);

            ImGuiInput.UpdateIo(io);

            _renderer.EnsureResources();
            _lastTime = Time.realtimeSinceStartup;

            ImGui.NewFrame();
        }

        private void Shutdown()
        {
            if (_context != IntPtr.Zero)
            {
                try { ImGui.DestroyContext(_context); }
                catch { }

                _context = IntPtr.Zero;
            }

            _renderer.Dispose();
            ImGuiTextureRegistry.Clear();
            ImGuiInput.Shutdown();
        }

        internal void Render(UnityEngine.Rendering.CommandBuffer cmd, Camera cam)
        {
            try
            {
                EnsureInitialized();

                if (_context == IntPtr.Zero)
                    return;

                using var scope = ImGuiScope.TryEnter();
                if (!scope.Active)
                    return;

                ImGui.SetCurrentContext(_context);

                var io = ImGui.GetIO();
                io.DeltaTime = Mathf.Max(0.0001f, Time.realtimeSinceStartup - _lastTime);
                _lastTime = Time.realtimeSinceStartup;
                io.DisplaySize = new System.Numerics.Vector2(cam.pixelWidth, cam.pixelHeight);

                ImGuiInput.UpdateIo(io);

                ImGui.Render();
                _renderer.RenderDrawData(ImGui.GetDrawData(), cmd);

                ImGui.NewFrame();
            }
            catch { }
        }
    }
}
