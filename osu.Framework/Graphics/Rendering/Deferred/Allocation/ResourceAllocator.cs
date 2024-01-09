// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using osu.Framework.Development;

namespace osu.Framework.Graphics.Rendering.Deferred.Allocation
{
    public class ResourceAllocator
    {
        private const int min_buffer_size = 1024 * 1024; // 1MB

        private readonly Dictionary<object, RendererResource> resourceReferences = new Dictionary<object, RendererResource>();
        private readonly List<object> resources = new List<object>();

        private readonly List<MemoryBuffer> memoryBuffers = new List<MemoryBuffer>();

        public void Reset()
        {
            ThreadSafety.EnsureDrawThread();

            for (int i = 0; i < memoryBuffers.Count; i++)
                memoryBuffers[i].Dispose();

            resourceReferences.Clear();
            resources.Clear();
            memoryBuffers.Clear();
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

        public RendererMemoryBlock Allocate<T>(T data)
            where T : unmanaged
        {
            ThreadSafety.EnsureDrawThread();

            int requiredSize = Marshal.SizeOf(data);

            if (memoryBuffers.Count == 0 || memoryBuffers[^1].Remaining < requiredSize)
                memoryBuffers.Add(new MemoryBuffer(memoryBuffers.Count, Math.Max(min_buffer_size, requiredSize)));

            RendererMemoryBlock block = memoryBuffers[^1].Reserve(requiredSize);
            Span<byte> buffer = GetBuffer(block);

            MemoryMarshal.Write(buffer, data);

            return block;
        }

        public object GetResource(RendererResource resource)
        {
            ThreadSafety.EnsureDrawThread();
            return resources[resource.Id];
        }

        public Span<byte> GetBuffer(RendererMemoryBlock block)
        {
            ThreadSafety.EnsureDrawThread();
            return memoryBuffers[block.BufferId].GetBuffer(block);
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
                Remaining = buffer.Length;
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
    }
}
