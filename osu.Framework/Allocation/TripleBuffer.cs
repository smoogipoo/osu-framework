// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Handles triple-buffering of any object type.
    /// Thread safety assumes at most one writer and one reader.
    /// Comes with the added assurance that the most recent <see cref="GetForRead"/> object is not written to.
    /// </summary>
    internal class TripleBuffer<T>
        where T : class
    {
        private const int buffer_count = 3;
        private const int read_timeout_milliseconds = 100;

        private BufferArray buffers;
        private int writeIndex;
        private int flipIndex = 1;
        private int readIndex = 2;

        public TripleBuffer()
        {
            buffers[writeIndex] = new Buffer(writeIndex);
            buffers[flipIndex] = new Buffer(flipIndex);
            buffers[readIndex] = new Buffer(readIndex);
        }

        public WriteUsage GetForWrite()
        {
            ref Buffer buffer = ref buffers[writeIndex];
            buffer.LastUsage = UsageType.Write;
            return new WriteUsage(this, ref buffer);
        }

        public ReadUsage GetForRead()
        {
            const int estimate_nanoseconds_per_cycle = 5;
            const int estimate_cycles_to_timeout = read_timeout_milliseconds * 1000 * 1000 / estimate_nanoseconds_per_cycle;

            // This should really never happen, but prevents a potential infinite loop if the usage can never be retrieved.
            for (int i = 0; i < estimate_cycles_to_timeout; i++)
            {
                flip(ref readIndex);

                ref Buffer buffer = ref buffers[readIndex];
                if (buffer.LastUsage == UsageType.Read)
                    continue;

                buffer.LastUsage = UsageType.Read;
                return new ReadUsage(ref buffer);
            }

            return default;
        }

        private void flip(ref int localIndex)
        {
            localIndex = Interlocked.Exchange(ref flipIndex, localIndex);
        }

        public readonly ref struct WriteUsage
        {
            private readonly TripleBuffer<T> tripleBuffer;
            private readonly ref Buffer buffer;

            public WriteUsage(TripleBuffer<T> tripleBuffer, ref Buffer buffer)
            {
                this.tripleBuffer = tripleBuffer;
                this.buffer = ref buffer;
            }

            public T? Object
            {
                get => buffer.Object;
                set => buffer.Object = value;
            }

            public int Index => buffer.Index;

            public void Dispose() => tripleBuffer.flip(ref tripleBuffer.writeIndex);
        }

        public readonly ref struct ReadUsage
        {
            private readonly ref Buffer buffer;

            public ReadUsage(ref Buffer buffer)
            {
                this.buffer = ref buffer;
            }

            [MemberNotNullWhen(true, nameof(Object))]
            public bool IsValid => !Unsafe.IsNullRef(ref buffer);

            public T? Object => buffer.Object;

            public int Index => buffer.Index;

            public void Dispose()
            {
            }
        }

        public record struct Buffer(int Index)
        {
            public T? Object;
            public volatile UsageType LastUsage;
        }

        public enum UsageType
        {
            Read,
            Write
        }

        [InlineArray(buffer_count)]
        private struct BufferArray
        {
            private Buffer buffer;
        }
    }
}
