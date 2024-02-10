// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Statistics;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal interface IDeferredUniformBuffer : IVeldridUniformBuffer
    {
        UniformBufferReference Write(in MemoryReference memory);
        void Activate(UniformBufferChunk chunk);
    }

    internal class DeferredUniformBuffer<TData> : IUniformBuffer<TData>, IDeferredUniformBuffer
        where TData : unmanaged, IEquatable<TData>
    {
        private readonly DeferredRenderer renderer;
        private readonly Dictionary<ChunkReference, ResourceSet> bufferChunks = new Dictionary<ChunkReference, ResourceSet>();

        private TData data;
        private ChunkReference currentChunk;

        public DeferredUniformBuffer(DeferredRenderer renderer)
        {
            this.renderer = renderer;
        }

        TData IUniformBuffer<TData>.Data
        {
            get => data;
            set
            {
                data = value;

                renderer.EnqueueEvent(SetUniformBufferDataEvent.Create(renderer, this, value));
                renderer.RegisterUniformBufferForReset(this);

                FrameStatistics.Increment(StatisticsCounterType.UniformUpl);
            }
        }

        public UniformBufferReference Write(in MemoryReference memory)
            => renderer.Context.UniformBufferManager.Write(memory);

        public void Activate(UniformBufferChunk chunk) => currentChunk = new ChunkReference(renderer, chunk);

        ResourceSet IVeldridUniformBuffer.GetResourceSet(ResourceLayout layout)
        {
            if (bufferChunks.TryGetValue(currentChunk, out ResourceSet? existing))
                return existing;

            return bufferChunks[currentChunk] = renderer.Factory.CreateResourceSet(
                new ResourceSetDescription(
                    layout,
                    new DeviceBufferRange(
                        currentChunk.Buffer,
                        currentChunk.Offset,
                        currentChunk.Size)));
        }

        void IVeldridUniformBuffer.ResetCounters()
        {
            foreach ((_, ResourceSet set) in bufferChunks)
                set.Dispose();

            bufferChunks.Clear();
            data = default;
            currentChunk = default;
        }

        public void Dispose()
        {
        }

        private readonly record struct ChunkReference
        {
            public readonly DeviceBuffer Buffer;
            public readonly uint Size;
            public readonly uint Offset;

            public ChunkReference(DeferredRenderer renderer, UniformBufferChunk chunk)
            {
                Buffer = renderer.Dereference<DeviceBuffer>(chunk.Buffer);
                Size = (uint)chunk.Size;
                Offset = (uint)chunk.Offset;
            }
        }
    }
}
