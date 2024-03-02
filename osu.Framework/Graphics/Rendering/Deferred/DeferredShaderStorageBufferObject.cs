// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Veldrid.Buffers;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal class DeferredShaderStorageBufferObject<TData> : IShaderStorageBufferObject<TData>, IDeferredShaderStorageBufferObject, IVeldridUniformBuffer
        where TData : unmanaged, IEquatable<TData>
    {
        public int Size { get; }

        private readonly TData[] data;
        private readonly DeviceBuffer buffer;
        private readonly DeferredRenderer renderer;

        private ResourceSet? resourceSet;
        private bool isDirty;

        public DeferredShaderStorageBufferObject(DeferredRenderer renderer, int ssboSize)
        {
            this.renderer = renderer;

            Size = ssboSize;

            data = new TData[Size];
            buffer = renderer.Factory.CreateBuffer(new BufferDescription(
                (uint)(Unsafe.SizeOf<TData>() * Size),
                BufferUsage.StructuredBufferReadOnly | BufferUsage.Dynamic,
                (uint)Unsafe.SizeOf<TData>(),
                true));
        }

        public TData this[int index]
        {
            get => data[index];
            set
            {
                if (value.Equals(data[index]))
                    return;

                data[index] = value;

                if (!isDirty)
                    renderer.Context.EnqueueEvent(FlushShaderStorageBufferObjectEvent.Create(renderer, this));

                isDirty = true;
            }
        }

        public void Flush()
        {
            if (!isDirty)
                return;

            MappedResource mappedBuffer = renderer.Device.Map(buffer, MapMode.Write);

            unsafe
            {
                MemoryMarshal.Cast<TData, byte>(data).CopyTo(new Span<byte>(mappedBuffer.Data.ToPointer(), (int)mappedBuffer.SizeInBytes));
            }

            renderer.Device.Unmap(buffer);
            isDirty = false;
        }

        public ResourceSet GetResourceSet(ResourceLayout layout)
            => resourceSet ??= renderer.Factory.CreateResourceSet(new ResourceSetDescription(layout, buffer));

        public void ResetCounters()
        {
        }

        public void Dispose()
            => buffer.Dispose();
    }
}
