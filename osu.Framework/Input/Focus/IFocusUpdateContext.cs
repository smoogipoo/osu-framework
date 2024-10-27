// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Input.States;

namespace osu.Framework.Input.Focus
{
    public interface IFocusUpdateContext : IDisposable
    {
        void HandleClick(InputState state, Drawable target);

        void AcquireFocus(InputState state, Drawable target);

        void ReleaseFocus(InputState state, Drawable target);
    }
}
