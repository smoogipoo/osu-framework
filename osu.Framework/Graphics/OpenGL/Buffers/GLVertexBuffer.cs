// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Development;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal abstract class GLVertexBuffer<T> : IVertexBuffer, IDisposable
        where T : unmanaged, IEquatable<T>, IVertex
    {
        protected static readonly int STRIDE = GLVertexUtils<DepthWrappingVertex<T>>.STRIDE;

        protected readonly GLRenderer Renderer;
        private readonly BufferUsageHint usage;

        private unsafe DepthWrappingVertex<T>* bufferPtr;

        private int vboId = -1;

        protected GLVertexBuffer(GLRenderer renderer, int amountVertices, BufferUsageHint usage)
        {
            Renderer = renderer;
            this.usage = usage;

            Size = amountVertices;
        }

        /// <summary>
        /// Sets the vertex at a specific index of this <see cref="GLVertexBuffer{T}"/>.
        /// </summary>
        /// <param name="vertexIndex">The index of the vertex.</param>
        /// <param name="vertex">The vertex.</param>
        /// <returns>Whether the vertex changed.</returns>
        public bool SetVertex(int vertexIndex, T vertex)
        {
            unsafe
            {
                Bind(false);

                DepthWrappingVertex<T>* vtx = &bufferPtr[vertexIndex];

                vtx->Vertex = vertex;
                vtx->BackbufferDrawDepth = Renderer.BackbufferDrawDepth;

                return true;
            }
        }

        /// <summary>
        /// Gets the number of vertices in this <see cref="GLVertexBuffer{T}"/>.
        /// </summary>
        public int Size { get; }

        public void Init() => Initialise();

        /// <summary>
        /// Initialises this <see cref="GLVertexBuffer{T}"/>. Guaranteed to be run on the draw thread.
        /// </summary>
        protected virtual unsafe void Initialise()
        {
            ThreadSafety.EnsureDrawThread();

            GL.GenBuffers(1, out vboId);

            if (Renderer.BindBuffer(BufferTarget.ArrayBuffer, vboId))
                GLVertexUtils<DepthWrappingVertex<T>>.Bind();

            int size = Size * STRIDE;

            osuTK.Graphics.OpenGL4.GL.BufferStorage(
                osuTK.Graphics.OpenGL4.BufferTarget.ArrayBuffer,
                size,
                IntPtr.Zero,
                osuTK.Graphics.OpenGL4.BufferStorageFlags.MapWriteBit | osuTK.Graphics.OpenGL4.BufferStorageFlags.MapPersistentBit);

            bufferPtr = (DepthWrappingVertex<T>*)GL.MapBufferRange(
                BufferTarget.ArrayBuffer,
                IntPtr.Zero,
                size,
                BufferAccessMask.MapWriteBit | BufferAccessMask.MapPersistentBit | BufferAccessMask.MapUnsynchronizedBit).ToPointer();
        }

        ~GLVertexBuffer()
        {
            Renderer.ScheduleDisposal(v => v.Dispose(false), this);
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

        public virtual void Bind(bool forRendering)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not bind disposed vertex buffers.");

            if (Renderer.BindBuffer(BufferTarget.ArrayBuffer, vboId))
                GLVertexUtils<DepthWrappingVertex<T>>.Bind();
        }

        public virtual void Unbind()
        {
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

            int countVertices = endIndex - startIndex;
            GL.DrawElements(Type, ToElements(countVertices), DrawElementsType.UnsignedShort, (IntPtr)(ToElementIndex(startIndex) * sizeof(ushort)));

            Unbind();
        }

        public void Update()
        {
            UpdateRange(0, Size);
        }

        public void UpdateRange(int startIndex, int endIndex)
        {
            Bind(false);
            Unbind();
        }

        public ulong LastUseResetId { get; private set; }

        public bool InUse => LastUseResetId > 0;

        void IVertexBuffer.Free()
        {
            if (vboId != -1)
            {
                Unbind();
                GL.DeleteBuffer(vboId);
                vboId = -1;
            }

            LastUseResetId = 0;
        }
    }
}
