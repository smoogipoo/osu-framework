// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.IO;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Rendering;
using osuTK;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;

namespace osu.Framework.Graphics.Textures
{
    public class Texture : IDisposable
    {
        public virtual INativeTexture TextureGL { get; }

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

        public Opacity Opacity => TextureGL.Opacity;

        public WrapMode WrapModeS => TextureGL.WrapModeS;

        public WrapMode WrapModeT => TextureGL.WrapModeT;

        /// <summary>
        /// Create a new texture.
        /// </summary>
        /// <param name="textureGl">The GL texture.</param>
        internal Texture(INativeTexture textureGl)
        {
            TextureGL = textureGl ?? throw new ArgumentNullException(nameof(textureGl));
        }

        /// <summary>
        /// Creates a new texture using the same backing texture as another <see cref="Texture"/>.
        /// </summary>
        /// <param name="other">The other <see cref="Texture"/>.</param>
        public Texture(Texture other)
            : this(other.TextureGL)
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

            return new Texture(new TextureSub(TextureGL, cropRectangle, wrapModeS, wrapModeT));
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
                Texture tex = atlas == null ? renderer.CreateTexture(data.Width, data.Height) : new Texture(atlas.Add(data.Width, data.Height));
                tex.SetData(data);
                return tex;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public int Width
        {
            get => TextureGL.Width;
            set => TextureGL.Width = value;
        }

        public int Height
        {
            get => TextureGL.Height;
            set => TextureGL.Height = value;
        }

        public Vector2 Size => new Vector2(Width, Height);

        /// <summary>
        /// Queue a <see cref="TextureUpload"/> to be uploaded on the draw thread.
        /// The provided upload will be disposed after the upload is completed.
        /// </summary>
        /// <param name="upload"></param>
        public void SetData(ITextureUpload upload)
        {
            TextureGL?.SetData(upload);
        }

        protected virtual RectangleF TextureBounds(RectangleF? textureRect = null)
        {
            RectangleF texRect = textureRect ?? new RectangleF(0, 0, DisplayWidth, DisplayHeight);

            if (ScaleAdjust != 1)
            {
                texRect.Width *= ScaleAdjust;
                texRect.Height *= ScaleAdjust;
                texRect.X *= ScaleAdjust;
                texRect.Y *= ScaleAdjust;
            }

            return texRect;
        }

        public RectangleF GetTextureRect(RectangleF? textureRect = null) => TextureGL.GetTextureRect(TextureBounds(textureRect));

        public override string ToString() => $@"{AssetName} ({Width}, {Height})";

        /// <summary>
        /// Whether <see cref="TextureGL"/> is in a usable state.
        /// </summary>
        public virtual bool Available => TextureGL.Available;

        /// <summary>
        /// Whether the latest data has been uploaded.
        /// </summary>
        public bool UploadComplete => TextureGL.UploadComplete;

        /// <summary>
        /// Flush any unprocessed uploads without actually uploading.
        /// </summary>
        internal void FlushUploads() => TextureGL.FlushUploads();

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
