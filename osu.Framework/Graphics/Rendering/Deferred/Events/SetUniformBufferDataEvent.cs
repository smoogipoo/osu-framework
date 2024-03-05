// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct SetUniformBufferDataEventOverlay(RenderEventType Type, ResourceReference Buffer, UniformBufferReference Reference) : IRenderEvent
    {
        public static SetUniformBufferDataEventOverlay Create(DeferredRenderer renderer, IDeferredUniformBuffer uniformBuffer)
            => new SetUniformBufferDataEventOverlay(RenderEventType.SetUniformBufferData, renderer.Context.Reference(uniformBuffer), default);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly record struct SetUniformBufferDataEvent<T>(SetUniformBufferDataEventOverlay Overlay, T Data) : IRenderEvent
        where T : unmanaged
    {
        public RenderEventType Type => Overlay.Type;

        public static SetUniformBufferDataEvent<T> Create(DeferredRenderer renderer, IDeferredUniformBuffer uniformBuffer, T data)
            => new SetUniformBufferDataEvent<T>(SetUniformBufferDataEventOverlay.Create(renderer, uniformBuffer), data);
    }
}
