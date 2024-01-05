// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Vertices;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    public readonly record struct PushQuadBatchEvent(RendererResource VertexBatch) : IRenderEvent
    {
        public RenderEventType Type => RenderEventType.PushQuadBatch;

        public void Run(DeferredRenderer current, IRenderer target)
        {
            target.PushQuadBatch(VertexBatch.Resolve<IVertexBatch<TexturedVertex2D>>(current));
        }
    }
}
