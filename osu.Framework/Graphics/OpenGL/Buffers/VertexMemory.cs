// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal class VertexMemory<T> : IDisposable
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly ArrayPool<byte> pool;
        private readonly byte[] bytes;

        public VertexMemory(ArrayPool<byte> pool, int count)
        {
            this.pool = pool;
            bytes = pool.Rent(count * VertexUtils<T>.STRIDE);
            bytes.AsSpan().Clear();
        }

        public Span<T> Span => MemoryMarshal.Cast<byte, T>(bytes);

        public void Dispose()
        {
            pool.Return(bytes);
        }
    }
}
