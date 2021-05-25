// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal class RenderBuffer : IDisposable
    {
        private readonly RenderbufferInternalFormat format;
        private readonly int sizePerPixel;

        private int renderBuffer = -1;
        private FramebufferAttachment attachment;

        public RenderBuffer(RenderbufferInternalFormat format)
        {
            this.format = format;

            attachment = format.GetAttachmentType();
            sizePerPixel = format.GetBytesPerPixel();
        }

        private Vector2 internalSize;
        private NativeMemoryTracker.NativeMemoryLease memoryLease;

        public void Bind(Vector2 size)
        {
            size = Vector2.Clamp(size, Vector2.One, new Vector2(GLWrapper.MaxRenderBufferSize));

            // See: https://www.khronos.org/registry/OpenGL/extensions/EXT/EXT_multisampled_render_to_texture.txt
            //    + https://developer.apple.com/library/archive/documentation/3DDrawing/Conceptual/OpenGLES_ProgrammingGuide/WorkingwithEAGLContexts/WorkingwithEAGLContexts.html
            // OpenGL ES allows the driver to discard renderbuffer contents after they are presented to the screen, so the storage must always be re-initialised for embedded devices.
            // Such discard does not exist on non-embedded platforms, so they are only re-initialised when required.
            if (GLWrapper.IsEmbedded || internalSize.X < size.X || internalSize.Y < size.Y)
            {
                delete();

                renderBuffer = GL.GenRenderbuffer();
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderBuffer);
                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, format, (int)Math.Ceiling(size.X), (int)Math.Ceiling(size.Y));
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, attachment, RenderbufferTarget.Renderbuffer, renderBuffer);

                memoryLease = NativeMemoryTracker.AddMemory(this, (long)(size.X * size.Y * sizePerPixel));
                internalSize = size;
            }
        }

        public void Unbind()
        {
            if (GLWrapper.IsEmbedded)
            {
                // Renderbuffers are not automatically discarded on all embedded devices, so invalidation is forced for extra performance and to unify logic between devices.
                GL.InvalidateFramebuffer(FramebufferTarget.Framebuffer, 1, ref attachment);
            }
        }

        private void delete()
        {
            if (renderBuffer == -1)
                return;

            GL.DeleteRenderbuffer(renderBuffer);
            renderBuffer = -1;

            memoryLease?.Dispose();
        }

        #region Disposal

        ~RenderBuffer()
        {
            GLWrapper.ScheduleDisposal(() => Dispose(false));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            delete();

            isDisposed = true;
        }

        #endregion
    }
}
