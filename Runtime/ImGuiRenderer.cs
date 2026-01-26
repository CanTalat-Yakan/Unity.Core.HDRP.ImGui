using System;
using System.Collections.Generic;
using ImGuiNET;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEssentials
{
    internal sealed class ImGuiRenderer : IDisposable
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

        // Cached once; reduces per-frame work
        private static readonly Vector4 s_identityScaleOffset = new(1, 1, 0, 0);

        public void EnsureResources()
        {
            if (_material == null)
            {
                // Stick to built-in UI/Default (as requested).
                var shader = Shader.Find("UI/Default");
                _material = shader != null ? new Material(shader) : null;

                // UI/Default expects a few keywords/states; keep material minimal.
                if (_material != null)
                {
                    _material.hideFlags = HideFlags.HideAndDontSave;
                }
            }

            if (_mesh == null)
            {
                _mesh = new Mesh { name = "ImguiMesh" };
                _mesh.MarkDynamic();
                _mesh.indexFormat = IndexFormat.UInt32;
                _mesh.hideFlags = HideFlags.HideAndDontSave;
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

                // sRGB (linear:false). Matches UI/Default expectations in a Linear project.
                _fontAtlas = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false, linear: false)
                {
                    name = "ImguiFontAtlas",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    anisoLevel = 1,
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            _fontAtlas.LoadRawTextureData(pixels, width * height * bytesPerPixel);
            _fontAtlas.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            _fontAtlasId = ImGuiTextureRegistry.GetId(_fontAtlas);
            io.Fonts.SetTexID(_fontAtlasId);
        }

        public void RenderDrawData(ImDrawDataPtr drawData, CommandBuffer cmd)
        {
            EnsureResources();
            if (_material == null || _mesh == null)
                return;

            if (!drawData.Valid || drawData.CmdListsCount == 0)
                return;

            var totalVtxCount = drawData.TotalVtxCount;
            var totalIdxCount = drawData.TotalIdxCount;
            if (totalVtxCount <= 0 || totalIdxCount <= 0)
                return;

            EnsureArrays(totalVtxCount, totalIdxCount);

            // ImGui draw data can be offset (multi-viewport) and scaled (HiDPI).
            var clipOff = drawData.DisplayPos;
            var clipScale = drawData.FramebufferScale;

            // Flatten command lists into a single mesh buffer.
            var vtxOffset = 0;
            var idxOffset = 0;

            for (var n = 0; n < drawData.CmdListsCount; n++)
            {
                var cmdList = drawData.CmdLists[n];

                for (var v = 0; v < cmdList.VtxBuffer.Size; v++)
                {
                    var iv = cmdList.VtxBuffer[v];

                    _positions[vtxOffset + v] = new Vector3(iv.pos.X, iv.pos.Y, 0f);
                    _uvs[vtxOffset + v] = new Vector2(iv.uv.X, iv.uv.Y);

                    // Keep your "linear hack" as requested.
                    var c = iv.col;
                    var col32 = new Color32(
                        (byte)(c & 0xFF),
                        (byte)((c >> 8) & 0xFF),
                        (byte)((c >> 16) & 0xFF),
                        (byte)((c >> 24) & 0xFF)
                    );

                    _colors[vtxOffset + v] = ((Color)col32).linear;
                }

                // Keep indices local; use baseVertex later.
                for (var i = 0; i < cmdList.IdxBuffer.Size; i++)
                    _indices[idxOffset + i] = cmdList.IdxBuffer[i];

                vtxOffset += cmdList.VtxBuffer.Size;
                idxOffset += cmdList.IdxBuffer.Size;
            }

            _mesh.Clear(keepVertexLayout: false);
            _mesh.SetVertices(_positions, 0, totalVtxCount);
            _mesh.SetUVs(0, _uvs, 0, totalVtxCount);
            _mesh.SetColors(_colors, 0, totalVtxCount);
            _mesh.SetTriangles(_indices, 0, totalIdxCount, 0, calculateBounds: false);

            var fbWidth = drawData.DisplaySize.X * clipScale.X;
            var fbHeight = drawData.DisplaySize.Y * clipScale.Y;
            if (fbWidth <= 0f || fbHeight <= 0f)
                return;

            // Convert vertices to clip-space (NDC).
            for (var i = 0; i < totalVtxCount; i++)
            {
                var p = _positions[i];
                var x = (p.x - clipOff.X) * clipScale.X;
                var y = (p.y - clipOff.Y) * clipScale.Y;

                var cx = (x / fbWidth) * 2f - 1f;
                var cy = 1f - (y / fbHeight) * 2f;

                _positions[i] = new Vector3(cx, cy, 0f);
            }

            _mesh.SetVertices(_positions, 0, totalVtxCount);

            // IMPORTANT: bounds must match clip-space now, otherwise Unity may cull incorrectly.
            _mesh.bounds = new Bounds(Vector3.zero, new Vector3(2f, 2f, 0.1f));

            // Render state: vertices are already in clip-space, so identity is correct.
            // NOTE: The render pass that calls this should restore camera matrices after this draw.
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            cmd.SetViewport(new Rect(0f, 0f, fbWidth, fbHeight));

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
                    {
                        sm++;
                        continue;
                    }

                    // Resolve texture
                    var tex = (Texture)_fontAtlas;
                    if (pcmd.TextureId != IntPtr.Zero && ImGuiTextureRegistry.TryGetTexture(pcmd.TextureId, out var resolved))
                        tex = resolved;

                    // Clip rect -> framebuffer space (scaled)
                    var cr = pcmd.ClipRect; // (x1,y1,x2,y2) in ImGui space
                    var clipX1 = (cr.X - clipOff.X) * clipScale.X;
                    var clipY1 = (cr.Y - clipOff.Y) * clipScale.Y;
                    var clipX2 = (cr.Z - clipOff.X) * clipScale.X;
                    var clipY2 = (cr.W - clipOff.Y) * clipScale.Y;

                    // Clamp
                    clipX1 = Mathf.Clamp(clipX1, 0f, fbWidth);
                    clipY1 = Mathf.Clamp(clipY1, 0f, fbHeight);
                    clipX2 = Mathf.Clamp(clipX2, 0f, fbWidth);
                    clipY2 = Mathf.Clamp(clipY2, 0f, fbHeight);

                    var scissorW = clipX2 - clipX1;
                    var scissorH = clipY2 - clipY1;

                    if (scissorW <= 0f || scissorH <= 0f)
                    {
                        sm++;
                        continue;
                    }

                    props.Clear();
                    props.SetVector(TextureScaleOffsetId, s_identityScaleOffset);
                    props.SetTexture(MainTexId, tex);

                    _mesh.SetSubMesh(sm, new SubMeshDescriptor(
                        indexStart: globalIdxOffset + (int)pcmd.IdxOffset,
                        indexCount: (int)pcmd.ElemCount,
                        topology: MeshTopology.Triangles)
                    {
                        baseVertex = globalVtxOffset + (int)pcmd.VtxOffset
                    }, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);

                    // Unity scissor is bottom-left origin. Use RectInt to avoid float rounding issues.
                    cmd.EnableScissorRect(new Rect(
                        Mathf.FloorToInt(clipX1),
                        Mathf.FloorToInt(fbHeight - clipY2),
                        Mathf.CeilToInt(scissorW),
                        Mathf.CeilToInt(scissorH)
                    ));

                    cmd.DrawMesh(_mesh, Matrix4x4.identity, _material, submeshIndex: sm, shaderPass: 0, properties: props);

                    cmd.DisableScissorRect();

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

    /// <summary>
    /// Maps ImGui texture IDs (<see cref="IntPtr"/>) to Unity <see cref="Texture"/> instances.
    /// </summary>
    public static class ImGuiTextureRegistry
    {
        private static readonly Dictionary<IntPtr, Texture> IdToTexture = new();
        private static readonly Dictionary<Texture, IntPtr> TextureToId = new();
        private static int _nextId = 1;

        /// <summary>
        /// Returns a stable ID for the given texture. The ID can be stored in ImGui as TextureId.
        /// </summary>
        public static IntPtr GetId(Texture texture)
        {
            if (texture == null)
                return IntPtr.Zero;

            if (TextureToId.TryGetValue(texture, out var id))
                return id;

            id = new IntPtr(_nextId++);
            TextureToId[texture] = id;
            IdToTexture[id] = texture;
            return id;
        }

        /// <summary>
        /// Attempts to resolve a previously registered texture ID.
        /// </summary>
        public static bool TryGetTexture(IntPtr id, out Texture texture) =>
            IdToTexture.TryGetValue(id, out texture);

        /// <summary>
        /// Clears all registrations.
        /// </summary>
        internal static void Clear()
        {
            IdToTexture.Clear();
            TextureToId.Clear();
            _nextId = 1;
        }
    }
}
