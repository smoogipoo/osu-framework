// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Vertices;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    public readonly record struct AddVertexToBatchEvent(RendererResource VertexBatch, RendererMemoryBlock Data) : IRenderEvent
    {
        public RenderEventType Type => RenderEventType.AddVertexToBatch;

        public void Run(DeferredRenderer current, IRenderer target) => throw new NotSupportedException();

        public static AddVertexToBatchEvent Create<T>(DeferredRenderer renderer, IVertexBatch<T> batch, T vertex)
            where T : unmanaged, IEquatable<T>, IVertex
        {
            RendererMemoryBlock data = renderer.Allocate<T>();
            Span<byte> buffer = data.GetBuffer(renderer);

            MemoryMarshal.Write(buffer, vertex);

            return new AddVertexToBatchEvent(renderer.Reference(batch), data);
        }
    }
}
