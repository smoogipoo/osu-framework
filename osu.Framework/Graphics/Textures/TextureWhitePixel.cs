// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Textures
{
    internal class TextureWhitePixel : TextureSub
    {
        public TextureWhitePixel(Texture texture)
            : base(texture, new RectangleI(0, 0, 1, 1), texture.WrapModeS, texture.WrapModeT)
        {
        }
    }
}
