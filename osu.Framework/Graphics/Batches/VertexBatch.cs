// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Statistics;

namespace osu.Framework.Graphics.Batches
{
    public abstract class VertexBatch<T> : IVertexBatch, IDisposable
        where T : struct, IEquatable<T>, IVertex
    {
        public List<VertexBuffer<T>> VertexBuffers = new List<VertexBuffer<T>>();

        /// <summary>
        /// The number of vertices in each VertexBuffer.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Adds a vertex to this <see cref="VertexBatch{T}"/>.
        /// This is a cached delegate of <see cref="Add"/> that should be used in memory-critical locations such as <see cref="DrawNode"/>s.
        /// </summary>
        public readonly Action<T> AddAction;

        private int currentBufferIndex;

        private VertexBuffer<T> currentVertexBuffer => VertexBuffers[currentBufferIndex];

        protected VertexBatch(int bufferSize, int maxBuffers)
        {
            // Vertex buffers of size 0 don't make any sense. Let's not blindly hope for good behavior of OpenGL.
            Trace.Assert(bufferSize > 0);
            Trace.Assert(maxBuffers > 0);

            Size = bufferSize;

            AddAction = Add;
            VertexBuffers.Add(CreateVertexBuffer());
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
                foreach (VertexBuffer<T> vbo in VertexBuffers)
                    vbo.Dispose();
            }
        }

        #endregion

        public void ResetCounters()
        {
            currentBufferIndex = 0;

            foreach (var buf in VertexBuffers)
                buf.ResetCounters();
        }

        protected abstract VertexBuffer<T> CreateVertexBuffer();

        /// <summary>
        /// Adds a vertex to this <see cref="VertexBatch{T}"/>.
        /// </summary>
        /// <param name="v">The vertex to add.</param>
        public void Add(T v)
        {
            GLWrapper.SetActiveBatch(this);

            if (currentVertexBuffer.IsFull)
            {
                currentVertexBuffer.Draw();

                if (currentBufferIndex == VertexBuffers.Count - 1)
                    VertexBuffers.Add(CreateVertexBuffer());
                currentBufferIndex++;

                FrameStatistics.Increment(StatisticsCounterType.VBufOverflow);
            }

            currentVertexBuffer.Push(v);
        }

        public int Draw() => currentVertexBuffer.Draw();
    }
}
