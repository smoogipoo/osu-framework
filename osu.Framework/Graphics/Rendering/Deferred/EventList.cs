// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
            int size = Unsafe.SizeOf<T>() + 1;
            byte[] bytes = ArrayPool<byte>.Shared.Rent(size);

            bytes[0] = (byte)renderEvent.Type;
            MemoryMarshal.Write(bytes.AsSpan()[1..], ref renderEvent);

            renderEvents.AddRange(new ArraySegment<byte>(bytes, 0, size));

            ArrayPool<byte>.Shared.Return(bytes);
        }

        public EventListReader CreateReader() => new EventListReader(renderEvents);
    }
}
