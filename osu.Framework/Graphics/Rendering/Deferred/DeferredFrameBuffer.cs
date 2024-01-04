// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    public class DeferredFrameBuffer : IFrameBuffer
    {
        public IFrameBuffer Resource { get; }

        private readonly DeferredRenderer renderer;

        public DeferredFrameBuffer(DeferredRenderer renderer, IFrameBuffer frameBuffer)
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

        public void Bind() => renderer.RenderEvents.Add(new BindFrameBufferEvent(this));

        public void Unbind() => renderer.RenderEvents.Add(new UnbindFrameBufferEvent(this));

        public void Dispose()
        {
            // Todo:
        }
    }
}
