// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Veldrid.Buffers;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    /// <summary>
    /// A vertex buffer implementation specialised for the Metal renderer.
    /// </summary>
    internal class VeldridVertexBuffer2 : IVeldridVertexBuffer
    {
        public int Size { get; }
        public DeviceBuffer Buffer { get; }
        public int Stride { get; }
        public VertexLayoutDescription Layout { get; }

        private readonly DeferredRenderer renderer;

        public VeldridVertexBuffer2(DeferredRenderer renderer, int size, int stride, VertexLayoutDescription layout)
        {
            this.renderer = renderer;

            Size = size;
            Stride = stride;
            Layout = layout;

            Buffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)(Stride * Size), BufferUsage.VertexBuffer));
        }

        public void Write(RendererStagingMemoryBlock block, CommandList commandList, int index)
        {
            block.CopyTo(renderer, commandList, Buffer, index * Stride);
        }

        ulong IVertexBuffer.LastUseFrameIndex => 0;

        bool IVertexBuffer.InUse => true;

        void IVertexBuffer.Free()
        {
        }

        ~VeldridVertexBuffer2()
        {
            renderer.ScheduleDisposal(v => v.Dispose(false), this);
        }

        public void Dispose()
        {
            renderer.ScheduleDisposal(v => v.Dispose(true), this);
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
    }
}
