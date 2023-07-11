// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    internal class VeldridMaskingBuffer : IMaskingBuffer, IVeldridUniformBuffer
    {
        private readonly VeldridRenderer renderer;

        private readonly List<VeldridMaskingBufferStorage<ShaderMaskingInfo>> buffers = new List<VeldridMaskingBufferStorage<ShaderMaskingInfo>>();
        private int currentElementIndex = -1;

        public VeldridMaskingBuffer(VeldridRenderer renderer)
        {
            this.renderer = renderer;
            buffers.Add(new VeldridMaskingBufferStorage<ShaderMaskingInfo>(renderer));
        }

        public int Add(ShaderMaskingInfo maskingInfo)
        {
            if (currentElementIndex == -1)
            {
                // Signal the first use of this buffer.
                renderer.RegisterUniformBufferForReset(this);
            }
            else if ((currentElementIndex + 1) % buffers[0].Size == 0)
            {
                // If this invocation transitions to a new buffer, flush the pipeline.
                renderer.FlushCurrentBatch(FlushBatchSource.SetUniform);
            }

            // Move to a new element.
            ++currentElementIndex;

            int bufferIndex = currentElementIndex / buffers[0].Size;

            if (bufferIndex >= buffers.Count)
            {
                while (bufferIndex >= buffers.Count)
                    buffers.Add(new VeldridMaskingBufferStorage<ShaderMaskingInfo>(renderer));
            }

            VeldridMaskingBufferStorage<ShaderMaskingInfo> buffer = buffers[bufferIndex];

            int bufferOffset = currentElementIndex % buffer.Size;
            buffer[bufferOffset] = maskingInfo;

            return bufferOffset;
        }

        public ResourceSet GetResourceSet(ResourceLayout layout)
        {
            int bufferIndex = currentElementIndex / buffers[0].Size;
            return buffers[bufferIndex].GetResourceSet(layout);
        }

        public void ResetCounters()
        {
            currentElementIndex = -1;
        }

        public void Dispose()
        {
            foreach (var buffer in buffers)
                buffer.Dispose();
        }
    }

    internal class VeldridMaskingBufferStorage<TData> : IDisposable
        where TData : unmanaged, IEquatable<TData>
    {
        /// <summary>
        /// Number of masking infos per array buffer.
        /// </summary>
        /// <remarks>
        /// The OpenGL spec guarantees a minimum size of 128MB for this type of buffer. This may differ for other backends.
        /// The total size as measured here, is around 1.5MB.
        /// </remarks>
        public const int ARRAY_BUFFER_SIZE = 8192;

        /// <summary>
        /// Number of masking infos per uniform buffer.
        /// </summary>
        /// <remarks>
        /// The OpenGL spec guarantees a minimum size of 16KB for this type of buffer. This may differ for other backends.
        /// The total size as measured here, is around 12KB.
        /// </remarks>
        public const int UNIFORM_BUFFER_SIZE = 64;

        public readonly int Size;

        private readonly TData[] data;
        private readonly DeviceBuffer buffer;
        private readonly VeldridRenderer renderer;
        private readonly uint elementSize;

        public VeldridMaskingBufferStorage(VeldridRenderer renderer)
        {
            this.renderer = renderer;

            elementSize = (uint)Marshal.SizeOf(default(ShaderMaskingInfo));

            if (renderer.UseStructuredBuffers)
            {
                Size = ARRAY_BUFFER_SIZE;
                buffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)(elementSize * Size), BufferUsage.StructuredBufferReadOnly | BufferUsage.Dynamic, elementSize));
            }
            else
            {
                Size = UNIFORM_BUFFER_SIZE;
                buffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)(elementSize * Size), BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            }

            data = new TData[Size];
        }

        private int changeBeginIndex = -1;
        private int changeCount;

        public TData this[int index]
        {
            get => data[index];
            set
            {
                if (data[index].Equals(value))
                    return;

                data[index] = value;

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

            renderer.BufferUpdateCommands.UpdateBuffer(buffer, (uint)(changeBeginIndex * elementSize), data.AsSpan().Slice(changeBeginIndex, changeCount));

            changeBeginIndex = -1;
            changeCount = 0;
        }

        public ResourceSet GetResourceSet(ResourceLayout layout)
        {
            flushChanges();
            return renderer.Factory.CreateResourceSet(new ResourceSetDescription(layout, buffer));
        }

        public void Dispose()
        {
            buffer.Dispose();
        }
    }
}
