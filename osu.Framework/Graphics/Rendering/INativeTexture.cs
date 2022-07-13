// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Primitives;
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
        /// Whether the texture is opaque, transparent, or a mix of both.
        /// </summary>
        Opacity Opacity { get; }

        /// <summary>
        /// The texture wrap mode in horizontal direction.
        /// </summary>
        WrapMode WrapModeS { get; }

        /// <summary>
        /// The texture wrap mode in vertical direction.
        /// </summary>
        WrapMode WrapModeT { get; }

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
        /// Whether the texture is currently queued for upload.
        /// </summary>
        internal bool IsQueuedForUpload { get; set; }

        /// <summary>
        /// Sets the pixel data of the texture.
        /// </summary>
        /// <param name="upload">The <see cref="ITextureUpload"/> containing the data.</param>
        void SetData(ITextureUpload upload);

        /// <summary>
        /// Sets the pixel data of this <see cref="TextureGLAtlas"/>.
        /// </summary>
        /// <param name="upload">The <see cref="ITextureUpload"/> containing the data.</param>
        /// <param name="wrapModeS">The texture wrap mode in horizontal direction.</param>
        /// <param name="wrapModeT">The texture wrap mode in vertical direction.</param>
        /// <param name="uploadOpacity">Whether the upload is opaque, transparent, or a mix of both.</param>
        internal void SetData(ITextureUpload upload, WrapMode wrapModeS, WrapMode wrapModeT, Opacity? uploadOpacity);

        internal bool Upload();

        internal bool Bind(TextureUnit unit, WrapMode wrapModeS, WrapMode wrapModeT);

        RectangleF GetTextureRect(RectangleF? textureRect);
    }
}
