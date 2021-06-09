// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal static class VertexPool<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        public static VertexMemory<T> Rent(int count) => new VertexMemory<T>(count);
    }
}
