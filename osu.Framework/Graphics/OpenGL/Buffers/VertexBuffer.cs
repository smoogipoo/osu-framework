// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using osu.Framework.Graphics.OpenGL.Vertices;
using osuTK.Graphics.ES30;
using osu.Framework.Statistics;
using osu.Framework.Development;
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

        private bool isInitialised;
        private int vaoId;
        private int vboId;

        protected VertexBuffer(int amountVertices, BufferUsageHint usage)
        {
            this.usage = usage;

            Size = amountVertices;
        }

        /// <summary>
        /// Sets the vertex at a specific index of this <see cref="VertexBuffer{T}"/>.
        /// </summary>
        /// <param name="vertexIndex">The index of the vertex.</param>
        /// <param name="vertex">The vertex.</param>
        /// <returns>Whether the vertex changed.</returns>
        public bool SetVertex(int vertexIndex, T vertex)
        {
            ref var currentVertex = ref getMemory().Span[vertexIndex];

            bool isNewVertex = !currentVertex.Vertex.Equals(vertex) || currentVertex.BackbufferDrawDepth != GLWrapper.BackbufferDrawDepth;

            currentVertex.Vertex = vertex;
            currentVertex.BackbufferDrawDepth = GLWrapper.BackbufferDrawDepth;

            return isNewVertex;
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

            int size = Size * STRIDE;

            vboId = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboId);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)size, IntPtr.Zero, usage);

            VertexUtils<DepthWrappingVertex<T>>.SetAttributes();
        }

        ~VertexBuffer()
        {
            GLWrapper.ScheduleDisposal(v => v.Dispose(false), this);
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

        public void Bind(bool forRendering)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not bind disposed vertex buffers.");

            if (!isInitialised)
            {
                GLWrapper.BindVertexArray(vaoId = GL.GenVertexArray());
                Initialise();

                isInitialised = true;
            }
            else
                GLWrapper.BindVertexArray(vaoId);
        }

        protected virtual int ToElements(int vertices) => vertices;

        protected virtual int ToElementIndex(int vertexIndex) => vertexIndex;

        protected abstract PrimitiveType Type { get; }

        public void Draw()
        {
            DrawRange(0, Size);
        }

        public void DrawRange(int startIndex, int endIndex)
        {
            Bind(true);

            int amountVertices = endIndex - startIndex;
            GL.DrawElements(Type, ToElements(amountVertices), DrawElementsType.UnsignedShort, (IntPtr)(ToElementIndex(startIndex) * sizeof(ushort)));
        }

        public void Update()
        {
            UpdateRange(0, Size);
        }

        public void UpdateRange(int startIndex, int endIndex)
        {
            Bind(false);

            int countVertices = endIndex - startIndex;

            GL.BindBuffer(BufferTarget.ArrayBuffer, vboId);
            GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)(startIndex * STRIDE), (IntPtr)(countVertices * STRIDE), ref getMemory().Span[startIndex]);

            FrameStatistics.Add(StatisticsCounterType.VerticesUpl, countVertices);
        }

        private ref Memory<DepthWrappingVertex<T>> getMemory()
        {
            ThreadSafety.EnsureDrawThread();

            if (!InUse)
            {
                memoryOwner = SixLabors.ImageSharp.Configuration.Default.MemoryAllocator.Allocate<DepthWrappingVertex<T>>(Size, AllocationOptions.Clean);
                vertexMemory = memoryOwner.Memory;

                GLWrapper.RegisterVertexBufferUse(this);
            }

            LastUseResetId = GLWrapper.ResetId;

            return ref vertexMemory;
        }

        public ulong LastUseResetId { get; private set; }

        public bool InUse => LastUseResetId > 0;

        void IVertexBuffer.Free()
        {
            if (isInitialised)
            {
                GL.DeleteBuffer(vboId);
                GL.DeleteVertexArray(vaoId);
            }

            memoryOwner?.Dispose();
            memoryOwner = null;
            vertexMemory = Memory<DepthWrappingVertex<T>>.Empty;

            LastUseResetId = 0;

            isInitialised = false;
        }
    }
}
