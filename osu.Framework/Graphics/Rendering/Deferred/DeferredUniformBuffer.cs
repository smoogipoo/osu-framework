// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering.Deferred.Events;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    public interface IDeferredUniformBuffer
    {
        void SetDataFromBuffer(ReadOnlySpan<byte> buffer);
    }

    public class DeferredUniformBuffer<TData> : IUniformBuffer<TData>, IDeferredUniformBuffer
        where TData : unmanaged, IEquatable<TData>
    {
        public IUniformBuffer<TData> Resource { get; }

        private readonly DeferredRenderer renderer;

        public DeferredUniformBuffer(DeferredRenderer renderer, IUniformBuffer<TData> uniformBuffer)
        {
            this.renderer = renderer;
            Resource = uniformBuffer;
        }

        private TData data;

        public TData Data
        {
            get => data;
            set
            {
                data = value;
                renderer.EnqueueEvent(SetUniformBufferDataEvent.Create(renderer, this, value));
            }
        }

        public void Dispose()
        {
            // Todo:
        }

        public void SetDataFromBuffer(ReadOnlySpan<byte> buffer)
        {
            Resource.Data = MemoryMarshal.Read<TData>(buffer);
        }
    }
}
