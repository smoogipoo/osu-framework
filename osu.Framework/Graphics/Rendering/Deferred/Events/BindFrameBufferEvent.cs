// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    public readonly record struct BindFrameBufferEvent(RendererResource FrameBuffer) : IRenderEvent
    {
        public RenderEventType Type => RenderEventType.BindFrameBuffer;
    }
}
