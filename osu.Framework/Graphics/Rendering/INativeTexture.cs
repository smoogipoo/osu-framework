// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Textures;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Rendering
{
    public interface INativeTexture : IDisposable
    {
        /// <summary>
        /// Maximum texture size in any direction.
        /// </summary>
        int MaxSize { get; }

        /// <summary>
        /// The width of the texture.
        /// </summary>
        int Width { get; set; }

        /// <summary>
        /// The height of the texture.
        /// </summary>
        int Height { get; set; }

        /// <summary>
        /// Whether the texture is in a usable state.
        /// </summary>
        bool Available { get; }

        /// <summary>
        /// By default, texture uploads are queued for upload at the beginning of each frame, allowing loading them ahead of time.
        /// When this is true, this will be bypassed and textures will only be uploaded on use. Should be set for every-frame texture uploads
        /// to avoid overloading the global queue.
        /// </summary>
        bool BypassTextureUploadQueueing { get; set; }

        /// <summary>
        /// Whether the latest data has been uploaded.
        /// </summary>
        bool UploadComplete { get; }

        /// <summary>
        /// Whether the texture is currently queued for upload.
        /// </summary>
        internal bool IsQueuedForUpload { get; set; }

        /// <summary>
        /// Flush any unprocessed uploads without actually uploading.
        /// </summary>
        internal void FlushUploads();

        /// <summary>
        /// Sets the pixel data of the texture.
        /// </summary>
        /// <param name="upload">The <see cref="ITextureUpload"/> containing the data.</param>
        void SetData(ITextureUpload upload);

        internal bool Upload();

        internal bool Bind(TextureUnit unit, WrapMode wrapModeS, WrapMode wrapModeT);
    }
}
