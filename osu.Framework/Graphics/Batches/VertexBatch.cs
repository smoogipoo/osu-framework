// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Statistics;

namespace osu.Framework.Graphics.Batches
{
    public abstract class VertexBatch<T> : IVertexBatch<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        public List<IVertexBuffer<T>> VertexBuffers = new List<IVertexBuffer<T>>();

        /// <summary>
        /// The number of vertices in each VertexBuffer.
        /// </summary>
        public int Size { get; }

        private int changeBeginIndex = -1;
        private int changeEndIndex = -1;

        private int currentBufferIndex;
        private int currentVertexIndex;

        private readonly IRenderer renderer;
        private readonly int maxBuffers;

        private IVertexBuffer<T> currentVertexBuffer => VertexBuffers[currentBufferIndex];

        protected VertexBatch(IRenderer renderer, int bufferSize, int maxBuffers)
        {
            // Vertex buffers of size 0 don't make any sense. Let's not blindly hope for good behavior of OpenGL.
            Trace.Assert(bufferSize > 0);

            Size = bufferSize;
            this.renderer = renderer;
            this.maxBuffers = maxBuffers;

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
                foreach (IVertexBuffer<T> vbo in VertexBuffers)
                    vbo.Dispose();
            }
        }

        #endregion

        void IVertexBatch.ResetCounters()
        {
            changeBeginIndex = -1;
            currentBufferIndex = 0;
            currentVertexIndex = 0;
        }

        protected abstract IVertexBuffer<T> CreateVertexBuffer();

        /// <summary>
        /// Adds a vertex to this <see cref="VertexBatch{T}"/>.
        /// </summary>
        /// <param name="vertex">The vertex to add.</param>
        public void Add(T vertex)
        {
            renderer.SetActiveBatch(this);

            if (currentBufferIndex < VertexBuffers.Count && currentVertexIndex >= currentVertexBuffer.Size)
            {
                Draw();
                FrameStatistics.Increment(StatisticsCounterType.VBufOverflow);
            }

            // currentIndex will change after Draw() above, so this cannot be in an else-condition
            while (currentBufferIndex >= VertexBuffers.Count)
                VertexBuffers.Add(CreateVertexBuffer());

            if (currentVertexBuffer.SetVertex(currentVertexIndex, vertex))
            {
                if (changeBeginIndex == -1)
                    changeBeginIndex = currentVertexIndex;

                changeEndIndex = currentVertexIndex + 1;
            }

            ++currentVertexIndex;
        }

        public Action<T> AddAction { get; }

        public int Draw()
        {
            if (currentVertexIndex == 0)
                return 0;

            IVertexBuffer<T> vertexBuffer = currentVertexBuffer;
            if (changeBeginIndex >= 0)
                vertexBuffer.UpdateRange(changeBeginIndex, changeEndIndex);

            vertexBuffer.DrawRange(0, currentVertexIndex);

            int count = currentVertexIndex;

            // When using multiple buffers we advance to the next one with every draw to prevent contention on the same buffer with future vertex updates.
            //TODO: let us know if we exceed and roll over to zero here.
            currentBufferIndex = (currentBufferIndex + 1) % maxBuffers;
            currentVertexIndex = 0;
            changeBeginIndex = -1;

            FrameStatistics.Increment(StatisticsCounterType.DrawCalls);
            FrameStatistics.Add(StatisticsCounterType.VerticesDraw, count);

            return count;
        }
    }
}
