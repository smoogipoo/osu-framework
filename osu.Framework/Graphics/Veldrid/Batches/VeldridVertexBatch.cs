// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Statistics;

namespace osu.Framework.Graphics.Veldrid.Batches
{
    internal abstract class VeldridVertexBatch<T> : IVertexBatch<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        public List<VeldridVertexBuffer<T>> VertexBuffers = new List<VeldridVertexBuffer<T>>();

        /// <summary>
        /// The number of vertices in each VertexBuffer.
        /// </summary>
        public int Size { get; }

        private int changeBeginIndex = -1;
        private int changeEndIndex = -1;

        private int currentBufferIndex;
        private int drawStartIndex;
        private int drawCount;

        private readonly VeldridRenderer renderer;

        private VeldridVertexBuffer<T> currentVertexBuffer => VertexBuffers[currentBufferIndex];

        protected VeldridVertexBatch(VeldridRenderer renderer, int bufferSize)
        {
            Size = bufferSize;
            this.renderer = renderer;

            AddAction = Add;
        }

        #region Disposal

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (VeldridVertexBuffer<T> vbo in VertexBuffers)
                    vbo.Dispose();
            }
        }

        #endregion

        void IVertexBatch.ResetCounters()
        {
            changeBeginIndex = -1;
            currentBufferIndex = 0;
            drawStartIndex = 0;
            drawCount = 0;
        }

        protected abstract VeldridVertexBuffer<T> CreateVertexBuffer(VeldridRenderer renderer);

        /// <summary>
        /// Adds a vertex to this <see cref="VeldridVertexBatch{T}"/>.
        /// </summary>
        /// <param name="v">The vertex to add.</param>
        public void Add(T v)
        {
            renderer.SetActiveBatch(this);

            if (currentBufferIndex < VertexBuffers.Count && drawStartIndex + drawCount >= currentVertexBuffer.Size)
            {
                Draw();

                drawStartIndex = 0;
                currentBufferIndex++;

                FrameStatistics.Increment(StatisticsCounterType.VBufOverflow);
            }

            // currentIndex will change after Draw() above, so this cannot be in an else-condition
            while (currentBufferIndex >= VertexBuffers.Count)
                VertexBuffers.Add(CreateVertexBuffer(renderer));

            int drawIndex = drawStartIndex + drawCount;

            if (currentVertexBuffer.SetVertex(drawIndex, v))
            {
                if (changeBeginIndex == -1)
                    changeBeginIndex = drawIndex;

                changeEndIndex = drawIndex + 1;
            }

            ++drawCount;
        }

        /// <summary>
        /// Adds a vertex to this <see cref="VeldridVertexBatch{T}"/>.
        /// This is a cached delegate of <see cref="Add"/> that should be used in memory-critical locations such as <see cref="DrawNode"/>s.
        /// </summary>
        public Action<T> AddAction { get; private set; }

        public int Draw()
        {
            int count = drawCount;
            drawCount = 0;

            if (count == 0)
                return 0;

            VeldridVertexBuffer<T> vertexBuffer = currentVertexBuffer;
            if (changeBeginIndex >= 0)
                vertexBuffer.UpdateRange(changeBeginIndex, changeEndIndex);

            vertexBuffer.DrawRange(drawStartIndex, drawStartIndex + count);

            changeBeginIndex = -1;
            drawStartIndex += count;

            FrameStatistics.Increment(StatisticsCounterType.DrawCalls);
            FrameStatistics.Add(StatisticsCounterType.VerticesDraw, count);

            return count;
        }
    }
}
