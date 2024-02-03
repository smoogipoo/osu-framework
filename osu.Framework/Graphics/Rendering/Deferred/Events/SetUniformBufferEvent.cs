// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct SetUniformBufferEvent(ResourceReference Name, ResourceReference Buffer) : IRenderEvent
    {
        public RenderEventType Type => RenderEventType.SetUniformBuffer;

        public static SetUniformBufferEvent Create(DeferredRenderer renderer, string name, IUniformBuffer buffer)
        {
            return new SetUniformBufferEvent(renderer.Reference(name), renderer.Reference(buffer));
        }
    }
}
