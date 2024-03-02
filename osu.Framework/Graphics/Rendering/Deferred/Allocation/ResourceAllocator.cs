// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using osu.Framework.Development;

namespace osu.Framework.Graphics.Rendering.Deferred.Allocation
{
    /// <summary>
    /// Handles allocation of objects in a deferred rendering context.
    /// </summary>
    internal class ResourceAllocator
    {
        private const int min_buffer_size = 1024 * 1024; // 1MB per buffer.
        private static readonly ArrayPool<byte> memory_pool = ArrayPool<byte>.Shared;

        private readonly List<object> resources = new List<object>();
        private readonly List<byte[]> memoryBuffers = new List<byte[]>();

        private int currentBufferLength;
        private int currentBufferRemaining;

        /// <summary>
        /// Prepares this <see cref="ResourceAllocator"/> for a new frame.
        /// </summary>
        public void NewFrame()
        {
            ThreadSafety.EnsureDrawThread();

            for (int i = 0; i < memoryBuffers.Count; i++)
                memory_pool.Return(memoryBuffers[i]);

            resources.Clear();
            memoryBuffers.Clear();
            currentBufferLength = 0;
            currentBufferRemaining = 0;

            // Special value used by NullReference().
            resources.Add(null!);
        }

        /// <summary>
        /// References an objet.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <typeparam name="T">The object type.</typeparam>
        /// <returns>A reference to the object. May be dereferenced via <see cref="Dereference{T}"/>.</returns>
        public ResourceReference Reference<T>(T obj)
            where T : class?
        {
            ThreadSafety.EnsureDrawThread();

            if (obj == null)
                return new ResourceReference(0);

            resources.Add(obj);
            return new ResourceReference(resources.Count - 1);
        }

        /// <summary>
        /// Dereferences an object.
        /// </summary>
        /// <param name="reference">The reference.</param>
        /// <typeparam name="T">The object type.</typeparam>
        /// <returns>The object.</returns>
        public T Dereference<T>(ResourceReference reference)
            where T : class?
        {
            ThreadSafety.EnsureDrawThread();

            return (T)resources[reference.Id];
        }

        /// <summary>
        /// Allocates a region of memory containing an object.
        /// </summary>
        /// <param name="data">The object.</param>
        /// <typeparam name="T">The object type.</typeparam>
        /// <returns>A reference to the memory region containing the object.</returns>
        public MemoryReference AllocateObject<T>(T data)
            where T : unmanaged
        {
            ThreadSafety.EnsureDrawThread();

            MemoryReference reference = AllocateRegion(Unsafe.SizeOf<T>());
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(GetRegion(reference)), data);
            return reference;
        }

        /// <summary>
        /// Allocates a region of memory containing some data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <typeparam name="T">The data type.</typeparam>
        /// <returns>A reference to the memory region containing the data.</returns>
        public MemoryReference AllocateRegion<T>(ReadOnlySpan<T> data)
            where T : unmanaged
        {
            ThreadSafety.EnsureDrawThread();

            ReadOnlySpan<byte> byteData = MemoryMarshal.Cast<T, byte>(data);
            MemoryReference region = AllocateRegion(byteData.Length);
            byteData.CopyTo(GetRegion(region));

            return region;
        }

        /// <summary>
        /// Allocates an empty memory region of the specified length.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns>A reference to the memory region.</returns>
        public MemoryReference AllocateRegion(int length)
        {
            ThreadSafety.EnsureDrawThread();

            if (currentBufferRemaining < length)
            {
                byte[] buffer = memory_pool.Rent(Math.Max(min_buffer_size * (1 << memoryBuffers.Count), length));
                memoryBuffers.Add(buffer);
                currentBufferLength = buffer.Length;
                currentBufferRemaining = buffer.Length;
            }

            int start = currentBufferLength - currentBufferRemaining;
            currentBufferRemaining -= length;
            return new MemoryReference(memoryBuffers.Count - 1, start, length);
        }

        /// <summary>
        /// Retrieves a <see cref="Span{T}"/> over the underlying referenced memory region.
        /// </summary>
        /// <param name="reference">The memory reference.</param>
        /// <returns>The <see cref="Span{T}"/>.</returns>
        public Span<byte> GetRegion(MemoryReference reference)
        {
            ThreadSafety.EnsureDrawThread();
            return memoryBuffers[reference.BufferId].AsSpan().Slice(reference.Offset, reference.Length);
        }

        /// <summary>
        /// Retrieves a <c>byte</c> reference to the underlying referenced memory region.
        /// </summary>
        /// <param name="reference">The memory reference.</param>
        /// <returns>A reference to the start of the memory region.</returns>
        public ref byte GetRegionRef(MemoryReference reference)
        {
            ThreadSafety.EnsureDrawThread();
            return ref memoryBuffers[reference.BufferId][reference.Offset];
        }
    }
}
