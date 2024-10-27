// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;

namespace osu.Framework.Input.Focus
{
    /// <summary>
    /// Manages focus and captures the first input responder.
    /// </summary>
    public interface IFocusSystem
    {
        /// <summary>
        /// The drawable that will be the first target for keyboard input.
        /// </summary>
        Drawable? FirstResponder { get; }

        /// <summary>
        /// Requests a drawable to be focused.
        /// </summary>
        /// <param name="target">The drawable.</param>
        void AcquireFocus(Drawable target);

        /// <summary>
        /// Requests focus to be removed from a given drawable.
        /// </summary>
        /// <param name="target">The drawable.</param>
        void ReleaseFocus(Drawable target);

        IFocusUpdateContext BeginFocusUpdate();
    }
}
