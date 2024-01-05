// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    public readonly record struct BindUniformBlockEvent(RendererResource Shader, RendererResource Name, RendererResource Buffer) : IRenderEvent
    {
        public RenderEventType Type => RenderEventType.BindUniformBlock;

        public void Run(DeferredRenderer current, IRenderer target)
        {
            Shader.Resolve<DeferredShader>(current).Resource.BindUniformBlock(
                Name.Resolve<string>(current),
                Buffer.Resolve<IUniformBuffer>(current));
        }
    }
}
