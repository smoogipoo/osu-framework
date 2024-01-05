// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Veldrid.Vertices;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    public interface IAddVertexToBatchEvent
    {
        int Stride { get; }

        void CopyTo(Span<byte> buffer);
    }

    public readonly record struct AddVertexToBatchEvent<TVertex>(DeferredVertexBatch<TVertex> VertexBatch, TVertex Vertex) : IEvent, IAddVertexToBatchEvent
        where TVertex : unmanaged, IEquatable<TVertex>, IVertex
    {
        public int Stride => VeldridVertexUtils<TVertex>.STRIDE;

        public void CopyTo(Span<byte> buffer)
        {
            MemoryMarshal.Cast<byte, TVertex>(buffer)[0] = Vertex;
        }

        public void Run(DeferredRenderer current, IRenderer target) => throw new NotSupportedException();
    }
}
