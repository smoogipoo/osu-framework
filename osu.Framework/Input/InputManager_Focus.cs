// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Input.Focus;
using osu.Framework.Input.States;
using osu.Framework.Testing;

namespace osu.Framework.Input
{
    public partial class InputManager : IFocusSystem, IFocusManager
    {
        public Drawable? FirstResponder { get; private set; }

        private readonly List<FocusRequest> pendingRequests = new List<FocusRequest>();
        private int isHandlingClick;

        public void RequestFocus(Drawable target)
            => enqueueRequest(new FocusRequest(FocusRequestType.Request, CurrentState, target));

        public void AcquireFocus(Drawable target)
            => enqueueRequest(new FocusRequest(FocusRequestType.Acquire, CurrentState, target));

        public void ResignFocus(Drawable target)
            => enqueueRequest(new FocusRequest(FocusRequestType.Resign, CurrentState, target));

        public void BeginFocusUpdate() => isHandlingClick++;

        public void EndFocusUpdate(Drawable? handled)
        {
            Debug.Assert(isHandlingClick >= 1);

            pendingRequests.Insert(0,
                handled != null
                    ? new FocusRequest(FocusRequestType.Acquire, CurrentState, handled)
                    : new FocusRequest(FocusRequestType.Clear, CurrentState, null));

            isHandlingClick--;
            processPendingRequests();
        }

        private void enqueueRequest(FocusRequest request)
        {
            pendingRequests.Add(request);
            processPendingRequests();
        }

        private void processPendingRequests()
        {
            Debug.Assert(isHandlingClick >= 0);
            if (isHandlingClick > 0)
                return;

            for (int i = 0; i < pendingRequests.Count; i++)
            {
                var req = pendingRequests[i];

                switch (req.Type)
                {
                    case FocusRequestType.Request:
                        Debug.Assert(req.Target != null);
                        if (getEnvironment(req.Target)?.CurrentFocus == null)
                            acquire(req.State, req.Target);
                        break;

                    case FocusRequestType.Acquire:
                        Debug.Assert(req.Target != null);
                        acquire(req.State, req.Target);
                        break;

                    case FocusRequestType.Resign:
                        Debug.Assert(req.Target != null);
                        resign(req.State, req.Target);
                        break;

                    case FocusRequestType.Clear:
                        while (CurrentFocus != null)
                            resign(req.State, CurrentFocus);
                        break;
                }
            }

            pendingRequests.Clear();

            void acquire(InputState state, Drawable target)
            {
                if (getEnvironment(target) is not IFocusEnvironment environment)
                    return;

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

            void resign(InputState state, Drawable target)
            {
                if (getEnvironment(target) is not IFocusEnvironment environment)
                    return;

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
        }

        private IFocusEnvironment? getEnvironment(Drawable? drawable)
            => drawable?.FindClosestParentOrSelf<IFocusEnvironment>();

        void IFocusManager.TriggerFocusContention(Drawable? triggerSource)
        {
        }

        bool IFocusManager.ChangeFocus(Drawable? potentialFocusTarget)
        {
            if (potentialFocusTarget == null)
            {
                if (CurrentFocus != null)
                    ResignFocus(CurrentFocus);
            }
            else
                AcquireFocus(potentialFocusTarget);

            return CurrentFocus == potentialFocusTarget;
        }
    }
}
