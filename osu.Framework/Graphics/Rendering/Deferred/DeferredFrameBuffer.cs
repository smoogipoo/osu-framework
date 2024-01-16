// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Veldrid.Buffers;
using osuTK;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal class DeferredFrameBuffer : IFrameBuffer
    {
        public VeldridFrameBuffer Resource { get; }

        private readonly DeferredRenderer renderer;

        public DeferredFrameBuffer(DeferredRenderer renderer, VeldridFrameBuffer frameBuffer)
        {
            this.renderer = renderer;
            Resource = frameBuffer;
        }

        public Texture Texture => Resource.Texture;

        public Vector2 Size
        {
            get => Resource.Size;
            set => Resource.Size = value;
        }

        public void Bind() => renderer.BindFrameBuffer(this);

        public void Unbind() => renderer.UnbindFrameBuffer(this);

        public void Dispose()
        {
            // Todo:
        }
    }
}
