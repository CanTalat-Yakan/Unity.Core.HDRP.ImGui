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
    [DisallowMultipleComponent]
    public sealed class ImGuiHost : GlobalSingleton<ImGuiHost>
    {
        public static bool ShowDemoWindow { get; set; } = true;
        
        /// <summary>
        /// Convenience API to register a callback and automatically ensure the host exists.
        /// Invoked after <c>ImGui.NewFrame()</c> and before <c>ImGui.Render()</c>.
        /// </summary>
        /// <remarks>
        /// Keep handlers fast (this runs every frame).
        /// Handlers should not call <c>ImGui.NewFrame()</c> / <c>ImGui.Render()</c>.
        /// Exceptions are caught so one bad handler won't break rendering.
        /// </remarks>
        public static void Register(Action callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            _ = Instance; // Ensure GlobalSingleton exists.
            s_afterNewFrame += callback;
        }

        /// <summary>
        /// Convenience API to unregister a previously registered callback.
        /// </summary>
        public static void Unregister(Action callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            s_afterNewFrame -= callback;
        }

        private IntPtr _context;
        private float _lastTime;
        private readonly ImGuiRenderer _renderer = new();
        private static event Action? s_afterNewFrame;

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

            _renderer.EnsureResources();
            _lastTime = Time.realtimeSinceStartup;
        }

        private void Shutdown()
        {
            if (_context != IntPtr.Zero)
            {
                try { ImGui.DestroyContext(_context); }
                catch { /* ignore */ }
                _context = IntPtr.Zero;
            }

            _renderer.Dispose();
            ImGuiTextureRegistry.Clear();
        }

        internal void Render(UnityEngine.Rendering.CommandBuffer cmd, Camera cam)
        {
            EnsureInitialized();

            if (_context == IntPtr.Zero)
                return;

            ImGui.SetCurrentContext(_context);

            var io = ImGui.GetIO();
            io.DeltaTime = Mathf.Max(0.0001f, Time.realtimeSinceStartup - _lastTime);
            _lastTime = Time.realtimeSinceStartup;
            io.DisplaySize = new System.Numerics.Vector2(cam.pixelWidth, cam.pixelHeight);

            ImGuiInput.UpdateIo(io);

            ImGui.NewFrame();
            
            try { s_afterNewFrame?.Invoke(); } 
            catch (Exception) { }
            
            if(ShowDemoWindow)
                ImGui.ShowDemoWindow();

            ImGui.Render();
            _renderer.RenderDrawData(ImGui.GetDrawData(), cmd);
        }
    }
}
