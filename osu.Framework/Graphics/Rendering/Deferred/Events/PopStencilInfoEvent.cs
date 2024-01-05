// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    public readonly record struct PopStencilInfoEvent : IRenderEvent
    {
        public RenderEventType Type => RenderEventType.PopStencilInfo;

        public void Run(DeferredRenderer current, IRenderer target)
        {
            target.PopStencilInfo();
        }
    }
}
