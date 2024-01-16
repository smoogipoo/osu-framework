// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Veldrid;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal interface IDeferredVertexBatch
    {
        PrimitiveTopology Topology { get; }
        IndexLayout IndexLayout { get; }

        int PrimitiveSize { get; }

        void WritePrimitive(RendererStagingMemoryBlock primitive, CommandList commandList);
        void Draw(VeldridRenderer veldridRenderer, int count);
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

        private int currentDrawCount;

        public DeferredVertexBatch(DeferredRenderer renderer, VertexManager vertexManager, PrimitiveTopology topology, IndexLayout indexLayout)
        {
            this.renderer = renderer;
            this.vertexManager = vertexManager;

            Topology = topology;
            IndexLayout = indexLayout;

            if (IndexLayout == IndexLayout.Linear)
            {
                PrimitiveSize = Topology switch
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
                PrimitiveSize = 4;

            AddAction = ((IVertexBatch<TVertex>)this).Add;
        }

        public PrimitiveTopology Topology { get; }

        public IndexLayout IndexLayout { get; }

        public int PrimitiveSize { get; }

        public void WritePrimitive(RendererStagingMemoryBlock primitive, CommandList commandList) => vertexManager.Commit(primitive, commandList);

        public void Draw(VeldridRenderer veldridRenderer, int count) => vertexManager.Draw<TVertex>(veldridRenderer, count, Topology, IndexLayout, PrimitiveSize);

        int IVertexBatch.Size => int.MaxValue;

        int IVertexBatch.Draw()
        {
            int count = currentDrawCount;
            currentDrawCount = 0;

            if (count == 0)
                return 0;

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
            current_primitive[currentPrimitiveSize] = vertex;

            if (++currentPrimitiveSize == PrimitiveSize)
            {
                renderer.EnqueueEvent(AddPrimitiveToBatchEvent.Create(renderer, this, current_primitive.AsSpan()[..PrimitiveSize]));
                currentPrimitiveSize = 0;
            }

            currentDrawCount++;
        }

        public void Dispose()
        {
        }
    }
}
