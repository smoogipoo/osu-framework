// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics.Rendering.Deferred;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkEventList
    {
        // Used for benchmark-local testing.
        private ResourceAllocator localAllocator = null!;
        private EventList localEventList = null!;

        // Used for benchmark-static testing.
        // 0: Basic events
        // 1: Events with data
        // 2: Mixed events
        private readonly (ResourceAllocator allocator, EventList list)[] staticItems = new (ResourceAllocator allocator, EventList list)[3];

        [GlobalSetup]
        public void GlobalSetup()
        {
            localAllocator = new ResourceAllocator();
            localEventList = new EventList(localAllocator);

            for (int i = 0; i < staticItems.Length; i++)
            {
                ResourceAllocator allocator = new ResourceAllocator();
                staticItems[i] = (allocator, new EventList(allocator));
            }

            for (int i = 0; i < 10000; i++)
            {
                staticItems[0].list.Enqueue(new FlushEvent(RenderEventType.Flush, new ResourceReference(1), 10));
                staticItems[1].list.Enqueue(new AddPrimitiveToBatchEvent(RenderEventType.AddPrimitiveToBatch, new ResourceReference(0), staticItems[1].allocator.AllocateRegion(1024)));

                if (i % 2 == 0)
                    staticItems[2].list.Enqueue(new FlushEvent(RenderEventType.Flush, new ResourceReference(1), 10));
                else
                    staticItems[2].list.Enqueue(new AddPrimitiveToBatchEvent(RenderEventType.AddPrimitiveToBatch, new ResourceReference(0), staticItems[2].allocator.AllocateRegion(1024)));
            }
        }

        [Benchmark]
        public void Write()
        {
            localEventList.NewFrame();
            localAllocator.NewFrame();

            for (int i = 0; i < 10000; i++)
                localEventList.Enqueue(new FlushEvent());
        }

        [Benchmark]
        public void WriteWithData()
        {
            localEventList.NewFrame();
            localAllocator.NewFrame();

            for (int i = 0; i < 10000; i++)
                localEventList.Enqueue(new AddPrimitiveToBatchEvent(RenderEventType.AddPrimitiveToBatch, new ResourceReference(0), localAllocator.AllocateRegion(1024)));
        }

        [Benchmark]
        public int Read()
        {
            var enumerator = staticItems[0].list.CreateEnumerator();

            int totalVertices = 0;

            while (enumerator.Next())
            {
                switch (enumerator.CurrentType())
                {
                    case RenderEventType.Flush:
                        ref FlushEvent e = ref enumerator.Current<FlushEvent>();
                        totalVertices += e.VertexCount;
                        break;
                }
            }

            return totalVertices;
        }

        [Benchmark]
        public int ReadWithData()
        {
            var enumerator = staticItems[1].list.CreateEnumerator();

            int data = 0;

            while (enumerator.Next())
            {
                switch (enumerator.CurrentType())
                {
                    case RenderEventType.AddPrimitiveToBatch:
                        ref AddPrimitiveToBatchEvent e = ref enumerator.Current<AddPrimitiveToBatchEvent>();
                        foreach (byte b in staticItems[1].allocator.GetRegion(e.Memory))
                            data += b;
                        break;
                }
            }

            return data;
        }

        [Benchmark]
        public int ReadMixed()
        {
            var enumerator = staticItems[2].list.CreateEnumerator();

            int data = 0;

            while (enumerator.Next())
            {
                switch (enumerator.CurrentType())
                {
                    case RenderEventType.Flush:
                    {
                        ref FlushEvent e = ref enumerator.Current<FlushEvent>();
                        data += e.VertexCount;
                        break;
                    }

                    case RenderEventType.AddPrimitiveToBatch:
                    {
                        ref AddPrimitiveToBatchEvent e = ref enumerator.Current<AddPrimitiveToBatchEvent>();
                        foreach (byte b in staticItems[2].allocator.GetRegion(e.Memory))
                            data += b;
                        break;
                    }
                }
            }

            return data;
        }

        [Benchmark]
        public int ReplaceSame()
        {
            localEventList.NewFrame();
            localAllocator.NewFrame();

            localEventList.Enqueue(new FlushEvent());
            localEventList.Enqueue(new FlushEvent());
            localEventList.Enqueue(new FlushEvent());

            var enumerator = localEventList.CreateEnumerator();
            enumerator.Next();
            enumerator.Next();
            enumerator.Replace(new FlushEvent());

            int i = 0;
            enumerator = localEventList.CreateEnumerator();

            while (enumerator.Next())
            {
                enumerator.CurrentType();
                i++;
            }

            localAllocator.NewFrame();

            return i;
        }

        [Benchmark]
        public int ReplaceSmaller()
        {
            localEventList.NewFrame();
            localAllocator.NewFrame();

            localEventList.Enqueue(new FlushEvent());
            localEventList.Enqueue(new FlushEvent());
            localEventList.Enqueue(new FlushEvent());

            var enumerator = localEventList.CreateEnumerator();
            enumerator.Next();
            enumerator.Next();
            enumerator.Replace(new SetScissorStateEvent());

            int i = 0;
            enumerator = localEventList.CreateEnumerator();

            while (enumerator.Next())
            {
                enumerator.CurrentType();
                i++;
            }

            localAllocator.NewFrame();

            return i;
        }

        [Benchmark]
        public int ReplaceBigger()
        {
            localEventList.NewFrame();
            localAllocator.NewFrame();

            localEventList.Enqueue(new FlushEvent());
            localEventList.Enqueue(new FlushEvent());
            localEventList.Enqueue(new FlushEvent());

            var enumerator = localEventList.CreateEnumerator();
            enumerator.Next();
            enumerator.Next();
            enumerator.Replace(new SetUniformBufferDataEventOverlay());

            int i = 0;
            enumerator = localEventList.CreateEnumerator();

            while (enumerator.Next())
            {
                enumerator.CurrentType();
                i++;
            }

            localAllocator.NewFrame();

            return i;
        }
    }
}
