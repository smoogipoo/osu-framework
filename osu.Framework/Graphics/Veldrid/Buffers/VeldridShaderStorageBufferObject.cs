// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Veldrid.Buffers.Staging;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    internal class VeldridShaderStorageBufferObject<TData> : IShaderStorageBufferObject<TData>, IVeldridUniformBuffer
        where TData : unmanaged, IEquatable<TData>
    {
        public int Size { get; }

        private readonly IStagingBuffer<TData> stagingBuffer;
        private readonly DeviceBuffer deviceBuffer;
        private readonly VeldridRenderer renderer;

        public VeldridShaderStorageBufferObject(VeldridRenderer renderer, int uboSize, int ssboSize)
        {
            this.renderer = renderer;

            uint elementSize = (uint)Marshal.SizeOf(default(TData));

            if (renderer.UseStructuredBuffers)
            {
                Size = ssboSize;
                deviceBuffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)(elementSize * Size), BufferUsage.StructuredBufferReadOnly | BufferUsage.Dynamic, elementSize, true));
            }
            else
            {
                Size = uboSize;
                deviceBuffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)(elementSize * Size), BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            }

            stagingBuffer = renderer.CreateStagingBuffer<TData>((uint)Size);
        }

        private int changeBeginIndex = -1;
        private int changeCount;

        public TData this[int index]
        {
            get => stagingBuffer.Data[index];
            set
            {
                if (stagingBuffer.Data[index].Equals(value))
                    return;

                stagingBuffer.Data[index] = value;

                if (changeBeginIndex == -1)
                {
                    // If this is the first upload, nothing more needs to be done.
                    changeBeginIndex = index;
                }
                else
                {
                    // If this is not the first upload, then we need to check if this index is contiguous with the previous changes.
                    if (index != changeBeginIndex + changeCount)
                    {
                        // This index is not contiguous. Flush the current uploads and start a new change set.
                        flushChanges();
                        changeBeginIndex = index;
                    }
                }

                changeCount++;
            }
        }

        private void flushChanges()
        {
            if (changeBeginIndex == -1)
                return;

            stagingBuffer.CopyTo(deviceBuffer, (uint)changeBeginIndex, (uint)changeBeginIndex, (uint)changeCount);

            changeBeginIndex = -1;
            changeCount = 0;
        }

        public ResourceSet GetResourceSet(ResourceLayout layout)
        {
            flushChanges();
            return renderer.Factory.CreateResourceSet(new ResourceSetDescription(layout, deviceBuffer));
        }

        public void ResetCounters()
        {
        }

        public void Dispose()
        {
            deviceBuffer.Dispose();
        }
    }
}
