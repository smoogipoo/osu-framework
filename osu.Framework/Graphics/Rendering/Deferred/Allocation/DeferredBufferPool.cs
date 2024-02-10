// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Veldrid;
using osu.Framework.Graphics.Veldrid.Pipelines;
using osu.Framework.Statistics;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred.Allocation
{
    internal class DeferredBufferPool : VeldridStagingResourcePool<PooledBuffer>
    {
        private readonly SimplePipeline pipeline;
        private readonly uint bufferSize;
        private readonly BufferUsage usage;

        public DeferredBufferPool(SimplePipeline pipeline, uint bufferSize, BufferUsage usage, string name)
            : base(pipeline, name)
        {
            this.pipeline = pipeline;
            this.bufferSize = bufferSize;
            this.usage = usage;
        }

        public PooledBuffer Get()
        {
            if (TryGet(_ => true, out PooledBuffer? existing))
                return existing;

            existing = new PooledBuffer(pipeline, bufferSize, usage);
            AddNewResource(existing);
            return existing;
        }
    }

    internal class PooledBuffer : IDisposable
    {
        public readonly DeviceBuffer Buffer;
        private readonly GlobalStatistic<long> statistic;

        public PooledBuffer(SimplePipeline pipeline, uint bufferSize, BufferUsage usage)
        {
            Buffer = pipeline.Factory.CreateBuffer(new BufferDescription(bufferSize, usage | BufferUsage.Dynamic));
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
