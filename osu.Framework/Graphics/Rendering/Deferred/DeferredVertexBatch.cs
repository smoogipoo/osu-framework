// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Rendering.Vertices;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    public class DeferredVertexBatch<TVertex> : IVertexBatch<TVertex>
        where TVertex : unmanaged, IEquatable<TVertex>, IVertex
    {
        public readonly IVertexBatch<TVertex> Resource;

        public Action<TVertex> AddAction { get; }

        private readonly DeferredRenderer renderer;

        public DeferredVertexBatch(DeferredRenderer renderer, IVertexBatch<TVertex> vertexBatch)
        {
            this.renderer = renderer;
            Resource = vertexBatch;

            AddAction = Add;
        }

        public int Size => int.MaxValue;

        public int Draw()
        {
            renderer.RenderEvents.Add(new DrawVertexBatchEvent<TVertex>(this));
            return 0;
        }

        public void Add(TVertex vertex) => renderer.RenderEvents.Add(new AddVertexToBatchEvent<TVertex>(this, vertex));

        public void ResetCounters()
        {
        }

        public void Dispose()
        {
            // Todo:
        }
    }
}
