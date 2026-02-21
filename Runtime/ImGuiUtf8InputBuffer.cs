using System;
using System.Text;
using ImGuiNET;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Helper for reading/writing the native UTF-8 backing buffer in ImGui InputText callbacks
    /// without allocating per callback.
    /// </summary>
    public static class ImGuiUtf8InputBuffer
    {
        private static readonly UTF8Encoding s_utf8 = new(false);
        private static byte[] s_utf8Buffer = new byte[2048];

        public static unsafe string Read(ImGuiInputTextCallbackData* data)
        {
            if (data->BufTextLen <= 0)
                return string.Empty;

            // Ensure scratch buffer capacity.
            if (s_utf8Buffer.Length < data->BufTextLen)
                s_utf8Buffer = new byte[Mathf.NextPowerOfTwo(data->BufTextLen)];

            for (var i = 0; i < data->BufTextLen; i++)
                s_utf8Buffer[i] = data->Buf[i];

            return s_utf8.GetString(s_utf8Buffer, 0, data->BufTextLen);
        }

        public static unsafe void Write(ImGuiInputTextCallbackData* data, string value)
        {
            value ??= string.Empty;

            // Encode into the reusable buffer.
            var maxBytes = data->BufSize > 0 ? data->BufSize - 1 : 0;
            var byteCount = s_utf8.GetByteCount(value);

            if (s_utf8Buffer.Length < byteCount)
                s_utf8Buffer = new byte[Mathf.NextPowerOfTwo(byteCount)];

            var bytesWritten = s_utf8.GetBytes(value, 0, value.Length, s_utf8Buffer, 0);
            var writeLen = Math.Min(bytesWritten, maxBytes);

            fixed (byte* src = s_utf8Buffer)
            {
                Buffer.MemoryCopy(src, data->Buf, data->BufSize, writeLen);
            }

            if (data->BufSize > 0)
                data->Buf[writeLen] = 0;

            data->BufTextLen = writeLen;
            data->CursorPos = writeLen;
            data->SelectionStart = writeLen;
            data->SelectionEnd = writeLen;
            data->BufDirty = 1;
        }
    }
}

