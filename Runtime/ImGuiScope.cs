using System;
using ImGuiNET;

namespace UnityEssentials
{
    /// <summary>
    /// Small helper that makes calling ImGui safe when the host/context isn't initialized.
    /// If there's no current ImGui context, the scope is inactive and you must skip drawing.
    /// </summary>
    public readonly struct ImGuiScope : IDisposable
    {
        public readonly bool Active;

        private ImGuiScope(bool active)
        {
            Active = active;
        }

        public static ImGuiScope TryEnter()
        {
            // Avoid calling into ImGui at all during playmode transitions / singleton teardown.
            if (ImGuiHost.IsBlockedForSafety)
                return new ImGuiScope(active: false);

            var known = ImGuiHost.KnownContext;
            if (known == IntPtr.Zero)
                return new ImGuiScope(active: false);

            try
            {
                var ctx = ImGui.GetCurrentContext();
                if (ctx == IntPtr.Zero)
                    return new ImGuiScope(active: false);

                // Require the current context to match the host-owned context.
                // This prevents drawing into a stale/dangling context pointer.
                if (ctx != known)
                    return new ImGuiScope(active: false);

                return new ImGuiScope(active: true);
            }
            catch
            {
                return new ImGuiScope(active: false);
            }
        }

        public void Dispose()
        {
        }
    }
}
