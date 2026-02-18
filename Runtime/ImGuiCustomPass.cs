using System;
using UnityEngine;
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
        protected override void Execute(CustomPassContext ctx)
        {
            var cam = ctx.hdCamera?.camera;
            var host = ImGuiHost.Instance;
            if (host == null || cam == null)
                return;
            
            if (cam.cameraType == CameraType.SceneView)
                return;
            
            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer);
            host.Render(ctx.cmd, cam);
        }
    }
}
