using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Maps ImGui texture IDs (<see cref="IntPtr"/>) to Unity <see cref="Texture"/> instances.
    /// </summary>
    public static class ImguiTextureRegistry
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
