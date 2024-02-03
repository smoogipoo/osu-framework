// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Veldrid;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Graphics.Veldrid.Pipelines;
using osu.Framework.Graphics.Veldrid.Vertices;
using osu.Framework.Statistics;
using osu.Framework.Utils;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred.Allocation
{
    internal class VertexManager
    {
        private const int buffer_size = 2 * 1024 * 1024; // 2MB per VBO.

        private readonly DeferredContext context;

        private readonly DeferredBufferPool vertexBufferPool;
        private readonly List<PooledBuffer> inUseBuffers = new List<PooledBuffer>();
        private readonly List<MappedResource> mappedBuffers = new List<MappedResource>();
        private readonly VeldridIndexBuffer?[] indexBuffers = new VeldridIndexBuffer?[2];

        private int currentWriteBuffer;
        private int currentWriteIndex;

        private int currentDrawBuffer;
        private int currentDrawIndex;

        public VertexManager(DeferredContext context)
        {
            this.context = context;
            vertexBufferPool = new DeferredBufferPool(context, buffer_size, BufferUsage.VertexBuffer, nameof(VertexManager));
        }

        public void Write<T>(in MemoryReference primitive)
            where T : unmanaged, IEquatable<T>, IVertex
        {
            // Make sure vertices are aligned to their strides.
            int vertexStride = VeldridVertexUtils<T>.STRIDE;
            currentWriteIndex = MathUtils.DivideRoundUp(currentWriteIndex, vertexStride) * vertexStride;

            if (currentWriteIndex + primitive.Length >= buffer_size)
            {
                currentWriteBuffer++;
                currentWriteIndex = 0;
            }

            if (currentWriteBuffer == inUseBuffers.Count)
            {
                PooledBuffer newBuffer = vertexBufferPool.Get(context);

                inUseBuffers.Add(newBuffer);
                mappedBuffers.Add(context.Device.Map(newBuffer.Buffer, MapMode.Write));
            }

            primitive.WriteTo(context, mappedBuffers[^1], currentWriteIndex);
            currentWriteIndex += primitive.Length;

            FrameStatistics.Increment(StatisticsCounterType.VerticesUpl);
        }

        public void Commit()
        {
            foreach (var b in mappedBuffers)
                context.Device.Unmap(b.Resource);

            mappedBuffers.Clear();
        }

        public void Draw<T>(GraphicsPipeline pipeline, int vertexCount, PrimitiveTopology topology, IndexLayout indexLayout, int primitiveSize)
            where T : unmanaged, IEquatable<T>, IVertex
        {
            if (vertexCount == 0)
                return;

            int vertexStride = VeldridVertexUtils<T>.STRIDE;
            int primitiveByteSize = primitiveSize * vertexStride;

            ref VeldridIndexBuffer? indexBuffer = ref indexBuffers[(int)indexLayout];
            indexBuffer ??= indexLayout switch
            {
                IndexLayout.Linear => new VeldridIndexBuffer(context.Renderer, VeldridIndexLayout.Linear, IRenderer.MAX_VERTICES),
                IndexLayout.Quad => new VeldridIndexBuffer(context.Renderer, VeldridIndexLayout.Quad, IRenderer.MAX_QUADS * IRenderer.VERTICES_PER_QUAD),
                _ => throw new ArgumentOutOfRangeException(nameof(indexLayout), indexLayout, null)
            };

            pipeline.SetIndexBuffer(indexBuffer);

            while (vertexCount > 0)
            {
                // Make sure vertices are aligned to their strides.
                currentDrawIndex = MathUtils.DivideRoundUp(currentDrawIndex, vertexStride) * vertexStride;

                // Jump to the next buffer index if the current draw call can't draw at least one primitive with the remaining data.
                if (currentDrawIndex + primitiveByteSize >= buffer_size)
                {
                    currentDrawBuffer++;
                    currentDrawIndex = 0;
                }

                // Each draw call can only draw a certain number of vertices. This is the minimum of:
                // 1. The amount of vertices requested.
                // 2. The amount of vertices that can be drawn given the index buffer (generally a ushort, so capped to 65535 vertices).
                // 3. The amount of primitives that can be drawn. Each draw call must form complete primitives.
                int maxPrimitives = (buffer_size - currentDrawIndex) / primitiveByteSize;
                int verticesToDraw = Math.Min(maxPrimitives * primitiveSize, Math.Min(vertexCount, indexBuffer.VertexCapacity));
                int vertexOffset = currentDrawIndex / vertexStride;

                // Bind the vertex buffer.
                pipeline.SetVertexBuffer(inUseBuffers[currentDrawBuffer].Buffer, VeldridVertexUtils<T>.Layout);
                pipeline.DrawVertices(topology.ToPrimitiveTopology(), 0, verticesToDraw, vertexOffset);

                currentDrawIndex += verticesToDraw * vertexStride;
                vertexCount -= verticesToDraw;
            }
        }

        public void Reset()
        {
            vertexBufferPool.NewFrame();
            inUseBuffers.Clear();

            currentWriteBuffer = 0;
            currentWriteIndex = 0;
            currentDrawBuffer = 0;
            currentDrawIndex = 0;

            Debug.Assert(mappedBuffers.Count == 0);
        }
    }
}
