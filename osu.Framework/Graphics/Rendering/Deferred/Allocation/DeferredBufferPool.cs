// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Veldrid;
using osu.Framework.Statistics;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred.Allocation
{
    internal class DeferredBufferPool : VeldridStagingResourcePool<PooledBuffer>
    {
        private readonly uint bufferSize;
        private readonly BufferUsage usage;

        public DeferredBufferPool(DeferredContext context, uint bufferSize, BufferUsage usage, string name)
            : base(context.Renderer.VeldridDevice, name)
        {
            this.bufferSize = bufferSize;
            this.usage = usage;
        }

        public PooledBuffer Get(DeferredContext context)
        {
            if (TryGet(_ => true, out PooledBuffer? existing))
                return existing;

            existing = new PooledBuffer(context, bufferSize, usage);
            AddNewResource(existing);
            return existing;
        }
    }

    internal class PooledBuffer : IDisposable
    {
        public readonly DeviceBuffer Buffer;
        private readonly GlobalStatistic<long> statistic;

        public PooledBuffer(DeferredContext context, uint bufferSize, BufferUsage usage)
        {
            Buffer = context.Factory.CreateBuffer(new BufferDescription(bufferSize, usage | BufferUsage.Dynamic));
            statistic = GlobalStatistics.Get<long>("Native", $"PooledBuffer - {usage}");

            statistic.Value += Buffer.SizeInBytes;
        }

        public void Dispose()
        {
            Buffer.Dispose();
            statistic.Value -= Buffer.SizeInBytes;
        }
    }
}
