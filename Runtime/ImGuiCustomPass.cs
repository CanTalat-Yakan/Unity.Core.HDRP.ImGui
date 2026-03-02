using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEssentials
{
    [Serializable]
    internal sealed class ImGuiCustomPass : CustomPass
    {
        public bool ShowDemoWindow = false;

        protected override void Execute(CustomPassContext ctx)
        {
            var cam = ctx.hdCamera?.camera;
            if (ImGuiHost.IsBlockedForSafety)
                return;

            var host = ImGuiHost.Instance;

            if (host == null || cam == null)
                return;

            if (cam.cameraType == CameraType.SceneView)
                return;

            host.ShowDemoWindow = ShowDemoWindow;

            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer);
            host.Render(ctx.cmd, cam);
        }
    }
}