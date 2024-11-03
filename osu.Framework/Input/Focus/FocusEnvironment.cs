// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Input.Focus
{
    public class FocusEnvironment : Container, IFocusEnvironment
    {
        public Drawable? CurrentFocus { get; private set; }

        Drawable? IFocusEnvironment.CurrentFocus
        {
            get => CurrentFocus;
            set => CurrentFocus = value;
        }
    }
}
