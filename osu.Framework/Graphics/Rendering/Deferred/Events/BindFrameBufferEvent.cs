// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    public readonly record struct BindFrameBufferEvent(DeferredFrameBuffer FrameBuffer) : IEvent
    {
        public void Run(DeferredShader current, IRenderer target)
        {
            FrameBuffer.Resource.Bind();
        }
    }
}
