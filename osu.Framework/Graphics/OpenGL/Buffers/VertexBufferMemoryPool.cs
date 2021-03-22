// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal static class VertexBufferMemoryPool<T>
        where T : struct, IEquatable<T>, IVertex
    {
        private const int max_vertices_per_buffer = 1000;

        // ReSharper disable once StaticMemberInGenericType
        private static readonly ArrayPool<byte> pool;

        static VertexBufferMemoryPool()
        {
            pool = ArrayPool<byte>.Create(max_vertices_per_buffer * VertexUtils<T>.STRIDE, 1000);
        }

        public static VertexMemory<T> Rent(int count) => new VertexMemory<T>(pool, count);
    }
}
