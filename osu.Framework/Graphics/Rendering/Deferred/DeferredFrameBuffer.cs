// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    public class DeferredFrameBuffer : IFrameBuffer
    {
        private readonly DeferredRenderer renderer;
        private readonly IFrameBuffer frameBuffer;

        public DeferredFrameBuffer(DeferredRenderer renderer, IFrameBuffer frameBuffer)
        {
            this.renderer = renderer;
            this.frameBuffer = frameBuffer;
        }

        public Texture Texture => frameBuffer.Texture;

        public Vector2 Size
        {
            get => frameBuffer.Size;
            set => frameBuffer.Size = value;
        }

        public void Bind() => renderer.RenderEvents.Add(new BindFrameBufferEvent(frameBuffer));

        public void Unbind() => renderer.RenderEvents.Add(new UnbindFrameBufferEvent(frameBuffer));

        public void Dispose()
        {
            // Todo:
        }
    }
}
