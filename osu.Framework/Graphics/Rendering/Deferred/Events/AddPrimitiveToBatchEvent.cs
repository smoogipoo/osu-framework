// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Vertices;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct AddPrimitiveToBatchEventOverlay(RenderEventType Type, ResourceReference VertexBatch) : IRenderEvent
    {
        public static AddPrimitiveToBatchEventOverlay Create(DeferredRenderer renderer, IDeferredVertexBatch batch)
            => new AddPrimitiveToBatchEventOverlay(RenderEventType.AddPrimitiveToBatch, renderer.Context.Reference(batch));
    }

    internal static class AddPrimitiveToBatchEvent<T>
        where T : unmanaged, IVertex, IEquatable<T>
    {
        public static Event<PrimitiveBuffer1> Create1(DeferredRenderer renderer, IDeferredVertexBatch batch, ReadOnlySpan<T> vertices)
            => new Event<PrimitiveBuffer1>(AddPrimitiveToBatchEventOverlay.Create(renderer, batch), PrimitiveBuffer1.Create(ref MemoryMarshal.GetReference(vertices)));

        public static Event<PrimitiveBuffer2> Create2(DeferredRenderer renderer, IDeferredVertexBatch batch, ReadOnlySpan<T> vertices)
            => new Event<PrimitiveBuffer2>(AddPrimitiveToBatchEventOverlay.Create(renderer, batch), PrimitiveBuffer2.Create(ref MemoryMarshal.GetReference(vertices)));

        public static Event<PrimitiveBuffer3> Create3(DeferredRenderer renderer, IDeferredVertexBatch batch, ReadOnlySpan<T> vertices)
            => new Event<PrimitiveBuffer3>(AddPrimitiveToBatchEventOverlay.Create(renderer, batch), PrimitiveBuffer3.Create(ref MemoryMarshal.GetReference(vertices)));

        public static Event<PrimitiveBuffer4> Create4(DeferredRenderer renderer, IDeferredVertexBatch batch, ReadOnlySpan<T> vertices)
            => new Event<PrimitiveBuffer4>(AddPrimitiveToBatchEventOverlay.Create(renderer, batch), PrimitiveBuffer4.Create(ref MemoryMarshal.GetReference(vertices)));

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal readonly record struct Event<TPrimitiveBuffer>(AddPrimitiveToBatchEventOverlay Overlay, TPrimitiveBuffer Primitive) : IRenderEvent
            where TPrimitiveBuffer : unmanaged, IPrimitiveBuffer
        {
            public RenderEventType Type => Overlay.Type;
        }

        internal interface IPrimitiveBuffer;

        [InlineArray(4)]
        internal struct PrimitiveBuffer1 : IPrimitiveBuffer
        {
            private T start;

            public static ref PrimitiveBuffer1 Create(ref T source) => ref Unsafe.As<T, PrimitiveBuffer1>(ref source);
        }

        [InlineArray(4)]
        internal struct PrimitiveBuffer2 : IPrimitiveBuffer
        {
            private T start;

            public static ref PrimitiveBuffer2 Create(ref T source) => ref Unsafe.As<T, PrimitiveBuffer2>(ref source);
        }

        [InlineArray(4)]
        internal struct PrimitiveBuffer3 : IPrimitiveBuffer
        {
            private T start;

            public static ref PrimitiveBuffer3 Create(ref T source) => ref Unsafe.As<T, PrimitiveBuffer3>(ref source);
        }

        [InlineArray(4)]
        internal struct PrimitiveBuffer4 : IPrimitiveBuffer
        {
            private T start;

            public static ref PrimitiveBuffer4 Create(ref T source) => ref Unsafe.As<T, PrimitiveBuffer4>(ref source);
        }
    }
}
