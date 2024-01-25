// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Veldrid.Pipelines;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal interface IDeferredVertexBatch
    {
        void Write(in MemoryReference primitive);
        void Draw(GraphicsPipeline pipeline, int count);
    }

    internal class DeferredVertexBatch<TVertex> : IVertexBatch<TVertex>, IDeferredVertexBatch
        where TVertex : unmanaged, IEquatable<TVertex>, IVertex
    {
        private static readonly TVertex[] current_primitive = new TVertex[4];

        // ReSharper disable once StaticMemberInGenericType
        private static int currentPrimitiveSize;

        public Action<TVertex> AddAction { get; }

        private readonly DeferredRenderer renderer;
        private readonly VertexManager vertexManager;

        private readonly PrimitiveTopology topology;
        private readonly IndexLayout indexLayout;
        private readonly int primitiveSize;

        private int currentDrawCount;

        public DeferredVertexBatch(DeferredRenderer renderer, VertexManager vertexManager, PrimitiveTopology topology, IndexLayout indexLayout)
        {
            this.renderer = renderer;
            this.vertexManager = vertexManager;

            this.topology = topology;
            this.indexLayout = indexLayout;

            if (this.indexLayout == IndexLayout.Linear)
            {
                primitiveSize = this.topology switch
                {
                    PrimitiveTopology.Points => 1,
                    PrimitiveTopology.Lines => 2,
                    PrimitiveTopology.LineStrip => throw new NotSupportedException(),
                    PrimitiveTopology.Triangles => 3,
                    PrimitiveTopology.TriangleStrip => throw new NotSupportedException(),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            else
                primitiveSize = 4;

            AddAction = ((IVertexBatch<TVertex>)this).Add;
        }

        public void Write(in MemoryReference primitive) => vertexManager.Write(primitive);

        public void Draw(GraphicsPipeline pipeline, int count) => vertexManager.Draw<TVertex>(pipeline, count, topology, indexLayout, primitiveSize);

        int IVertexBatch.Size => int.MaxValue;

        int IVertexBatch.Draw()
        {
            int count = currentDrawCount;
            currentDrawCount = 0;

            if (count == 0)
                return 0;

            renderer.DrawVertices(topology, 0, count);
            renderer.EnqueueEvent(new FlushEvent(renderer.Reference(this), count));
            return count;
        }

        void IVertexBatch.ResetCounters()
        {
            currentPrimitiveSize = 0;
            currentDrawCount = 0;
        }

        void IVertexBatch<TVertex>.Add(TVertex vertex)
        {
            renderer.SetActiveBatch(this);

            current_primitive[currentPrimitiveSize] = vertex;

            if (++currentPrimitiveSize == primitiveSize)
            {
                renderer.EnqueueEvent(AddPrimitiveToBatchEvent.Create(renderer, this, current_primitive.AsSpan()[..primitiveSize]));
                currentPrimitiveSize = 0;
            }

            currentDrawCount++;
        }

        public void Dispose()
        {
        }
    }
}
