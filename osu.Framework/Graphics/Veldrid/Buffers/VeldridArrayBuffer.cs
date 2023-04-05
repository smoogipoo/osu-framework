// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    internal class VeldridArrayBuffer<TData> : IArrayBuffer<TData>, IVeldridUniformBuffer
        where TData : unmanaged, IEquatable<TData>
    {
        public readonly int Length;

        private readonly VeldridRenderer renderer;
        private readonly uint structureSize;

        private readonly DeviceBuffer? buffer;
        private readonly TData[]? bufferData;
        private readonly VeldridUniformBuffer<TData>? uniformBuffer;

        private ResourceSet? set;

        public VeldridArrayBuffer(VeldridRenderer renderer, int length)
        {
            this.renderer = renderer;

            Length = length;
            structureSize = (uint)Marshal.SizeOf(default(TData));

            if (renderer.Device.Features.StructuredBuffer)
            {
                bufferData = new TData[length];
                buffer = renderer.Factory.CreateBuffer(new BufferDescription(
                    (uint)(structureSize * length),
                    BufferUsage.StructuredBufferReadOnly | BufferUsage.Dynamic,
                    structureSize));
            }
            else
            {
                uniformBuffer = new VeldridUniformBuffer<TData>(renderer);
            }
        }

        public TData this[int index]
        {
            get
            {
                if (renderer.Device.Features.StructuredBuffer)
                    return bufferData![index];

                return uniformBuffer!.Data;
            }
            set
            {
                if (renderer.Device.Features.StructuredBuffer)
                {
                    if (bufferData![index].Equals(value))
                        return;

                    if (!renderer.Device.Features.StructuredBuffer)
                        renderer.FlushCurrentBatch(FlushBatchSource.SetUniform);

                    bufferData[index] = value;
                    renderer.BufferUpdateCommands.UpdateBuffer(buffer, (uint)(index * structureSize), ref bufferData[index]);
                }
                else
                    uniformBuffer!.Data = value;
            }
        }

        public ResourceSet GetResourceSet(ResourceLayout layout)
        {
            if (renderer.Device.Features.StructuredBuffer)
                return set ??= renderer.Factory.CreateResourceSet(new ResourceSetDescription(layout, buffer));

            return uniformBuffer!.GetResourceSet(layout);
        }

        public void ResetCounters()
        {
            uniformBuffer?.ResetCounters();
        }

        public void Dispose()
        {
            buffer?.Dispose();
            uniformBuffer?.Dispose();
            set?.Dispose();
        }
    }
}
