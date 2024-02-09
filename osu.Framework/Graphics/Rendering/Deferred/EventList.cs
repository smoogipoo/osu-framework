// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal class EventList
    {
        private readonly ResourceAllocator allocator;
        private readonly List<MemoryReference> events = new List<MemoryReference>();

        public EventList(ResourceAllocator allocator)
        {
            this.allocator = allocator;
        }

        public void Reset()
        {
            events.Clear();
        }

        public void Enqueue<T>(in T renderEvent)
            where T : unmanaged, IRenderEvent
        {
            int requiredSize = Unsafe.SizeOf<T>() + 1;

            MemoryReference reference = allocator.AllocateRegion(requiredSize);
            Span<byte> buffer = allocator.GetRegion(reference);

            buffer[0] = (byte)renderEvent.Type;
            Unsafe.WriteUnaligned(ref buffer[1], renderEvent);

            events.Add(reference);
        }

        public EventListReader CreateReader() => new EventListReader(allocator, events);
    }
}
