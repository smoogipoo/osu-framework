// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Rendering;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Batches
{
    public class QuadBatch<T> : VertexBatch<T>
        where T : struct, IEquatable<T>, IVertex
    {
        public QuadBatch(IRenderer renderer, int size, int maxBuffers)
            : base(renderer, size, maxBuffers)
        {
            if (size > QuadVertexBuffer<T>.MAX_QUADS)
                throw new OverflowException($"Attempted to initialise a {nameof(QuadVertexBuffer<T>)} with more than {nameof(QuadVertexBuffer<T>)}.{nameof(QuadVertexBuffer<T>.MAX_QUADS)} quads ({QuadVertexBuffer<T>.MAX_QUADS}).");
        }

        protected override VertexBuffer<T> CreateVertexBuffer(IRenderer renderer) => new QuadVertexBuffer<T>(renderer, Size, BufferUsageHint.DynamicDraw);
    }
}
