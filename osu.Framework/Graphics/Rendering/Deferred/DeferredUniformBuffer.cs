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
    internal interface IDeferredUniformBuffer
    {
        void Write(in MemoryReference memory);
        void MoveNext();
    }

    internal class DeferredUniformBuffer<TData> : IUniformBuffer<TData>, IDeferredUniformBuffer, IVeldridUniformBuffer
        where TData : unmanaged, IEquatable<TData>
    {
        private readonly DeferredRenderer renderer;

        private readonly List<UniformBufferReference> dataOffsets = new List<UniformBufferReference>();
        private int currentOffsetIndex = -1;
        private TData data;

        private readonly Dictionary<UniformBufferChunk, ResourceSet> resourceSets = new Dictionary<UniformBufferChunk, ResourceSet>();

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

        public void Write(in MemoryReference memory)
            => dataOffsets.Add(renderer.Context.UniformBufferManager.Write(memory));

        public void MoveNext()
            => currentOffsetIndex++;

        ResourceSet IVeldridUniformBuffer.GetResourceSet(ResourceLayout layout)
        {
            UniformBufferChunk chunk = dataOffsets[currentOffsetIndex].Chunk;

            if (resourceSets.TryGetValue(chunk, out ResourceSet? existing))
                return existing;

            return resourceSets[chunk] = renderer.Factory.CreateResourceSet(
                new ResourceSetDescription(
                    layout,
                    new DeviceBufferRange(
                        chunk.Buffer,
                        (uint)chunk.Offset,
                        (uint)chunk.Size)));
        }

        uint IVeldridUniformBuffer.GetOffset() => (uint)dataOffsets[currentOffsetIndex].OffsetInChunk;

        void IVeldridUniformBuffer.ResetCounters()
        {
            foreach ((_, ResourceSet set) in resourceSets)
                set.Dispose();

            resourceSets.Clear();
            dataOffsets.Clear();

            currentOffsetIndex = -1;
            data = default;
        }

        public void Dispose()
        {
        }
    }
}
