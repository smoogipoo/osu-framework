// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering.Deferred.Events;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    public class EventList
    {
        private readonly List<byte> renderEvents = new List<byte>();

        public void Reset()
        {
            renderEvents.Clear();
        }

        public void Enqueue<T>(T renderEvent)
            where T : unmanaged, IRenderEvent
        {
            ReadOnlySpan<byte> eventBytes = MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateReadOnlySpan(ref renderEvent, 1));

            renderEvents.Add((byte)renderEvent.Type);
            renderEvents.AddRange(eventBytes);
        }

        public EventListReader CreateReader() => new EventListReader(renderEvents);
    }
}
