// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Input.States;

namespace osu.Framework.Input.Focus
{
    public class ImmediateFocusUpdateContext : IFocusUpdateContext
    {
        public required Action<InputState, Drawable> Acquire { get; init; }
        public required Action<InputState, Drawable> Release { get; init; }

        public void HandleClick(InputState state, Drawable target)
            => Acquire(state, target);

        public void AcquireFocus(InputState state, Drawable target)
            => Acquire(state, target);

        public void ReleaseFocus(InputState state, Drawable target)
            => Release(state, target);

        public void Dispose()
        {
        }
    }
}
