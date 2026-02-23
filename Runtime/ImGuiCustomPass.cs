using System;
using ImGuiNET;
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
            var host = ImGuiHost.Instance;

            if (host == null || cam == null)
                return;

            if (cam.cameraType == CameraType.SceneView)
                return;
            
            try
            {
                if (ShowDemoWindow)
                    ImGui.ShowDemoWindow();
            }
            catch { }

            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer);
            host.Render(ctx.cmd, cam);
        }
    }
}