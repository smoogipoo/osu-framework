// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal static class VertexPool<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly ArrayPool<byte> pool;

        static VertexPool()
        {
            pool = ArrayPool<byte>.Create(2000 * VertexUtils<T>.STRIDE, 1000);
        }

        public static VertexMemory<T> Rent(int count) => new VertexMemory<T>(pool, count);
    }
}
