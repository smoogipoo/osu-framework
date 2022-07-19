// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL.Buffers;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Batches
{
    public class QuadBatch<T> : VertexBatch<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly OpenGLRenderer renderer;

        public QuadBatch(OpenGLRenderer renderer, int size, int maxBuffers)
            : base(renderer, size, maxBuffers)
        {
            this.renderer = renderer;

            if (size > QuadVertexBuffer<T>.MAX_QUADS)
                throw new OverflowException($"Attempted to initialise a {nameof(QuadVertexBuffer<T>)} with more than {nameof(QuadVertexBuffer<T>)}.{nameof(QuadVertexBuffer<T>.MAX_QUADS)} quads ({QuadVertexBuffer<T>.MAX_QUADS}).");
        }

        protected override IVertexBuffer<T> CreateVertexBuffer() => new QuadVertexBuffer<T>(renderer, Size, BufferUsageHint.DynamicDraw);
    }
}
