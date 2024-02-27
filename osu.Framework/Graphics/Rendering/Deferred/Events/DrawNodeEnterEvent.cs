// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct DrawNodeEnterEvent(RenderEventType Type, ResourceReference DrawNode, DrawNodeEnterMetadata Metadata) : IRenderEvent
    {
        public static DrawNodeEnterEvent Create(DeferredRenderer renderer, DrawNode drawNode)
            => new DrawNodeEnterEvent(RenderEventType.DrawNodeEnter, renderer.Context.Reference(drawNode), default);

        public static DrawNodeEnterEvent Create(DeferredRenderer renderer, DrawNode drawNode, DrawNodeEnterMetadata metadata)
            => new DrawNodeEnterEvent(RenderEventType.DrawNodeEnter, renderer.Context.Reference(drawNode), metadata);
    }

    internal readonly record struct DrawNodeEnterMetadata(bool HasFrameBuffer);
}
