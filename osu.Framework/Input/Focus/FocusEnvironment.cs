// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;

namespace osu.Framework.Input.Focus
{
    public class FocusEnvironment : Container, IFocusEnvironment
    {
        public Drawable? CurrentFocus { get; private set; }

        void IFocusEnvironment.ChangeFocus(InputState state, Drawable? target)
        {
            if (CurrentFocus == target)
                return;

            HashSet<Drawable> newFocusSet = [..buildFocusSet(target)];
            HashSet<Drawable> oldFocusSet = [..buildFocusSet(CurrentFocus)];

            foreach (var d in buildFocusSet(CurrentFocus).Except(newFocusSet))
            {
                d.HasFocus = false;
                d.TriggerEvent(new FocusLostEvent(state, target));
            }

            foreach (var d in buildFocusSet(target).Reverse().Except(oldFocusSet))
            {
                d.HasFocus = true;
                d.TriggerEvent(new FocusEvent(state, CurrentFocus));
            }

            CurrentFocus = target;
        }

        private static IEnumerable<Drawable> buildFocusSet(Drawable? target)
        {
            Drawable? d = target;

            while (d != null)
            {
                if (d is Drawable obj)
                    yield return obj;

                if (d is IFocusEnvironment)
                    break;

                d = d.Parent;
            }
        }
    }
}
