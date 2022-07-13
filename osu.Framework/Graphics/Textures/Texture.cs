﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.IO;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics.Rendering;
using osuTK;
using osuTK.Graphics.ES30;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;

namespace osu.Framework.Graphics.Textures
{
    public class Texture : IDisposable
    {
        protected virtual INativeTexture NativeTexture { get; }

        public string Filename;
        public string AssetName;

        /// <summary>
        /// A lookup key used by <see cref="TextureStore"/>s.
        /// </summary>
        internal string LookupKey;

        /// <summary>
        /// At what multiple of our expected resolution is our underlying texture?
        /// </summary>
        public float ScaleAdjust = 1;

        public float DisplayWidth => Width / ScaleAdjust;
        public float DisplayHeight => Height / ScaleAdjust;

        public Opacity Opacity { get; protected set; } = Opacity.Mixed;

        /// <summary>
        /// The texture wrap mode in horizontal direction.
        /// </summary>
        public readonly WrapMode WrapModeS;

        /// <summary>
        /// The texture wrap mode in vertical direction.
        /// </summary>
        public readonly WrapMode WrapModeT;

        /// <summary>
        /// Create a new texture.
        /// </summary>
        /// <param name="nativeTexture">The GL texture.</param>
        /// <param name="wrapModeS">The texture wrap mode in horizontal direction.</param>
        /// <param name="wrapModeT">The texture wrap mode in vertical direction.</param>
        internal Texture(INativeTexture nativeTexture, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None)
        {
            NativeTexture = nativeTexture ?? throw new ArgumentNullException(nameof(nativeTexture));
            WrapModeS = wrapModeS;
            WrapModeT = wrapModeT;
        }

        /// <summary>
        /// Creates a new texture using the same backing texture as another <see cref="Texture"/>.
        /// </summary>
        /// <param name="parent">The other <see cref="Texture"/>.</param>
        /// <param name="wrapModeS">The texture wrap mode in horizontal direction.</param>
        /// <param name="wrapModeT">The texture wrap mode in vertical direction.</param>
        public Texture(Texture parent, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None)
            : this(parent.NativeTexture, wrapModeS, wrapModeT)
        {
        }

        /// <summary>
        /// Crop the texture.
        /// </summary>
        /// <param name="cropRectangle">The rectangle the cropped texture should reference.</param>
        /// <param name="relativeSizeAxes">Which axes have a relative size in [0,1] in relation to the texture size.</param>
        /// <param name="wrapModeS">The texture wrap mode in horizontal direction.</param>
        /// <param name="wrapModeT">The texture wrap mode in vertical direction.</param>
        /// <returns>The cropped texture.</returns>
        public Texture Crop(RectangleF cropRectangle, Axes relativeSizeAxes = Axes.None, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None)
        {
            if (relativeSizeAxes != Axes.None)
            {
                Vector2 scale = new Vector2(
                    relativeSizeAxes.HasFlagFast(Axes.X) ? Width : 1,
                    relativeSizeAxes.HasFlagFast(Axes.Y) ? Height : 1
                );
                cropRectangle *= scale;
            }

            return new TextureSub(this, cropRectangle, wrapModeS, wrapModeT);
        }

        /// <summary>
        /// Creates a texture from a data stream representing a bitmap.
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="stream">The data stream containing the texture data.</param>
        /// <param name="atlas">The atlas to add the texture to.</param>
        /// <returns>The created texture.</returns>
        public static Texture FromStream(IRenderer renderer, Stream stream, TextureAtlas atlas = null)
        {
            if (stream == null || stream.Length == 0)
                return null;

            try
            {
                var data = new TextureUpload(stream);
                Texture tex = atlas == null ? renderer.CreateTexture(data.Width, data.Height) : atlas.Add(data.Width, data.Height);
                tex.SetData(data);
                return tex;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public virtual int Width
        {
            get => NativeTexture.Width;
            set => NativeTexture.Width = value;
        }

        public virtual int Height
        {
            get => NativeTexture.Height;
            set => NativeTexture.Height = value;
        }

        public Vector2 Size => new Vector2(Width, Height);

        internal bool Bind(TextureUnit unit, WrapMode wrapModeS, WrapMode wrapModeT) => NativeTexture.Bind(unit, wrapModeS, wrapModeT);

        /// <summary>
        /// Queue a <see cref="TextureUpload"/> to be uploaded on the draw thread.
        /// The provided upload will be disposed after the upload is completed.
        /// </summary>
        /// <param name="upload"></param>
        public void SetData(ITextureUpload upload) => SetData(upload, WrapModeS, WrapModeT, null);

        internal virtual void SetData(ITextureUpload upload, WrapMode wrapModeS, WrapMode wrapModeT, Opacity? opacity)
        {
            if (!Available)
                throw new ObjectDisposedException(ToString(), "Can not set data of a disposed texture.");

            if (upload.Bounds.Width > NativeTexture.MaxSize || upload.Bounds.Height > NativeTexture.MaxSize)
                throw new TextureTooLargeForGLException();

            if (upload.Bounds.IsEmpty && upload.Data.Length > 0)
            {
                upload.Bounds = GetTextureRect().AABB;

                if (upload.Bounds.Width * upload.Bounds.Height > upload.Data.Length)
                {
                    throw new InvalidOperationException(
                        $"Size of texture upload ({upload.Bounds.Width}x{upload.Bounds.Height}) does not contain enough data ({upload.Data.Length} < {upload.Bounds.Width * upload.Bounds.Height})");
                }
            }

            UpdateOpacity(upload, ref opacity);

            NativeTexture?.SetData(upload);
        }

        protected static Opacity ComputeOpacity(ITextureUpload upload)
        {
            // TODO: Investigate performance issues and revert functionality once we are sure there is no overhead.
            // see https://github.com/ppy/osu/issues/9307
            return Opacity.Mixed;

            // ReadOnlySpan<Rgba32> data = upload.Data;
            //
            // if (data.Length == 0)
            //     return Opacity.Transparent;
            //
            // int firstPixelValue = data[0].A;
            //
            // // Check if the first pixel has partial transparency (neither fully-opaque nor fully-transparent).
            // if (firstPixelValue != 0 && firstPixelValue != 255)
            //     return Opacity.Mixed;
            //
            // // The first pixel is GUARANTEED to be either fully-opaque or fully-transparent.
            // // Now we need to go through the rest of the image and check that every other pixel matches this value.
            // for (int i = 1; i < data.Length; i++)
            // {
            //     if (data[i].A != firstPixelValue)
            //         return Opacity.Mixed;
            // }
            //
            // return firstPixelValue == 0 ? Opacity.Transparent : Opacity.Opaque;
        }

        protected void UpdateOpacity(ITextureUpload upload, ref Opacity? uploadOpacity)
        {
            // Compute opacity if it doesn't have a value yet
            uploadOpacity ??= ComputeOpacity(upload);

            // Update the texture's opacity depending on the upload's opacity.
            // If the upload covers the entire bounds of the texture, it fully
            // determines the texture's opacity. Otherwise, it can only turn
            // the texture's opacity into a mixed state (if it disagrees with
            // the texture's existing opacity).
            if (upload.Bounds == GetTextureRect().AABB && upload.Level == 0)
                Opacity = uploadOpacity.Value;
            else if (uploadOpacity.Value != Opacity)
                Opacity = Opacity.Mixed;
        }

        protected virtual RectangleF TextureBounds(RectangleF? textureRect = null)
        {
            RectangleF texRect = textureRect ?? new RectangleF(0, 0, DisplayWidth, DisplayHeight);

            texRect.X *= ScaleAdjust / Width;
            texRect.Y *= ScaleAdjust / Height;
            texRect.Width *= ScaleAdjust / Width;
            texRect.Height *= ScaleAdjust / Height;

            return texRect;
        }

        public RectangleF GetTextureRect(RectangleF? textureRect = null) => TextureBounds(textureRect);

        public override string ToString() => $@"{AssetName} ({Width}, {Height})";

        /// <summary>
        /// Whether <see cref="NativeTexture"/> is in a usable state.
        /// </summary>
        public virtual bool Available => NativeTexture.Available;

        /// <summary>
        /// Whether the latest data has been uploaded.
        /// </summary>
        public bool UploadComplete => NativeTexture.UploadComplete;

        /// <summary>
        /// Flush any unprocessed uploads without actually uploading.
        /// </summary>
        internal void FlushUploads() => NativeTexture.FlushUploads();

        internal bool HasSameNativeTexture(Texture other) => NativeTexture == other.NativeTexture;

        /// <summary>
        /// By default, texture uploads are queued for upload at the beginning of each frame, allowing loading them ahead of time.
        /// When this is true, this will be bypassed and textures will only be uploaded on use. Should be set for every-frame texture uploads
        /// to avoid overloading the global queue.
        /// </summary>
        public bool BypassTextureUploadQueueing
        {
            get => NativeTexture.BypassTextureUploadQueueing;
            set => NativeTexture.BypassTextureUploadQueueing = value;
        }

        #region Disposal

        // Intentionally no finalizer implementation as our disposal is NOOP. Finalizer is implemented in TextureWithRefCount usage.

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
        }

        #endregion
    }
}
