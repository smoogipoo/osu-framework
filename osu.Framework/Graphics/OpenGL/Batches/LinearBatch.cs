// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL.Buffers;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Batches
{
    internal class LinearBatch<T> : VertexBatch<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly OpenGLRenderer renderer;
        private readonly PrimitiveType type;

        public LinearBatch(OpenGLRenderer renderer, int size, int maxBuffers, PrimitiveType type)
            : base(renderer, size, maxBuffers)
        {
            this.renderer = renderer;
            this.type = type;
        }

        protected override IVertexBuffer<T> CreateVertexBuffer() => new LinearVertexBuffer<T>(renderer, Size, type, BufferUsageHint.DynamicDraw);
    }
}
