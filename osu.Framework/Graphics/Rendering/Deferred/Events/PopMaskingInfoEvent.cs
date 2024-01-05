// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    public readonly record struct PopMaskingInfoEvent : IRenderEvent
    {
        public RenderEventType Type => RenderEventType.PopMaskingInfo;

        public void Run(DeferredRenderer current, IRenderer target)
        {
            target.PopMaskingInfo();
        }
    }
}
