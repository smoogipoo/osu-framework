// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal class EventList
    {
        private readonly DeferredRenderer renderer;
        private readonly List<MemoryReference> events = new List<MemoryReference>();

        public EventList(DeferredRenderer renderer)
        {
            this.renderer = renderer;
        }

        public void Reset()
        {
            events.Clear();
        }

        public void Enqueue<T>(in T renderEvent)
            where T : unmanaged, IRenderEvent
        {
            int requiredSize = Unsafe.SizeOf<T>() + 1;

            MemoryReference reference = renderer.AllocateRegion(requiredSize);
            Span<byte> buffer = renderer.GetRegion(reference);

            buffer[0] = (byte)renderEvent.Type;
            MemoryMarshal.Write(buffer[1..], ref Unsafe.AsRef(in renderEvent));

            events.Add(reference);
        }

        public EventListReader CreateReader() => new EventListReader(renderer, events);
    }
}
