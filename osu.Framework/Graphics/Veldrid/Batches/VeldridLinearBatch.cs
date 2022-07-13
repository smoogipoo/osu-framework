// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.Veldrid.Buffers;
using Veldrid;
using BufferUsage = Veldrid.BufferUsage;

namespace osu.Framework.Graphics.Veldrid.Batches
{
    public class VeldridLinearBatch<T> : VertexBatch<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly VeldridRenderer renderer;
        private readonly PrimitiveTopology type;

        public VeldridLinearBatch(VeldridRenderer renderer, int size, int maxBuffers, PrimitiveTopology type)
            : base(renderer, size, maxBuffers)
        {
            this.renderer = renderer;
            this.type = type;
        }

        protected override IVertexBuffer<T> CreateVertexBuffer() => new VeldridLinearVertexBuffer<T>(renderer, Size, type, BufferUsage.Dynamic);
    }
}
