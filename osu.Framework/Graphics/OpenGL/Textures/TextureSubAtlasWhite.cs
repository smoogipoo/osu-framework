// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    /// <summary>
    /// A special texture which refers to the area of a texture atlas which is white.
    /// Allows use of such areas while being unaware of whether we need to bind a texture or not.
    /// </summary>
    internal class TextureSubAtlasWhite : TextureSub
    {
        public TextureSubAtlasWhite(INativeTexture parent)
            : base(parent, new RectangleI(0, 0, 1, 1), parent.WrapModeS, parent.WrapModeT)
        {
        }
    }
}
