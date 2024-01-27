// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Utils;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred.Allocation
{
    internal class UniformBufferManager
    {
        private const int buffer_size = 1024 * 1024; // 1MB per UBO (these are pretty small).

        /// <summary>
        /// The UBO is split and bound in 65K chunks, which is the maximum supported by D3D11.
        /// </summary>
        private const int buffer_chunk_size = 65536;

        private readonly DeferredContext context;

        private readonly DeferredBufferPool uniformBufferPool;
        private readonly List<PooledBuffer> inUseBuffers = new List<PooledBuffer>();
        private readonly List<MappedResource> mappedBuffers = new List<MappedResource>();

        private int currentBuffer;
        private int currentWriteIndex;

        public UniformBufferManager(DeferredContext context)
        {
            this.context = context;
            uniformBufferPool = new DeferredBufferPool(context, buffer_size, BufferUsage.UniformBuffer, nameof(UniformBufferManager));
        }

        public UniformBufferReference Write(in MemoryReference memory)
        {
            if (currentWriteIndex + memory.Length > buffer_size)
            {
                currentBuffer++;
                currentWriteIndex = 0;
            }

            if (currentBuffer == inUseBuffers.Count)
            {
                PooledBuffer newBuffer = uniformBufferPool.Get(context);

                inUseBuffers.Add(newBuffer);
                mappedBuffers.Add(context.Device.Map(newBuffer.Buffer, MapMode.Write));
            }

            memory.WriteTo(context, mappedBuffers[currentBuffer], currentWriteIndex);

            int alignment = (int)context.Device.UniformBufferMinOffsetAlignment;
            int alignedLength = MathUtils.DivideRoundUp(memory.Length, alignment) * alignment;

            int writeIndex = currentWriteIndex;
            currentWriteIndex += alignedLength;

            if (context.Device.Features.BufferRangeBinding)
            {
                return new UniformBufferReference(
                    new UniformBufferChunk(
                        inUseBuffers[currentBuffer].Buffer,
                        writeIndex / buffer_chunk_size * buffer_chunk_size,
                        Math.Min(buffer_chunk_size, buffer_size - writeIndex)),
                    writeIndex % buffer_chunk_size);
            }

            return new UniformBufferReference(
                new UniformBufferChunk(
                    inUseBuffers[currentBuffer].Buffer,
                    0,
                    buffer_size),
                writeIndex);
        }

        public void Commit()
        {
            foreach (var b in mappedBuffers)
                context.Device.Unmap(b.Resource);

            mappedBuffers.Clear();
        }

        public void Reset()
        {
            uniformBufferPool.NewFrame();
            inUseBuffers.Clear();

            currentBuffer = 0;
            currentWriteIndex = 0;

            Debug.Assert(mappedBuffers.Count == 0);
        }
    }

    public readonly record struct UniformBufferChunk(DeviceBuffer Buffer, int Offset, int Size);

    public readonly record struct UniformBufferReference(UniformBufferChunk Chunk, int OffsetInChunk);
}
