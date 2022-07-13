// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.Rendering;
using osuTK.Graphics.ES30;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    public abstract class TextureGL : INativeTexture
    {
        protected readonly OpenGLRenderer Renderer;

        protected TextureGL(OpenGLRenderer renderer)
        {
            Renderer = renderer;
        }

        #region Disposal

        public abstract bool UploadComplete { get; }

        bool INativeTexture.IsQueuedForUpload { get; set; }

        /// <summary>
        /// By default, texture uploads are queued for upload at the beginning of each frame, allowing loading them ahead of time.
        /// When this is true, this will be bypassed and textures will only be uploaded on use. Should be set for every-frame texture uploads
        /// to avoid overloading the global queue.
        /// </summary>
        public bool BypassTextureUploadQueueing { get; set; }

        /// <summary>
        /// Whether this <see cref="TextureGL"/> can used for drawing.
        /// </summary>
        public bool Available { get; private set; } = true;

        private bool isDisposed;

        protected virtual void Dispose(bool isDisposing)
        {
            Renderer.ScheduleDisposal(t => t.Available = false, this);
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        public int MaxSize => Renderer.MaxTextureSize;

        public abstract int TextureId { get; }

        public abstract int Height { get; set; }

        public abstract int Width { get; set; }

        /// <summary>
        /// Bind as active texture.
        /// </summary>
        /// <param name="unit">The texture unit to bind to.</param>
        /// <param name="wrapModeS">The texture wrap mode in horizontal direction.</param>
        /// <param name="wrapModeT">The texture wrap mode in vertical direction.</param>
        /// <returns>True if bind was successful.</returns>
        internal abstract bool Bind(TextureUnit unit, WrapMode wrapModeS, WrapMode wrapModeT);

        bool INativeTexture.Bind(TextureUnit unit, WrapMode wrapModeS, WrapMode wrapModeT) => Bind(unit, wrapModeS, wrapModeT);

        /// <summary>
        /// Uploads pending texture data to the GPU if it exists.
        /// </summary>
        /// <returns>Whether pending data existed and an upload has been performed.</returns>
        internal abstract bool Upload();

        bool INativeTexture.Upload() => Upload();

        protected abstract void FlushUploads();

        void INativeTexture.FlushUploads() => FlushUploads();

        /// <summary>
        /// Sets the pixel data of this <see cref="TextureGL"/>.
        /// </summary>
        /// <param name="upload">The <see cref="ITextureUpload"/> containing the data.</param>
        public abstract void SetData(ITextureUpload upload);
    }
}
