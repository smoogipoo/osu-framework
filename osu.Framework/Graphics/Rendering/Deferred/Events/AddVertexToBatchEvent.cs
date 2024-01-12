// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Vertices;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct AddVertexToBatchEvent(RendererResource VertexBatch, RendererStagingMemoryBlock Memory) : IRenderEvent
    {
        public RenderEventType Type => RenderEventType.AddVertexToBatch;

        public static AddVertexToBatchEvent Create<T>(DeferredRenderer renderer, DeferredVertexBatch<T> batch, T vertex)
            where T : unmanaged, IVertex, IEquatable<T>
        {
            return new AddVertexToBatchEvent(renderer.Reference(batch), renderer.AllocateStaging(vertex));
        }
    }
}
