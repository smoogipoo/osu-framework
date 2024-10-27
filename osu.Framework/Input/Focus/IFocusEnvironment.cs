// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Input.States;

namespace osu.Framework.Input.Focus
{
    /// <summary>
    /// Captures the focus state of a hierarchy.
    /// </summary>
    public interface IFocusEnvironment : IDrawable
    {
        /// <summary>
        /// The immediate focused drawable within this environment.
        /// </summary>
        Drawable? CurrentFocus { get; }

        void ChangeFocus(InputState state, Drawable? target);
    }
}
