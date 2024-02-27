// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct DrawNodeExitEvent(RenderEventType Type, ResourceReference DrawNode) : IRenderEvent
    {
        public static DrawNodeExitEvent Create(DeferredRenderer renderer, DrawNode drawNode)
            => new DrawNodeExitEvent(RenderEventType.DrawNodeExit, renderer.Context.Reference(drawNode));
    }
}
