// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal readonly record struct BindUniformBlockEvent(ResourceReference Shader, ResourceReference Name, ResourceReference Buffer) : IRenderEvent
    {
        public RenderEventType Type => RenderEventType.BindUniformBlock;

        public static BindUniformBlockEvent Create(DeferredRenderer renderer, DeferredShader shader, string name, IDeferredUniformBuffer buffer)
        {
            return new BindUniformBlockEvent(renderer.Reference(shader), renderer.Reference(name), renderer.Reference(buffer));
        }
    }
}
