// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    public readonly record struct PushProjectionMatrixEvent(Matrix4 Matrix) : IEvent;
}
