// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using BenchmarkDotNet.Attributes;
using osu.Framework.Allocation;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkTripleBuffer
    {
        private readonly TripleBuffer<object> tripleBuffer = new TripleBuffer<object>();
        private readonly ConcurrentExecutor concurrentExecutor;

        public BenchmarkTripleBuffer()
        {
            concurrentExecutor = new ConcurrentExecutor(tripleBuffer);
        }

        [Benchmark]
        public void Write()
        {
            using var write = tripleBuffer.GetForWrite();
        }

        [Benchmark]
        public void ReadAndWrite()
        {
            using var write = tripleBuffer.GetForWrite();
            using var read = tripleBuffer.GetForRead();
        }

        [Benchmark]
        public void ReadOnly()
        {
            using var read = tripleBuffer.GetForRead();
        }

        [Benchmark]
        public void ConcurrentAccess()
        {
            concurrentExecutor.Run(1000);
        }

        private class ConcurrentExecutor
        {
            private readonly TripleBuffer<object> tripleBuffer;
            private ulong countReads;

            public ConcurrentExecutor(TripleBuffer<object> tripleBuffer)
            {
                this.tripleBuffer = tripleBuffer;
            }

            public void Run(ulong count)
            {
                Interlocked.Exchange(ref countReads, 0);

                using CancellationTokenSource writeCts = new CancellationTokenSource();
                using CancellationTokenSource readCts = new CancellationTokenSource();

                CancellationToken writeToken = writeCts.Token;
                CancellationToken readToken = readCts.Token;

                Thread writeThread = new Thread(() =>
                {
                    while (!writeToken.IsCancellationRequested)
                    {
                        using var _ = tripleBuffer.GetForWrite();
                    }
                });

                Thread readThread = new Thread(() =>
                {
                    while (!readToken.IsCancellationRequested)
                    {
                        using var _ = tripleBuffer.GetForRead();
                        Interlocked.Increment(ref countReads);
                    }
                });

                writeThread.Start();
                readThread.Start();

                while (Interlocked.Read(ref countReads) < count)
                {
                }

                readCts.Cancel();
                readThread.Join();

                writeCts.Cancel();
                writeThread.Join();
            }
        }
    }
}
