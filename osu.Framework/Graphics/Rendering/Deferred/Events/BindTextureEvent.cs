// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    public readonly record struct BindTextureEvent(RendererResource Texture, int Unit, WrapMode? WrapModeS, WrapMode? WrapModeT) : IRenderEvent
    {
        public RenderEventType Type => RenderEventType.BindTexture;

        public void Run(DeferredRenderer current, IRenderer target)
        {
            target.BindTexture(Texture.Resolve<Texture>(current), Unit, WrapModeS, WrapModeT);
        }
    }
}
