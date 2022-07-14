﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osuTK;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal class FrameBuffer : IFrameBuffer
    {
        private int frameBuffer = -1;

        public Texture Texture { get; private set; }

        private readonly List<RenderBuffer> attachedRenderBuffers = new List<RenderBuffer>();

        private bool isInitialised;

        private readonly OpenGLRenderer renderer;
        private readonly RenderbufferInternalFormat[] renderBufferFormats;
        private readonly All filteringMode;

        private TextureGL textureGL;

        public FrameBuffer(OpenGLRenderer renderer, RenderbufferInternalFormat[] renderBufferFormats = null, All filteringMode = All.Linear)
        {
            this.renderer = renderer;
            this.renderBufferFormats = renderBufferFormats;
            this.filteringMode = filteringMode;
        }

        private Vector2 size = Vector2.One;

        /// <summary>
        /// Sets the size of the texture of this frame buffer.
        /// </summary>
        public Vector2 Size
        {
            get => size;
            set
            {
                if (value == size)
                    return;

                size = value;

                if (isInitialised)
                {
                    Texture.Width = (int)Math.Ceiling(size.X);
                    Texture.Height = (int)Math.Ceiling(size.Y);

                    textureGL.SetData(new TextureUpload());
                    textureGL.Upload();
                }
            }
        }

        private void initialise()
        {
            frameBuffer = GL.GenFramebuffer();
            Texture = renderer.CreateTexture(textureGL = new FrameBufferTexture(renderer, Size, filteringMode), WrapMode.None, WrapMode.None);

            renderer.BindFrameBuffer(frameBuffer);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget2d.Texture2D, textureGL.TextureId, 0);
            renderer.BindTexture(0);

            if (renderBufferFormats != null)
            {
                foreach (var format in renderBufferFormats)
                    attachedRenderBuffers.Add(new RenderBuffer(renderer, format));
            }
        }

        /// <summary>
        /// Binds the framebuffer.
        /// <para>Does not clear the buffer or reset the viewport/ortho.</para>
        /// </summary>
        public void Bind()
        {
            if (!isInitialised)
            {
                initialise();
                isInitialised = true;
            }
            else
            {
                // Buffer is bound during initialisation
                renderer.BindFrameBuffer(frameBuffer);
            }

            foreach (var buffer in attachedRenderBuffers)
                buffer.Bind(Size);
        }

        /// <summary>
        /// Unbinds the framebuffer.
        /// </summary>
        public void Unbind()
        {
            // See: https://community.arm.com/developer/tools-software/graphics/b/blog/posts/mali-performance-2-how-to-correctly-handle-framebuffers
            // Unbinding renderbuffers causes an invalidation of the relevant attachment of this framebuffer on embedded devices, causing the renderbuffers to remain transient.
            // This must be done _before_ the framebuffer is flushed via the framebuffer unbind process, otherwise the renderbuffer may be copied to system memory.
            foreach (var buffer in attachedRenderBuffers)
                buffer.Unbind();

            renderer.UnbindFrameBuffer(frameBuffer);
        }

        #region Disposal

        ~FrameBuffer()
        {
            renderer.ScheduleDisposal(b => b.Dispose(false), this);
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

            if (isInitialised)
            {
                textureGL?.Dispose();
                textureGL = null;

                renderer.DeleteFrameBuffer(frameBuffer);

                foreach (var buffer in attachedRenderBuffers)
                    buffer.Dispose();
            }

            isDisposed = true;
        }

        #endregion

        private class FrameBufferTexture : TextureGL
        {
            private readonly OpenGLRenderer renderer;

            public FrameBufferTexture(OpenGLRenderer renderer, Vector2 size, All filteringMode = All.Linear)
                : base(renderer,
                    Math.Clamp((int)Math.Ceiling(size.X), 1, renderer.MaxTextureSize),
                    Math.Clamp((int)Math.Ceiling(size.Y), 1, renderer.MaxTextureSize)
                    , true,
                    filteringMode)
            {
                this.renderer = renderer;
                BypassTextureUploadQueueing = true;

                SetData(new TextureUpload());
                Upload();
            }

            public override int Width
            {
                get => base.Width;
                set => base.Width = renderer == null ? value : Math.Clamp(value, 1, renderer.MaxTextureSize);
            }

            public override int Height
            {
                get => base.Height;
                set => base.Height = renderer == null ? value : Math.Clamp(value, 1, renderer.MaxTextureSize);
            }
        }
    }
}
