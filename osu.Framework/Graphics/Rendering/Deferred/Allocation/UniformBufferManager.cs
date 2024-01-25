// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Platform;
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

        private readonly DeferredRenderer renderer;
        private readonly List<DeviceBuffer> buffers = new List<DeviceBuffer>();
        private readonly List<MappedResource> mappedBuffers = new List<MappedResource>();

        private int currentBuffer;
        private int currentWriteIndex;

        public UniformBufferManager(DeferredRenderer renderer)
        {
            this.renderer = renderer;
        }

        public UniformBufferReference Write(MemoryReference memory)
        {
            if (currentWriteIndex + memory.Length > buffer_size)
            {
                currentBuffer++;
                currentWriteIndex = 0;
            }

            if (currentBuffer == buffers.Count)
            {
                buffers.Add(renderer.Factory.CreateBuffer(new BufferDescription(buffer_size, BufferUsage.UniformBuffer | BufferUsage.Dynamic)));
                NativeMemoryTracker.AddMemory(this, buffer_size);
            }

            if (currentBuffer == mappedBuffers.Count)
                mappedBuffers.Add(renderer.Device.Map(buffers[currentBuffer], MapMode.Write));

            memory.WriteTo(renderer, mappedBuffers[currentBuffer], currentWriteIndex);

            int alignment = (int)renderer.Device.UniformBufferMinOffsetAlignment;
            int alignedLength = MathUtils.DivideRoundUp(memory.Length, alignment) * alignment;

            int writeIndex = currentWriteIndex;
            currentWriteIndex += alignedLength;

            return new UniformBufferReference(
                new UniformBufferChunk(
                    currentBuffer,
                    writeIndex / buffer_chunk_size * buffer_chunk_size,
                    Math.Min(buffer_chunk_size, buffer_size - writeIndex)),
                writeIndex % buffer_chunk_size);
        }

        public void Commit()
        {
            foreach (var b in mappedBuffers)
                renderer.Device.Unmap(b.Resource);

            mappedBuffers.Clear();
        }

        public DeviceBuffer GetBuffer(UniformBufferReference reference) => buffers[reference.Chunk.BufferId];

        public void Reset()
        {
            currentBuffer = 0;
            currentWriteIndex = 0;

            Debug.Assert(mappedBuffers.Count == 0);
        }
    }

    public readonly record struct UniformBufferChunk(int BufferId, int Offset, int Size);

    public readonly record struct UniformBufferReference(UniformBufferChunk Chunk, int OffsetInChunk);
}
