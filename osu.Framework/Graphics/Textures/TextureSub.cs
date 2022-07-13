// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Textures
{
    internal class TextureSub : INativeTexture
    {
        private readonly INativeTexture parent;
        private RectangleI bounds;

        public TextureSub(INativeTexture parent, RectangleI bounds, WrapMode wrapModeS, WrapMode wrapModeT)
        {
            this.parent = parent;
            this.bounds = bounds;

            WrapModeS = wrapModeS;
            WrapModeT = wrapModeT;
        }

        public int MaxSize => parent.MaxSize;
        public Opacity Opacity => Opacity.Mixed;
        public WrapMode WrapModeS { get; }
        public WrapMode WrapModeT { get; }

        public int Width
        {
            get => bounds.Width;
            set => bounds.Height = value;
        }

        public int Height
        {
            get => bounds.Height;
            set => bounds.Height = value;
        }

        public bool Available => parent.Available;

        public bool BypassTextureUploadQueueing
        {
            get => parent.BypassTextureUploadQueueing;
            set => throw new InvalidOperationException(); // Todo: I'm preeeeeetty sure this is correct, need to check.
        }

        public bool UploadComplete => parent.UploadComplete;

        bool INativeTexture.IsQueuedForUpload
        {
            get => parent.IsQueuedForUpload;
            set => parent.IsQueuedForUpload = value;
        }

        void INativeTexture.FlushUploads() => parent.FlushUploads();

        public void SetData(ITextureUpload upload) => ((INativeTexture)this).SetData(upload, WrapModeS, WrapModeS, null);

        void INativeTexture.SetData(ITextureUpload upload, WrapMode wrapModeS, WrapMode wrapModeT, Opacity? uploadOpacity)
        {
            if (upload.Bounds.Width > bounds.Width || upload.Bounds.Height > bounds.Height)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(upload),
                    $"Texture is too small to fit the requested upload. Texture size is {bounds.Width} x {bounds.Height}, upload size is {upload.Bounds.Width} x {upload.Bounds.Height}.");
            }

            if (upload.Bounds.IsEmpty)
                upload.Bounds = bounds;
            else
            {
                var adjustedBounds = upload.Bounds;

                adjustedBounds.X += bounds.X;
                adjustedBounds.Y += bounds.Y;

                upload.Bounds = adjustedBounds;
            }

            // Todo: How?
            // UpdateOpacity(upload, ref uploadOpacity);

            // Todo: Not sure if this is correct...
            parent.SetData(upload, wrapModeS, wrapModeT, uploadOpacity);
        }

        bool INativeTexture.Upload() => parent.Upload();

        bool INativeTexture.Bind(TextureUnit unit, WrapMode wrapModeS, WrapMode wrapModeT) => parent.Bind(unit, wrapModeS, wrapModeT);

        public RectangleF GetTextureRect(RectangleF? textureRect) => parent.GetTextureRect(boundsInParent(textureRect));

        private RectangleF boundsInParent(RectangleF? textureRect)
        {
            RectangleF actualBounds = bounds;

            if (textureRect.HasValue)
            {
                RectangleF localBounds = textureRect.Value;
                actualBounds.X += localBounds.X;
                actualBounds.Y += localBounds.Y;
                actualBounds.Width = localBounds.Width;
                actualBounds.Height = localBounds.Height;
            }

            return actualBounds;
        }

        public void Dispose()
        {
            // Todo: I'm preeeeeeeeeeeetty sure this shouldn't do anything, but TextureGLSub seemed to dispose the parent texture somehow?
        }
    }
}
