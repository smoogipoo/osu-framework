// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    public readonly record struct PushScissorOffsetEvent(Vector2I Offset) : IEvent
    {
        public void Run(DeferredRenderer current, IRenderer target)
        {
            target.PushScissorOffset(Offset);
        }
    }
}
