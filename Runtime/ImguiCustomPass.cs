using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEssentials
{
    /// <summary>
    /// Minimal HDRP <see cref="CustomPass"/> that forwards rendering to a referenced <see cref="ImguiHost"/>.
    /// </summary>
    internal sealed class ImguiCustomPass : CustomPass
    {
        /// <summary>
        /// Scene reference. Assign this in the Custom Pass inspector.
        /// </summary>
        public ImguiHost host;

        protected override void Execute(CustomPassContext ctx)
        {
            var cam = ctx.hdCamera?.camera;
            if (host == null || cam == null)
                return;

            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer);
            host.Render(ctx.cmd, cam);
        }
    }
}
