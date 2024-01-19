// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Platform;
using osu.Framework.Utils;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred.Allocation
{
    internal class UniformBufferManager
    {
        private const int buffer_size = 1024 * 1024; // 1MB per UBO (these are pretty small).

        private readonly DeferredRenderer renderer;
        private readonly List<DeviceBuffer> buffers = new List<DeviceBuffer>();

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

            int writeIndex = currentWriteIndex;
            memory.WriteTo(renderer, buffers[currentBuffer], writeIndex);

            uint alignment = renderer.Device.UniformBufferMinOffsetAlignment;
            currentWriteIndex = MathUtils.DivideRoundUp(currentWriteIndex + memory.Length, (int)alignment) * (int)alignment;

            return new UniformBufferReference(currentBuffer, writeIndex);
        }

        public DeviceBuffer GetBuffer(UniformBufferReference reference) => buffers[reference.BufferId];

        public uint GetOffset(UniformBufferReference reference) => (uint)reference.Index;

        public void Reset()
        {
            currentBuffer = 0;
            currentWriteIndex = 0;
        }
    }

    public readonly record struct UniformBufferReference(int BufferId, int Index);
}
