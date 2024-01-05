// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    public readonly record struct PushProjectionMatrixEvent(Matrix4 Matrix) : IRenderEvent
    {
        public RenderEventType Type => RenderEventType.PushProjectionMatrix;

        public void Run(DeferredRenderer current, IRenderer target)
        {
            target.PushProjectionMatrix(Matrix);
        }
    }
}
