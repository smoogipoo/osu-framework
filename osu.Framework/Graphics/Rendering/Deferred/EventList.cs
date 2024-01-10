// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering.Deferred.Events;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    public class EventList
    {
        private const int buffer_size = 1024 * 1024; // 1MB

        private readonly List<EventBuffer> buffers = new List<EventBuffer>();

        public void Reset()
        {
            foreach (EventBuffer buf in buffers)
                buf.Dispose();
            buffers.Clear();
        }

        public void Enqueue<T>(in T renderEvent)
            where T : unmanaged, IRenderEvent
        {
            if (buffers.Count == 0 || !buffers[^1].HasSpace<T>())
                buffers.Add(new EventBuffer(Unsafe.SizeOf<T>() + 1));

            buffers[^1].Write(in renderEvent);
        }

        public EventListReader CreateReader() => new EventListReader(buffers);

        public class EventBuffer : IDisposable
        {
            public int DataLength { get; private set; }
            public readonly int Size;

            private readonly byte[] data;

            public EventBuffer(int minSize)
            {
                data = ArrayPool<byte>.Shared.Rent(Math.Max(buffer_size, minSize));
                Size = data.Length;
            }

            public bool HasSpace<T>()
                where T : unmanaged, IRenderEvent
                => DataLength + Unsafe.SizeOf<T>() + 1 <= Size;

            public ReadOnlySpan<byte> GetData() => data.AsSpan()[..DataLength];

            public void Write<T>(in T renderEvent)
                where T : unmanaged, IRenderEvent
            {
                Debug.Assert(HasSpace<T>());

                data[DataLength] = (byte)renderEvent.Type;
                MemoryMarshal.Write(data.AsSpan(DataLength + 1), ref Unsafe.AsRef(in renderEvent));

                DataLength += Unsafe.SizeOf<T>() + 1;
            }

            public void Dispose()
            {
                ArrayPool<byte>.Shared.Return(data);
            }
        }
    }
}
