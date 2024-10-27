// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    public class ResignFirstResponderEvent : UIEvent
    {
        public readonly Drawable? NextFirstResponder;

        public ResignFirstResponderEvent(InputState state, Drawable? nextFirstResponder)
            : base(state)
        {
            NextFirstResponder = nextFirstResponder;
        }
    }
}
