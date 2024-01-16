// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Veldrid.Buffers;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal interface IDeferredUniformBuffer
    {
        void Write(RendererStagingMemoryBlock memory, CommandList commandList);

        void MoveNext();
    }

    internal class DeferredUniformBuffer<TData> : IUniformBuffer<TData>, IDeferredUniformBuffer, IVeldridUniformBuffer
        where TData : unmanaged, IEquatable<TData>
    {
        private readonly DeferredRenderer renderer;
        private readonly UniformBufferManager uniformBufferManager;

        private readonly List<int> dataOffsets = new List<int>();
        private int currentOffsetIndex = -1;
        private TData data;

        private readonly Dictionary<DeviceBuffer, ResourceSet> resourceSets = new Dictionary<DeviceBuffer, ResourceSet>();

        public DeferredUniformBuffer(DeferredRenderer renderer, UniformBufferManager uniformBufferManager)
        {
            this.renderer = renderer;
            this.uniformBufferManager = uniformBufferManager;
        }

        TData IUniformBuffer<TData>.Data
        {
            get => data;
            set
            {
                data = value;
                renderer.EnqueueEvent(SetUniformBufferDataEvent.Create(renderer, this, value));
                renderer.RegisterUniformBufferForReset(this);
            }
        }

        public void Write(RendererStagingMemoryBlock memory, CommandList commandList) => dataOffsets.Add(uniformBufferManager.Commit(memory, commandList));

        public void MoveNext() => currentOffsetIndex++;

        ResourceSet IVeldridUniformBuffer.GetResourceSet(ResourceLayout layout)
        {
            DeviceBuffer buffer = uniformBufferManager.GetBuffer(dataOffsets[currentOffsetIndex]);

            if (resourceSets.TryGetValue(buffer, out ResourceSet? existing))
                return existing;

            return resourceSets[buffer] = renderer.Factory.CreateResourceSet(new ResourceSetDescription(layout, buffer));
        }

        uint IVeldridUniformBuffer.GetOffset() => uniformBufferManager.GetOffset(dataOffsets[currentOffsetIndex]);

        void IVeldridUniformBuffer.ResetCounters()
        {
            dataOffsets.Clear();
            currentOffsetIndex = -1;
        }

        public void Dispose()
        {
            // Todo:
        }
    }
}
