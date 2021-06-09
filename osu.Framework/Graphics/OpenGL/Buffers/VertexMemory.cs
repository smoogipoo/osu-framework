// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal class VertexMemory<T> : IDisposable
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly IntPtr bytes;
        private readonly int count;
        private readonly int totalBytes;

        public unsafe VertexMemory(int count)
        {
            this.count = count;

            totalBytes = count * sizeof(T);
            bytes = Marshal.AllocHGlobal(totalBytes);
            // GC.AddMemoryPressure(totalBytes);

            Span.Clear();
        }

        public unsafe Span<T> Span => new Span<T>(bytes.ToPointer(), count);

        public void Dispose()
        {
            Marshal.FreeHGlobal(bytes);
            // GC.RemoveMemoryPressure(totalBytes);
        }
    }
}
