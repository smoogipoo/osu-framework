// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using osu.Framework.Development;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred.Allocation
{
    internal class ResourceAllocator
    {
        private readonly DeferredRenderer renderer;
        private const int min_buffer_size = 1024 * 1024; // 1MB

        private readonly Dictionary<object, RendererResource> resourceReferences = new Dictionary<object, RendererResource>();
        private readonly List<object> resources = new List<object>();

        private readonly List<MemoryBuffer> memoryBuffers = new List<MemoryBuffer>();
        private readonly List<StagingMemoryBuffer> stagingMemoryBuffers = new List<StagingMemoryBuffer>();

        public ResourceAllocator(DeferredRenderer renderer)
        {
            this.renderer = renderer;
        }

        public void Reset()
        {
            ThreadSafety.EnsureDrawThread();

            for (int i = 0; i < memoryBuffers.Count; i++)
                memoryBuffers[i].Dispose();

            for (int i = 0; i < stagingMemoryBuffers.Count; i++)
                stagingMemoryBuffers[i].Dispose();

            resourceReferences.Clear();
            resources.Clear();
            memoryBuffers.Clear();
            stagingMemoryBuffers.Clear();
        }

        public RendererResource Reference<T>(T obj)
            where T : class
        {
            ThreadSafety.EnsureDrawThread();

            if (resourceReferences.TryGetValue(obj, out RendererResource existing))
                return existing;

            resources.Add(obj);

            return resourceReferences[obj] = new RendererResource(resources.Count - 1);
        }

        public object GetResource(RendererResource resource)
        {
            ThreadSafety.EnsureDrawThread();
            return resources[resource.Id];
        }

        public RendererMemoryBlock Allocate<T>(T data)
            where T : unmanaged
        {
            RendererMemoryBlock block = AllocateRegion(Marshal.SizeOf(data));
            MemoryMarshal.Write(GetBuffer(block), ref data);
            return block;
        }

        public RendererMemoryBlock AllocateRegion(int length)
        {
            ThreadSafety.EnsureDrawThread();

            if (memoryBuffers.Count == 0 || memoryBuffers[^1].Remaining < length)
                memoryBuffers.Add(new MemoryBuffer(memoryBuffers.Count, Math.Max(min_buffer_size, length)));

            return memoryBuffers[^1].Reserve(length);
        }

        public Span<byte> GetBuffer(RendererMemoryBlock block)
        {
            ThreadSafety.EnsureDrawThread();
            return memoryBuffers[block.BufferId].GetBuffer(block);
        }

        public RendererStagingMemoryBlock AllocateStaging<T>(T data)
            where T : unmanaged
        {
            return AllocateStaging(MemoryMarshal.CreateReadOnlySpan(ref data, 1));
        }

        public RendererStagingMemoryBlock AllocateStaging<T>(ReadOnlySpan<T> data)
            where T : unmanaged
        {
            ThreadSafety.EnsureDrawThread();

            ReadOnlySpan<byte> dataSpan = MemoryMarshal.Cast<T, byte>(data);

            if (stagingMemoryBuffers.Count == 0 || stagingMemoryBuffers[^1].Remaining < dataSpan.Length)
                stagingMemoryBuffers.Add(new StagingMemoryBuffer(renderer, stagingMemoryBuffers.Count, Math.Max(min_buffer_size, dataSpan.Length)));

            return stagingMemoryBuffers[^1].Write(dataSpan);
        }

        public void CopyStagingBuffer(RendererStagingMemoryBlock block, CommandList commandList, DeviceBuffer target, int offsetInTarget)
        {
            ThreadSafety.EnsureDrawThread();
            commandList.CopyBuffer(stagingMemoryBuffers[block.BufferId].GetBuffer(), (uint)block.Index, target, (uint)offsetInTarget, (uint)block.Length);
        }

        private class MemoryBuffer : IDisposable
        {
            public readonly int Id;
            public int Size => buffer.Length;
            public int Remaining { get; private set; }

            private readonly byte[] buffer;

            public MemoryBuffer(int id, int minSize)
            {
                Id = id;
                buffer = ArrayPool<byte>.Shared.Rent(minSize);
                Remaining = Size;
            }

            public RendererMemoryBlock Reserve(int length)
            {
                Debug.Assert(length <= Remaining);

                int start = Size - Remaining;
                Remaining -= length;
                return new RendererMemoryBlock(Id, start, length);
            }

            public Span<byte> GetBuffer(RendererMemoryBlock block)
            {
                Debug.Assert(block.BufferId == Id);
                return buffer.AsSpan().Slice(block.Index, block.Length);
            }

            public void Dispose()
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private class StagingMemoryBuffer : IDisposable
        {
            public readonly int Id;
            public int Size => (int)buffer.SizeInBytes;
            public int Remaining { get; private set; }

            private readonly DeferredRenderer renderer;
            private readonly DeviceBuffer buffer;

            public StagingMemoryBuffer(DeferredRenderer renderer, int id, int minSize)
            {
                this.renderer = renderer;
                Id = id;
                buffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)minSize, BufferUsage.Staging));
                Remaining = Size;
            }

            public RendererStagingMemoryBlock Write(ReadOnlySpan<byte> data)
            {
                Debug.Assert(data.Length <= Remaining);

                int start = Size - Remaining;
                Remaining -= data.Length;

                renderer.Device.UpdateBuffer(buffer, (uint)start, data);

                return new RendererStagingMemoryBlock(Id, start, data.Length);
            }

            public DeviceBuffer GetBuffer() => buffer;

            public void Dispose()
            {
                buffer.Dispose();
            }
        }
    }
}
