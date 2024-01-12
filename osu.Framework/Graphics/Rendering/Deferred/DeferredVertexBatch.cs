// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Veldrid;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Graphics.Veldrid.Vertices;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal interface IDeferredVertexBatch
    {
        void Write(RendererStagingMemoryBlock block, CommandList commandList);
        void Draw(VeldridRenderer veldridRenderer, int endIndex);
        void ResetCounters();
    }

    internal readonly record struct DeferredVertexBatchLookup(Type VertexType, PrimitiveTopology Topology, IndexLayout IndexLayout);

    internal class DeferredVertexBatch<TVertex> : IVertexBatch<TVertex>, IDeferredVertexBatch
        where TVertex : unmanaged, IEquatable<TVertex>, IVertex
    {
        public Action<TVertex> AddAction { get; }

        public readonly int Size;

        private readonly DeferredRenderer renderer;
        private readonly PrimitiveTopology topology;
        private readonly IndexLayout indexLayout;

        private readonly List<VeldridVertexBuffer2> buffers = new List<VeldridVertexBuffer2>();
        private int currentBuffer;
        private int currentWriteIndex;
        private int currentDrawIndex;

        public DeferredVertexBatch(DeferredRenderer renderer, PrimitiveTopology topology, IndexLayout indexLayout)
        {
            this.renderer = renderer;
            this.topology = topology;
            this.indexLayout = indexLayout;

            Size = indexLayout switch
            {
                IndexLayout.Linear => IRenderer.MAX_VERTICES,
                IndexLayout.Quad => IRenderer.MAX_QUADS * IRenderer.VERTICES_PER_QUAD,
                _ => throw new ArgumentOutOfRangeException(nameof(indexLayout), indexLayout, null)
            };

            AddAction = ((IVertexBatch<TVertex>)this).Add;
        }

        public void Write(RendererStagingMemoryBlock block, CommandList commandList)
        {
            if (currentWriteIndex == Size)
            {
                currentBuffer++;
                currentWriteIndex = 0;
            }

            if (currentBuffer == buffers.Count)
                buffers.Add(new VeldridVertexBuffer2(renderer, Size, VeldridVertexUtils<TVertex>.STRIDE, VeldridVertexUtils<TVertex>.Layout));

            buffers[currentBuffer].Write(block, commandList, currentWriteIndex++);
        }

        public void Draw(VeldridRenderer veldridRenderer, int count)
        {
            VeldridIndexLayout veldridLayout = indexLayout switch
            {
                IndexLayout.Linear => VeldridIndexLayout.Linear,
                IndexLayout.Quad => VeldridIndexLayout.Quad,
                _ => throw new InvalidOperationException()
            };

            while (count > 0)
            {
                int bufferIndex = currentDrawIndex / Size;
                int indexInBuffer = currentDrawIndex % Size;
                int countToDraw = Math.Min(count, Size);

                veldridRenderer.BindVertexBuffer(buffers[bufferIndex]);
                veldridRenderer.BindIndexBuffer(veldridLayout, Size);
                veldridRenderer.DrawVertices(topology.ToPrimitiveTopology(), indexInBuffer, count);

                currentDrawIndex += countToDraw;
                count -= countToDraw;
            }
        }

        public void ResetCounters()
        {
            currentBuffer = 0;
            currentWriteIndex = 0;
            currentDrawIndex = 0;
        }

        int IVertexBatch.Size => int.MaxValue;

        int IVertexBatch.Draw()
        {
            renderer.EnqueueEvent(new DrawVertexBatchEvent());
            return 0;
        }

        void IVertexBatch<TVertex>.Add(TVertex vertex)
        {
            renderer.EnqueueEvent(AddVertexToBatchEvent.Create(renderer, this, vertex));
        }

        public void Dispose()
        {
        }
    }
}
