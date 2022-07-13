// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Textures
{
    public class TextureSub : Texture
    {
        private readonly Texture parent;
        private readonly RectangleI bounds;

        public TextureSub(Texture parent, RectangleI bounds, WrapMode wrapModeS, WrapMode wrapModeT)
            : base(parent, wrapModeS, wrapModeT)
        {
            this.parent = parent;
            this.bounds = bounds;
        }

        internal override void SetData(ITextureUpload upload, WrapMode wrapModeS, WrapMode wrapModeT, Opacity? opacity)
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
            parent.SetData(upload, wrapModeS, wrapModeT, opacity);
        }

        protected override RectangleF TextureBounds(RectangleF? textureRect = null)
        {
            var actualBounds = base.TextureBounds(textureRect);

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
    }
}
