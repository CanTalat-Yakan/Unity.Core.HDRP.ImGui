using System;
using ImGuiNET;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEssentials
{
    internal sealed class ImguiRenderer : IDisposable
    {
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int TextureScaleOffsetId = Shader.PropertyToID("_TextureScaleOffset");

        private Material _material;
        private Texture2D _fontAtlas;
        private IntPtr _fontAtlasId;

        private Mesh _mesh;
        private Vector3[] _positions;
        private Color32[] _colors;
        private Vector2[] _uvs;
        private int[] _indices;

        public void EnsureResources()
        {
            if (_material == null)
            {
                // Simple UI material for rendering ImGui triangles. Uses Unity's built-in UI shader.
                // This should work across render pipelines as long as the shader is available.
                var shader = Shader.Find("UI/Default");
                if (shader == null)
                    shader = Shader.Find("Unlit/Transparent");

                _material = shader != null ? new Material(shader) : null;
            }

            if (_mesh == null)
            {
                _mesh = new Mesh { name = "ImguiMesh" };
                _mesh.MarkDynamic();
                _mesh.indexFormat = IndexFormat.UInt32;
            }

            EnsureFontAtlasTexture();
        }

        private void EnsureFontAtlasTexture()
        {
            var io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);
            if (pixels == IntPtr.Zero || width <= 0 || height <= 0)
                return;

            if (_fontAtlas == null || _fontAtlas.width != width || _fontAtlas.height != height)
            {
                if (_fontAtlas != null)
                    UnityEngine.Object.DestroyImmediate(_fontAtlas);

                _fontAtlas = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false, linear: true)
                {
                    name = "ImguiFontAtlas",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            _fontAtlas.LoadRawTextureData(pixels, width * height * bytesPerPixel);
            _fontAtlas.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            _fontAtlasId = ImguiTextureRegistry.GetId(_fontAtlas);
            io.Fonts.SetTexID(_fontAtlasId);
        }

        public void RenderDrawData(ImDrawDataPtr drawData, CommandBuffer cmd)
        {
            EnsureResources();
            if (_material == null || _mesh == null)
                return;

            if (!drawData.Valid || drawData.CmdListsCount == 0)
                return;

            // Total sizes
            var totalVtxCount = drawData.TotalVtxCount;
            var totalIdxCount = drawData.TotalIdxCount;
            if (totalVtxCount <= 0 || totalIdxCount <= 0)
                return;

            EnsureArrays(totalVtxCount, totalIdxCount);

            // ImGui draw data can be offset (multi-viewport) and scaled (HiDPI).
            // Apply these transforms so vertices and clip rectangles map to the correct framebuffer space.
            var clipOff = drawData.DisplayPos;
            var clipScale = drawData.FramebufferScale;

            // Convert ImGui vertex buffers into a Unity mesh
            var vtxOffset = 0;
            var idxOffset = 0;

            for (var n = 0; n < drawData.CmdListsCount; n++)
            {
                var cmdList = drawData.CmdLists[n];

                for (var v = 0; v < cmdList.VtxBuffer.Size; v++)
                {
                    var iv = cmdList.VtxBuffer[v];

                    // Keep vertex positions in ImGui space for now (we'll convert to clip later).
                    _positions[vtxOffset + v] = new Vector3(iv.pos.X, iv.pos.Y, 0);
                    _uvs[vtxOffset + v] = new Vector2(iv.uv.X, iv.uv.Y);
                    _colors[vtxOffset + v] = new Color32(
                        (byte)(iv.col & 0xFF),
                        (byte)((iv.col >> 8) & 0xFF),
                        (byte)((iv.col >> 16) & 0xFF),
                        (byte)((iv.col >> 24) & 0xFF));
                }

                for (var i = 0; i < cmdList.IdxBuffer.Size; i++)
                    _indices[idxOffset + i] = vtxOffset + cmdList.IdxBuffer[i];

                vtxOffset += cmdList.VtxBuffer.Size;
                idxOffset += cmdList.IdxBuffer.Size;
            }

            _mesh.Clear(keepVertexLayout: false);
            _mesh.vertices = _positions;
            _mesh.uv = _uvs;
            _mesh.colors32 = _colors;
            _mesh.triangles = _indices;
            _mesh.RecalculateBounds();

            var fbWidth = drawData.DisplaySize.X * clipScale.X;
            var fbHeight = drawData.DisplaySize.Y * clipScale.Y;
            if (fbWidth <= 0 || fbHeight <= 0)
                return;

            // Convert vertices to clip-space (NDC).
            // ImGui coordinates: origin at top-left, +Y down.
            // Clip space: origin at center, +Y up, range [-1..+1].
            for (var i = 0; i < totalVtxCount; i++)
            {
                var p = _positions[i];
                var x = (p.x - clipOff.X) * clipScale.X;
                var y = (p.y - clipOff.Y) * clipScale.Y;

                var cx = (x / fbWidth) * 2f - 1f;
                var cy = 1f - (y / fbHeight) * 2f;

                _positions[i] = new Vector3(cx, cy, 0);
            }

            _mesh.vertices = _positions;

            // Render state: use identity matrices since vertices are already in clip-space.
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            cmd.SetViewport(new Rect(0, 0, fbWidth, fbHeight));

            // Per-draw call state.
            var props = new MaterialPropertyBlock();

            var subMeshCount = 0;
            for (var n = 0; n < drawData.CmdListsCount; n++)
                subMeshCount += drawData.CmdLists[n].CmdBuffer.Size;

            _mesh.subMeshCount = subMeshCount;

            var globalIdxOffset = 0;
            var globalVtxOffset = 0;
            var sm = 0;

            for (var n = 0; n < drawData.CmdListsCount; n++)
            {
                var cmdList = drawData.CmdLists[n];

                for (var cmdI = 0; cmdI < cmdList.CmdBuffer.Size; cmdI++)
                {
                    var pcmd = cmdList.CmdBuffer[cmdI];

                    if (pcmd.ElemCount == 0)
                        continue;

                    // Reset scale/offset for each draw call.
                    _material.SetVector(TextureScaleOffsetId, new Vector4(1, 1, 0, 0));

                    _mesh.SetSubMesh(sm, new SubMeshDescriptor(
                        indexStart: globalIdxOffset + (int)pcmd.IdxOffset,
                        indexCount: (int)pcmd.ElemCount,
                        topology: MeshTopology.Triangles)
                    {
                        baseVertex = globalVtxOffset + (int)pcmd.VtxOffset
                    }, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);

                    // Use the ImGui font atlas texture.
                    // (User textures can be added later by resolving pcmd.TextureId via ImguiTextureRegistry.)
                    _material.SetTexture(MainTexId, _fontAtlas);

                    // Draw the requested submesh.
                    cmd.DrawMesh(_mesh, Matrix4x4.identity, _material, submeshIndex: sm, shaderPass: -1, properties: props);
                    sm++;
                }

                globalIdxOffset += cmdList.IdxBuffer.Size;
                globalVtxOffset += cmdList.VtxBuffer.Size;
            }
        }

        private void EnsureArrays(int vtxCount, int idxCount)
        {
            if (_positions == null || _positions.Length != vtxCount)
            {
                _positions = new Vector3[vtxCount];
                _uvs = new Vector2[vtxCount];
                _colors = new Color32[vtxCount];
            }

            if (_indices == null || _indices.Length != idxCount)
                _indices = new int[idxCount];
        }

        public void Dispose()
        {
            if (_material != null)
            {
                UnityEngine.Object.DestroyImmediate(_material);
                _material = null;
            }

            if (_fontAtlas != null)
            {
                UnityEngine.Object.DestroyImmediate(_fontAtlas);
                _fontAtlas = null;
            }

            if (_mesh != null)
            {
                UnityEngine.Object.DestroyImmediate(_mesh);
                _mesh = null;
            }
        }
    }
}
