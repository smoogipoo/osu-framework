// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using osu.Framework.Graphics.OpenGL.Vertices;
using osuTK.Graphics.ES30;
using osu.Framework.Statistics;
using osu.Framework.Development;
using osu.Framework.Platform;
using SixLabors.ImageSharp.Memory;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    public abstract class VertexBuffer<T> : IVertexBuffer, IDisposable
        where T : struct, IEquatable<T>, IVertex
    {
        protected static readonly int STRIDE = VertexUtils<DepthWrappingVertex<T>>.STRIDE;

        private readonly BufferUsageHint usage;

        private Memory<DepthWrappingVertex<T>> vertexMemory;
        private IMemoryOwner<DepthWrappingVertex<T>> memoryOwner;
        private NativeMemoryTracker.NativeMemoryLease memoryLease;

        private int vboId = -1;

        protected VertexBuffer(int amountVertices, BufferUsageHint usage)
        {
            this.usage = usage;

            Size = amountVertices;
        }

        /// <summary>
        /// Gets the number of vertices in this <see cref="VertexBuffer{T}"/>.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Initialises this <see cref="VertexBuffer{T}"/>. Guaranteed to be run on the draw thread.
        /// </summary>
        protected virtual void Initialise()
        {
            ThreadSafety.EnsureDrawThread();

            GL.GenBuffers(1, out vboId);

            if (GLWrapper.BindBuffer(BufferTarget.ArrayBuffer, vboId))
                VertexUtils<DepthWrappingVertex<T>>.Bind();

            int size = Size * STRIDE;

            memoryLease = NativeMemoryTracker.AddMemory(this, Size * STRIDE);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)size, IntPtr.Zero, usage);
        }

        ~VertexBuffer()
        {
            GLWrapper.ScheduleDisposal(() => Dispose(false));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected bool IsDisposed { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            ((IVertexBuffer)this).Free();

            IsDisposed = true;
        }

        public void ResetCounters()
        {
            isNew = memoryOwner == null;
            changeStartIndex = -1;
            changeCount = 0;
            drawStartIndex = 0;
            drawCount = 0;
        }

        private bool isNew = true;
        private int changeStartIndex = -1;
        private int changeCount;
        private int drawStartIndex;
        private int drawCount;

        public bool IsFull => drawStartIndex + drawCount == Size;

        public void Push(T vertex)
        {
            if (IsFull)
                throw new InvalidOperationException("Vertex buffer is too small to contain the requested vertex.");

            int currentVertexIndex = drawStartIndex + drawCount;
            ref var currentVertex = ref getMemory().Span[currentVertexIndex];

            // Begin tracking changes only if the DrawNode is invalidated. This continues until the next Draw() as different counts of vertices can be pushed by the DrawNode.
            if (isNew || GLWrapper.IsDrawNodeInvalidated)
            {
                if (changeStartIndex == -1)
                    changeStartIndex = currentVertexIndex;
            }

            // Update the vertex only if we're tracking changes.
            if (changeStartIndex != -1)
            {
                currentVertex.Vertex = vertex;
                currentVertex.BackbufferDrawDepth = GLWrapper.BackbufferDrawDepth;
                changeCount++;
            }

            drawCount++;
            LastUseResetId = GLWrapper.ResetId;
        }

        public int Draw()
        {
            if (drawCount == 0)
                return 0;

            Bind(true);

            if (changeCount > 0)
            {
                GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)(changeStartIndex * STRIDE), (IntPtr)(changeCount * STRIDE), ref getMemory().Span[changeStartIndex]);
                FrameStatistics.Add(StatisticsCounterType.VerticesUpl, changeCount);

                changeStartIndex = -1;
                changeCount = 0;
            }

            GL.DrawElements(Type, ToElements(drawCount), DrawElementsType.UnsignedShort, (IntPtr)(ToElementIndex(drawStartIndex) * sizeof(ushort)));

            FrameStatistics.Increment(StatisticsCounterType.DrawCalls);
            FrameStatistics.Add(StatisticsCounterType.VerticesDraw, drawCount);

            Unbind();

            int lastDrawCount = drawCount;
            drawStartIndex += drawCount;
            drawCount = 0;
            LastUseResetId = GLWrapper.ResetId;

            return lastDrawCount;
        }

        public virtual void Bind(bool forRendering)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not bind disposed vertex buffers.");

            if (vboId == -1)
                Initialise();

            if (GLWrapper.BindBuffer(BufferTarget.ArrayBuffer, vboId))
                VertexUtils<DepthWrappingVertex<T>>.Bind();
        }

        public virtual void Unbind()
        {
        }

        protected virtual int ToElements(int vertices) => vertices;

        protected virtual int ToElementIndex(int vertexIndex) => vertexIndex;

        protected abstract PrimitiveType Type { get; }

        private ref Memory<DepthWrappingVertex<T>> getMemory()
        {
            ThreadSafety.EnsureDrawThread();

            if (!InUse)
            {
                memoryOwner = SixLabors.ImageSharp.Configuration.Default.MemoryAllocator.Allocate<DepthWrappingVertex<T>>(Size, AllocationOptions.Clean);
                vertexMemory = memoryOwner.Memory;
                isNew = true;

                GLWrapper.RegisterVertexBufferUse(this);
            }

            return ref vertexMemory;
        }

        public ulong LastUseResetId { get; private set; }

        public bool InUse => LastUseResetId > 0;

        void IVertexBuffer.Free()
        {
            if (vboId != -1)
            {
                Unbind();

                memoryLease?.Dispose();
                memoryLease = null;

                GL.DeleteBuffer(vboId);
                vboId = -1;
            }

            memoryOwner?.Dispose();
            memoryOwner = null;
            vertexMemory = Memory<DepthWrappingVertex<T>>.Empty;

            LastUseResetId = 0;
        }
    }
}
