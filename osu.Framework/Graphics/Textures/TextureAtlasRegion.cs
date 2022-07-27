// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Textures
{
    internal class TextureAtlasRegion : TextureRegion
    {
        public TextureAtlasRegion(Texture parent, RectangleI bounds, WrapMode wrapModeS, WrapMode wrapModeT)
            : base(parent, bounds, wrapModeS, wrapModeT)
        {
        }
    }
}
