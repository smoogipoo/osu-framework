﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Platform;
using osuTK;

namespace osu.Framework.Graphics.Shapes
{
    /// <summary>
    /// A simple rectangular box. Can be colored using the <see cref="Drawable.Colour"/> property.
    /// </summary>
    public class Box : Sprite
    {
        public Box()
        {
            // Texture is late bound but would otherwise set an initial (1, 1) size for relative size.
            Size = Vector2.One;
        }

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            base.Texture = host.Renderer.WhiteTexture;
        }

        public override Texture Texture
        {
            get => base.Texture;
            set => throw new InvalidOperationException($"The texture of a {nameof(Box)} cannot be set.");
        }
    }
}
