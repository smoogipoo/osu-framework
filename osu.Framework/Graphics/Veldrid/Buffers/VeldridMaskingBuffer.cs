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
        /// <summary>
        /// Number of masking infos per array buffer.
        /// </summary>
        /// <remarks>
        /// The OpenGL spec guarantees a minimum size of 128MB for this type of buffer.
        /// </remarks>
        private const int array_buffer_size = 1024;

        /// <summary>
        /// Number of masking infos per uniform buffer.
        /// </summary>
        /// <remarks>
        /// The OpenGL spec guarantees a minimum size of 16KB for this type of buffer.
        /// </remarks>
        private const int uniform_buffer_size = 128;

        /// <summary>
        /// Number of elements inside each buffer.
        /// </summary>
        private readonly int bufferSize;

        /// <summary>
        /// The size (in bytes) of each element of the buffer.
        /// </summary>
        private readonly uint elementSize;

        private readonly VeldridRenderer renderer;

        private readonly List<DeviceBuffer> buffers = new List<DeviceBuffer>();
        private ResourceSet?[] resourceSets = Array.Empty<ResourceSet?>();

        private int currentElementIndex = -1;

        public VeldridMaskingBuffer(VeldridRenderer renderer)
        {
            this.renderer = renderer;
            bufferSize = renderer.UseStructuredBuffers ? array_buffer_size : uniform_buffer_size;
            elementSize = (uint)Marshal.SizeOf(default(ShaderMaskingInfo));
        }

        public int Add(ShaderMaskingInfo maskingInfo)
        {
            if (currentElementIndex == -1)
            {
                // Signal the first use of this buffer.
                renderer.RegisterUniformBufferForReset(this);
            }
            else if ((currentElementIndex + 1) % bufferSize == 0)
            {
                // If this invocation transitions to a new buffer, flush the pipeline.
                renderer.FlushCurrentBatch(FlushBatchSource.SetUniform);
            }

            // Move to a new element.
            ++currentElementIndex;

            int bufferIndex = currentElementIndex / bufferSize;

            if (bufferIndex >= buffers.Count)
            {
                while (bufferIndex >= buffers.Count)
                {
                    buffers.Add(renderer.UseStructuredBuffers
                        ? renderer.Factory.CreateBuffer(new BufferDescription((uint)(elementSize * bufferSize), BufferUsage.StructuredBufferReadOnly | BufferUsage.Dynamic, elementSize))
                        : renderer.Factory.CreateBuffer(new BufferDescription((uint)(elementSize * bufferSize), BufferUsage.UniformBuffer)));
                }

                Array.Resize(ref resourceSets, buffers.Count);
            }

            DeviceBuffer buffer = buffers[bufferIndex];
            int bufferOffset = currentElementIndex % bufferSize;
            uint bufferOffsetInBytes = (uint)(bufferOffset * elementSize);

            // Upload the element.
            renderer.BufferUpdateCommands.UpdateBuffer(buffer, bufferOffsetInBytes, ref maskingInfo);

            return bufferOffset;
        }

        public ResourceSet GetResourceSet(ResourceLayout layout)
        {
            int bufferIndex = currentElementIndex / bufferSize;
            return resourceSets[bufferIndex] ??= renderer.Factory.CreateResourceSet(new ResourceSetDescription(layout, buffers[bufferIndex]));
        }

        public void ResetCounters()
        {
            currentElementIndex = -1;
        }

        public void Dispose()
        {
            foreach (DeviceBuffer buffer in buffers)
                buffer.Dispose();
        }
    }
}
