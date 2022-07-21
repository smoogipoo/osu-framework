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

        public override int Width => bounds.Width;

        public override int Height => bounds.Height;

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

            // Todo: Not sure if this is correct...
            parent.SetData(upload, wrapModeS, wrapModeT, opacity);
        }

        private RectangleF boundsInParent(RectangleF? textureRect)
        {
            RectangleF actualBounds = bounds;

            if (textureRect is RectangleF rect)
            {
                actualBounds.X += rect.X * ScaleAdjust;
                actualBounds.Y += rect.Y * ScaleAdjust;
                actualBounds.Width = rect.Width * ScaleAdjust;
                actualBounds.Height = rect.Height * ScaleAdjust;
            }

            return actualBounds / parent.ScaleAdjust;
        }

        protected override RectangleF TextureBounds(RectangleF? textureRect = null) => parent.GetTextureRect(boundsInParent(textureRect));
    }
}
