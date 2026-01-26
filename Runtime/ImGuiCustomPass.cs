using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEssentials
{
    /// <summary>
    /// Minimal HDRP <see cref="CustomPass"/> that forwards rendering to a referenced <see cref="ImGuiHost"/>.
    /// </summary>
    [Serializable]
    internal sealed class ImGuiCustomPass : CustomPass
    {
        /// <summary>
        /// Scene reference. Assign this in the Custom Pass inspector.
        /// </summary>
        public ImGuiHost Host = ImGuiHost.Instance;

        protected override void Execute(CustomPassContext ctx)
        {
            var cam = ctx.hdCamera?.camera;
            if (Host == null || cam == null)
                return;
            
            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer);
            Host.Render(ctx.cmd, cam);
        }
    }
}
