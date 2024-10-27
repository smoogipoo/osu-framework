// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;
using osu.Framework.Testing;
using osuTK;

namespace osu.Framework.Input.Focus
{
    public class FocusSystem : FocusEnvironment, IFocusSystem, IFocusManager
    {
        public Drawable? FirstResponder { get; private set; }

        private IFocusUpdateContext context;

        public FocusSystem()
        {
            context = new ImmediateFocusUpdateContext
            {
                Acquire = acquireFocus,
                Release = releaseFocus
            };
        }

        protected override bool OnClick(ClickEvent e)
        {
            while (FirstResponder != null)
            {
                // Release focus immediately -- we are in a deferred context at this time.
                Debug.Assert(context is DeferredFocusUpdateContext);
                releaseFocus(GetContainingInputManager()?.CurrentState ?? new InputState(), FirstResponder);
            }

            return false;
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true;

        internal override bool BuildNonPositionalInputQueue(List<Drawable> queue, bool allowBlocking = true)
        {
            bool result = base.BuildNonPositionalInputQueue(queue, allowBlocking);

            if (FirstResponder != null)
            {
                queue.Remove(FirstResponder);
                queue.Add(FirstResponder);
            }

            return result;
        }

        public void AcquireFocus(Drawable target)
            => context.AcquireFocus(GetContainingInputManager()?.CurrentState ?? new InputState(), target);

        public void ReleaseFocus(Drawable target)
            => context.ReleaseFocus(GetContainingInputManager()?.CurrentState ?? new InputState(), target);

        public IFocusUpdateContext BeginFocusUpdate()
        {
            IFocusUpdateContext currentContext = context;
            return context = new DeferredFocusUpdateContext(context, () => context = currentContext);
        }

        private void acquireFocus(InputState state, Drawable target)
        {
            IFocusEnvironment environment = target.FindClosestParent<IFocusEnvironment>()!;

            Drawable? lastFirstResponder = null;
            Drawable? nextFirstResponder = null;

            if (FirstResponder != target)
            {
                lastFirstResponder = FirstResponder;
                nextFirstResponder = target;
            }

            lastFirstResponder?.TriggerEvent(new ResignFirstResponderEvent(state, nextFirstResponder));
            environment.ChangeFocus(state, target);
            nextFirstResponder?.TriggerEvent(new BecomeFirstResponderEvent(state, lastFirstResponder));

            if (lastFirstResponder != nextFirstResponder)
                FirstResponder = nextFirstResponder;
        }

        private void releaseFocus(InputState state, Drawable target)
        {
            IFocusEnvironment environment = target.FindClosestParent<IFocusEnvironment>()!;

            if (environment.CurrentFocus != target)
                return;

            Drawable? lastFirstResponder = null;
            Drawable? nextFirstResponder = null;

            if (FirstResponder == target)
            {
                lastFirstResponder = FirstResponder;
                nextFirstResponder = this.ChildrenOfType<IFocusEnvironment>().Select(e => e.CurrentFocus).FirstOrDefault(d => d != null && d != lastFirstResponder);
            }

            lastFirstResponder?.TriggerEvent(new ResignFirstResponderEvent(state, nextFirstResponder));
            environment.ChangeFocus(state, null);
            nextFirstResponder?.TriggerEvent(new BecomeFirstResponderEvent(state, lastFirstResponder));

            if (lastFirstResponder != nextFirstResponder)
                FirstResponder = nextFirstResponder;
        }

        void IFocusManager.TriggerFocusContention(Drawable? triggerSource)
        {
        }

        bool IFocusManager.ChangeFocus(Drawable? potentialFocusTarget)
        {
            if (potentialFocusTarget == null)
            {
                if (CurrentFocus != null)
                    ReleaseFocus(CurrentFocus);
            }
            else
                AcquireFocus(potentialFocusTarget);

            return CurrentFocus == potentialFocusTarget;
        }
    }
}
