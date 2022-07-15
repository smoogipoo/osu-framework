// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.OpenGL.Buffers;
using osuTK.Graphics.ES30;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Rendering;

namespace osu.Framework.Graphics.Batches
{
    internal class LinearBatch<T> : VertexBatch<T>
        where T : struct, IEquatable<T>, IVertex
    {
        private readonly PrimitiveType type;

        public LinearBatch(IRenderer renderer, int size, int maxBuffers, PrimitiveType type)
            : base(renderer, size, maxBuffers)
        {
            this.type = type;
        }

        protected override VertexBuffer<T> CreateVertexBuffer(IRenderer renderer) => new LinearVertexBuffer<T>(renderer, Size, type, BufferUsageHint.DynamicDraw);
    }
}
