// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics.Rendering;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// A texture which can cleans up any resources held by the underlying <see cref="ITexture"/> on <see cref="Dispose"/>.
    /// </summary>
    public class DisposableTexture : Texture
    {
        internal DisposableTexture(ITexture textureGl)
            : base(textureGl)
        {
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            TextureGL.Dispose();
        }
    }
}
