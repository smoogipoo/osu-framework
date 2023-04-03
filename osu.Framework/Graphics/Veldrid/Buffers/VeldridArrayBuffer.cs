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
        private readonly DeviceBuffer buffer;
        private readonly TData[] bufferData;
        private readonly uint structureSize;

        private ResourceSet? set;

        public VeldridArrayBuffer(VeldridRenderer renderer, int length)
        {
            this.renderer = renderer;

            Length = length;

            bufferData = new TData[length];
            structureSize = (uint)Marshal.SizeOf(default(TData));

            buffer = renderer.Factory.CreateBuffer(new BufferDescription(
                (uint)(structureSize * length),
                BufferUsage.StructuredBufferReadOnly | BufferUsage.Dynamic,
                structureSize));
        }

        public TData this[int index]
        {
            get => bufferData[index];
            set
            {
                if (bufferData[index].Equals(value))
                    return;

                bufferData[index] = value;
                renderer.BufferUpdateCommands.UpdateBuffer(buffer, (uint)(index * structureSize), ref bufferData[index]);
            }
        }

        public ResourceSet GetResourceSet(ResourceLayout layout) => set ??= renderer.Factory.CreateResourceSet(new ResourceSetDescription(layout, buffer));

        public void ResetCounters()
        {
        }

        public void Dispose()
        {
            buffer.Dispose();
            set?.Dispose();
        }
    }
}
