// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    public readonly record struct BindTextureEvent(Texture Texture, int Unit, WrapMode? WrapModeS, WrapMode? WrapModeT) : IEvent
    {
        public void Run(DeferredShader current, IRenderer target)
        {
            target.BindTexture(Texture, Unit, WrapModeS, WrapModeT);
        }
    }
}
