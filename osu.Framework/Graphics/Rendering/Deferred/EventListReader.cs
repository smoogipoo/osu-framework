// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal ref struct EventListReader
    {
        private readonly DeferredRenderer renderer;
        private readonly List<MemoryReference> events = new List<MemoryReference>();

        private int eventIndex;
        private Span<byte> eventData = Span<byte>.Empty;

        public EventListReader(DeferredRenderer renderer, List<MemoryReference> events)
        {
            this.renderer = renderer;
            this.events = events;

            eventIndex = -1;
        }

        public bool Next()
        {
            if (eventIndex == events.Count - 1)
            {
                eventData = Span<byte>.Empty;
                return false;
            }

            eventIndex++;
            eventData = events[eventIndex].GetRegion(renderer);
            return true;
        }

        public RenderEventType CurrentType()
            => (RenderEventType)eventData[0];

        public T Current<T>()
            where T : unmanaged, IRenderEvent
            => MemoryMarshal.Read<T>(eventData[1..]);

        public void Reset()
        {
            eventIndex = -1;
            eventData = Span<byte>.Empty;
        }
    }
}
