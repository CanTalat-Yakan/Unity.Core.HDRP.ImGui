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
        internal static IntPtr KnownContext { get; private set; }

#if UNITY_EDITOR
        private static bool s_editorBlocked;
#endif

        private IntPtr _context;
        private float _lastTime;
        private readonly ImGuiRenderer _renderer = new();

        internal bool ShowDemoWindow { get; set; }

        private bool _shuttingDown;

        internal static bool IsBlockedForSafety
        {
            get
            {
                if (GlobalSingletonRegistrar.IsDestroyingAll)
                    return true;

#if UNITY_EDITOR
                if (s_editorBlocked)
                    return true;
#endif

                return false;
            }
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void EditorInit()
        {
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            // Block native calls while Unity is transitioning playmode.
            if (state == UnityEditor.PlayModeStateChange.ExitingEditMode ||
                state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                s_editorBlocked = true;

            if (state == UnityEditor.PlayModeStateChange.EnteredEditMode ||
                state == UnityEditor.PlayModeStateChange.EnteredPlayMode)
                s_editorBlocked = false;
        }
#endif

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
            if (IsBlockedForSafety)
                return;

            if (_context != IntPtr.Zero)
                return;

            _context = ImGui.CreateContext();
            KnownContext = _context;
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
            _shuttingDown = true;

            KnownContext = IntPtr.Zero;

            var ctx = _context;
            _context = IntPtr.Zero;

            // During playmode transitions (and while global singletons are being destroyed), calling into
            // native plugins can be unsafe on some devices/editors. Prefer leaking the context rather than
            // crashing the editor.
            if (ctx != IntPtr.Zero && !IsBlockedForSafety)
            {
                try { ImGui.SetCurrentContext(IntPtr.Zero); }
                catch { }

                try { ImGui.DestroyContext(ctx); }
                catch { }
            }

            _renderer.Dispose();
            ImGuiTextureRegistry.Clear();
            ImGuiInput.Shutdown();

            _shuttingDown = false;
        }

        internal void Render(UnityEngine.Rendering.CommandBuffer cmd, Camera cam)
        {
            if (IsBlockedForSafety)
                return;

            if (_shuttingDown)
                return;

            try
            {
                EnsureInitialized();

                if (_context == IntPtr.Zero)
                    return;

                ImGui.SetCurrentContext(_context);

                using var scope = ImGuiScope.TryEnter();
                if (!scope.Active)
                    return;

                // If requested by the Custom Pass, draw the demo window inside the host-owned context.
                // (Avoids calling ImGui from the Custom Pass when context may be stale.)
                if (ShowDemoWindow)
                {
                    try { ImGui.ShowDemoWindow(); }
                    catch { }
                }

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
