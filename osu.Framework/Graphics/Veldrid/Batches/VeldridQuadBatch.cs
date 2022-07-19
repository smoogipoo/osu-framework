// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.Veldrid.Buffers;
using BufferUsage = Veldrid.BufferUsage;

namespace osu.Framework.Graphics.Veldrid.Batches
{
    public class VeldridQuadBatch<T> : VertexBatch<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly VeldridRenderer renderer;

        public VeldridQuadBatch(VeldridRenderer renderer, int size, int maxBuffers)
            : base(renderer, size, maxBuffers)
        {
            this.renderer = renderer;

            if (size > VeldridQuadVertexBuffer<T>.MAX_QUADS)
                throw new OverflowException($"Attempted to initialise a {nameof(VeldridQuadVertexBuffer<T>)} with more than {nameof(VeldridQuadVertexBuffer<T>)}.{nameof(VeldridQuadVertexBuffer<T>.MAX_QUADS)} quads ({VeldridQuadVertexBuffer<T>.MAX_QUADS}).");
        }

        protected override IVertexBuffer<T> CreateVertexBuffer() => new VeldridQuadVertexBuffer<T>(renderer, Size, BufferUsage.Dynamic);
    }
}
