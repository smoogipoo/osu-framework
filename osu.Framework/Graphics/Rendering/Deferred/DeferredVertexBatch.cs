// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Veldrid.Buffers;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    public interface IDeferredVertexBatch
    {
        void Prepare();
        void Draw(int startIndex, int endIndex);
        void ResetCounters();
    }

    public readonly record struct DeferredVertexBatchLookup(Type VertexType, PrimitiveTopology Topology, IndexLayout IndexLayout);

    public class DeferredVertexBatch<TVertex> : IVertexBatch<TVertex>, IDeferredVertexBatch
        where TVertex : unmanaged, IEquatable<TVertex>, IVertex
    {
        public Action<TVertex> AddAction { get; }

        public readonly int Size;

        private readonly DeferredRenderer renderer;
        private readonly PrimitiveTopology topology;
        private readonly IndexLayout indexLayout;

        private readonly List<TVertex> pendingVertices = new List<TVertex>();
        private readonly List<VeldridMetalVertexBuffer<TVertex>> batches = new List<VeldridMetalVertexBuffer<TVertex>>();

        public DeferredVertexBatch(DeferredRenderer renderer, PrimitiveTopology topology, IndexLayout indexLayout)
        {
            this.renderer = renderer;
            this.topology = topology;
            this.indexLayout = indexLayout;

            Size = indexLayout switch
            {
                IndexLayout.Linear => IRenderer.MAX_VERTICES,
                IndexLayout.Quad => IRenderer.MAX_QUADS * IRenderer.VERTICES_PER_QUAD,
                _ => throw new ArgumentOutOfRangeException(nameof(indexLayout), indexLayout, null)
            };

            AddAction = ((IVertexBatch<TVertex>)this).Add;
        }

        public void Prepare()
        {
            // Todo: This must consider the number of vertices required to form a primitive.
            // Todo: This makes no effort to be space-efficient.

            int numBatchesRequired = pendingVertices.Count / Size + 1;

            while (batches.Count < numBatchesRequired)
                batches.Add(renderer.CreateVertexBuffer<TVertex>(Size));

            ReadOnlySpan<TVertex> vertices = CollectionsMarshal.AsSpan(pendingVertices);
            int batch = 0;

            while (vertices.Length > 0)
            {
                int sizeToUpload = Math.Min(batches[batch].Size, vertices.Length);

                batches[batch].SetBuffer(vertices[..sizeToUpload]);
                vertices = vertices[sizeToUpload..];

                batch++;
            }
        }

        public void Draw(int startIndex, int count)
        {
            while (count > 0)
            {
                int batch = startIndex / Size;
                int indexInBatch = startIndex % Size;
                int countToDraw = Math.Min(count, Size);

                renderer.DrawVertexBuffer(batches[batch], indexLayout, topology, indexInBatch, countToDraw);

                startIndex += countToDraw;
                count -= countToDraw;
            }
        }

        public void ResetCounters()
        {
            pendingVertices.Clear();
        }

        int IVertexBatch.Size => int.MaxValue;

        int IVertexBatch.Draw()
        {
            renderer.EnqueueEvent(new DrawVertexBatchEvent());
            return 0;
        }

        void IVertexBatch<TVertex>.Add(TVertex vertex)
        {
            pendingVertices.Add(vertex);
            renderer.EnqueueEvent(new AddVertexToBatchEvent(renderer.Reference(this), pendingVertices.Count - 1));
        }

        public void Dispose()
        {
        }
    }
}
