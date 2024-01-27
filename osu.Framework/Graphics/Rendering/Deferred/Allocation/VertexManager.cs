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
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osu.Framework.Utils;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred.Allocation
{
    internal class VertexManager
    {
        private const int buffer_size = 2 * 1024 * 1024; // 2MB per VBO.

        private readonly DeferredContext context;
        private readonly List<DeviceBuffer> buffers = new List<DeviceBuffer>();
        private readonly VeldridIndexBuffer?[] indexBuffers = new VeldridIndexBuffer?[2];
        private readonly List<MappedResource> mappedBuffers = new List<MappedResource>();

        private int currentBuffer;
        private int currentWriteIndex;
        private int currentDrawIndex;

        public VertexManager(DeferredContext context)
        {
            this.context = context;
        }

        public void Write(in MemoryReference primitive)
        {
            if (currentWriteIndex + primitive.Length > buffer_size)
            {
                currentBuffer++;
                currentWriteIndex = 0;
            }

            if (currentBuffer == buffers.Count)
            {
                buffers.Add(context.Factory.CreateBuffer(new BufferDescription(buffer_size, BufferUsage.VertexBuffer | BufferUsage.Dynamic)));
                NativeMemoryTracker.AddMemory(this, buffer_size);
            }

            if (currentBuffer == mappedBuffers.Count)
                mappedBuffers.Add(context.Device.Map(buffers[currentBuffer], MapMode.Write));

            primitive.WriteTo(context, mappedBuffers[currentBuffer], currentWriteIndex);
            currentWriteIndex += primitive.Length;

            FrameStatistics.Increment(StatisticsCounterType.VerticesUpl);
        }

        public void Commit()
        {
            foreach (var b in mappedBuffers)
                context.Device.Unmap(b.Resource);

            mappedBuffers.Clear();
        }

        public void Draw<T>(GraphicsPipeline pipeline, int count, PrimitiveTopology topology, IndexLayout indexLayout, int primitiveSize)
            where T : unmanaged, IEquatable<T>, IVertex
        {
            int primitiveByteSize = primitiveSize * VeldridVertexUtils<T>.STRIDE;

            ref VeldridIndexBuffer? indexBuffer = ref indexBuffers[(int)indexLayout];

            indexBuffer ??= indexLayout switch
            {
                IndexLayout.Linear => new VeldridIndexBuffer(context.Renderer, VeldridIndexLayout.Linear, IRenderer.MAX_VERTICES),
                IndexLayout.Quad => new VeldridIndexBuffer(context.Renderer, VeldridIndexLayout.Quad, IRenderer.MAX_QUADS * IRenderer.VERTICES_PER_QUAD),
                _ => throw new ArgumentOutOfRangeException(nameof(indexLayout), indexLayout, null)
            };

            pipeline.SetIndexBuffer(indexBuffer);

            while (count > 0)
            {
                int bufferIndex;
                int indexInBuffer;

                // Jump to the next buffer index if the current draw call can't draw at least one primitive with the remaining data.
                // move to the next buffer index.
                if (buffer_size - currentDrawIndex % buffer_size < primitiveByteSize)
                {
                    bufferIndex = MathUtils.DivideRoundUp(currentDrawIndex, buffer_size);
                    indexInBuffer = 0;
                    currentDrawIndex = bufferIndex * buffer_size;
                }
                else
                {
                    bufferIndex = currentDrawIndex / buffer_size;
                    indexInBuffer = currentDrawIndex % buffer_size;
                }

                // Each draw call can only draw a certain number of vertices. This is the minimum of:
                // 1. The amount of vertices requested.
                // 2. The amount of vertices that can be drawn given the index buffer (generally a ushort, so capped to 65535 vertices).
                // 3. The amount of primitives that can be drawn. Each draw call must form complete primitives.
                int remainingPrimitives = (buffer_size - currentDrawIndex % buffer_size) / primitiveByteSize;
                int countToDraw = Math.Min(remainingPrimitives * primitiveSize, Math.Min(count, indexBuffer.VertexCapacity));

                // Bind the vertex buffer.
                pipeline.SetVertexBuffer(buffers[bufferIndex], VeldridVertexUtils<T>.Layout, (uint)indexInBuffer);
                pipeline.DrawVertices(topology.ToPrimitiveTopology(), 0, countToDraw);

                currentDrawIndex += countToDraw * VeldridVertexUtils<T>.STRIDE;
                count -= countToDraw;
            }
        }

        public void Reset()
        {
            currentBuffer = 0;
            currentWriteIndex = 0;
            currentDrawIndex = 0;

            Debug.Assert(mappedBuffers.Count == 0);
        }
    }
}
