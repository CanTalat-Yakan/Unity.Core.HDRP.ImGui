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
            try
            {
                var ctx = ImGui.GetCurrentContext();
                return new ImGuiScope(active: ctx != IntPtr.Zero);
            }
            catch
            {
                return new ImGuiScope(active: false);
            }
        }

        public void Dispose()
        {
            // No-op.
        }
    }
}
