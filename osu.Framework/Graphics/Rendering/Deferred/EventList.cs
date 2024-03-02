// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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

        /// <summary>
        /// Prepares this <see cref="EventList"/> for a new frame.
        /// </summary>
        public void NewFrame()
            => events.Clear();

        /// <summary>
        /// Enqueues a render event to the list.
        /// </summary>
        /// <param name="renderEvent">The render event.</param>
        /// <typeparam name="T">The event type.</typeparam>
        public void Enqueue<T>(in T renderEvent)
            where T : unmanaged, IRenderEvent
            => events.Add(createEvent(renderEvent));

        private MemoryReference createEvent<T>(in T renderEvent)
            where T : unmanaged, IRenderEvent
            => writeAligned(allocator.AllocateRegion(Unsafe.SizeOf<T>()), renderEvent);

        /// <summary>
        /// Creates a reader of this <see cref="EventList"/>.
        /// </summary>
        /// <returns>The <see cref="Enumerator"/>.</returns>
        public Enumerator CreateEnumerator()
            => new Enumerator(this);

        /// <summary>
        /// Writes data to the underlying referenced memory region.
        /// </summary>
        /// <param name="reference">The memory reference.</param>
        /// <param name="data">The data to write.</param>
        /// <typeparam name="T">The type of data to write.</typeparam>
        /// <returns>This <see cref="MemoryReference"/>.</returns>
        private MemoryReference writeAligned<T>(MemoryReference reference, in T data)
            where T : unmanaged, IRenderEvent
        {
            unsafe
            {
                Unsafe.Write(Unsafe.AsPointer(ref allocator.GetRegionRef(reference)), data);
            }

            return reference;
        }

        /// <summary>
        /// Reads data from the underlying memory referenced memory region.
        /// </summary>
        /// <param name="reference">The memory reference.</param>
        /// <typeparam name="T">The type of data to read.</typeparam>
        /// <returns>The data.</returns>
        private ref T readAligned<T>(MemoryReference reference)
            where T : unmanaged
        {
            unsafe
            {
                return ref Unsafe.AsRef<T>(Unsafe.AsPointer(ref allocator.GetRegionRef(reference)));
            }
        }

        /// <summary>
        /// Reads an <see cref="EventList"/>. Semantically, this is very similar to <see cref="IEnumerator{T}"/>.
        /// </summary>
        internal ref struct Enumerator
        {
            private readonly EventList list;

            private int eventIndex;
            private MemoryReference eventRef;

            public Enumerator(EventList list)
            {
                this.list = list;
                eventIndex = 0;
            }

            /// <summary>
            /// Advances to the next (or first) event in the list.
            /// </summary>
            /// <returns>Whether an event can be read.</returns>
            public bool Next()
            {
                if (eventIndex < list.events.Count)
                {
                    eventRef = list.events[eventIndex];
                    eventIndex++;
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Reads the current event type.
            /// </summary>
            /// <remarks>
            /// Not valid for use if <see cref="Next"/> returns <c>false</c>.
            /// </remarks>
            public readonly ref RenderEventType CurrentType()
                => ref list.readAligned<RenderEventType>(eventRef);

            /// <summary>
            /// Reads the current event.
            /// </summary>
            /// <typeparam name="T">The expected event type.</typeparam>
            /// <remarks>
            /// Not valid for use if <see cref="Next"/> returns <c>false</c>.
            /// </remarks>
            public readonly ref T Current<T>()
                where T : unmanaged, IRenderEvent
                => ref list.readAligned<T>(eventRef);

            /// <summary>
            /// Replaces the current event with a new one.
            /// </summary>
            /// <param name="newEvent">The new render event.</param>
            /// <typeparam name="T">The new event type.</typeparam>
            public void Replace<T>(in T newEvent)
                where T : unmanaged, IRenderEvent
            {
                if (Unsafe.SizeOf<T>() <= eventRef.Length)
                {
                    // Fast path where we can maintain contiguous data reads.
                    list.writeAligned(eventRef, newEvent);
                }
                else
                {
                    // Slow path.
                    eventRef = list.events[eventIndex] = list.createEvent(newEvent);
                }
            }
        }
    }
}
