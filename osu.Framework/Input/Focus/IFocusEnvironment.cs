// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
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
        Drawable? CurrentFocus { get; internal set; }

        internal void ChangeFocus(InputState state, Drawable? target)
        {
            if (CurrentFocus == target)
                return;

            HashSet<Drawable> newFocusSet = [..BuildFocusSet(target)];
            HashSet<Drawable> oldFocusSet = [..BuildFocusSet(CurrentFocus)];

            foreach (var d in BuildFocusSet(CurrentFocus).Except(newFocusSet))
            {
                d.HasFocus = false;
                d.TriggerEvent(new FocusLostEvent(state, target));
            }

            foreach (var d in BuildFocusSet(target).Reverse().Except(oldFocusSet))
            {
                d.HasFocus = true;
                d.TriggerEvent(new FocusEvent(state, CurrentFocus));
            }

            CurrentFocus = target;
        }
    }
}
