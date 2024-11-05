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

        private readonly List<IFocusEnvironment> activeEnvironments = new List<IFocusEnvironment>();
        private readonly List<FocusRequest> pendingRequests = new List<FocusRequest>();
        private int isBatchingUpdates;

        private void updateFocus()
        {
            using (BeginFocusUpdateBatch())
            {
                for (int i = activeEnvironments.Count - 1; i >= 0; i--)
                {
                    IFocusEnvironment env = activeEnvironments[i];
                    Debug.Assert(env.CurrentFocus != null);

                    if (!isDrawableValidForFocus(env.CurrentFocus))
                    {
                    }
                }
            }

            foreach (IFocusEnvironment env in activeEnvironments)
            {
                Debug.Assert(env.CurrentFocus != null);
            }
        }

        public void RequestFocus(Drawable target)
            => enqueueRequest(new FocusRequest(FocusRequestType.Request, CurrentState, target));

        public void AcquireFocus(Drawable target)
            => enqueueRequest(new FocusRequest(FocusRequestType.Acquire, CurrentState, target));

        public void ResignFocus(Drawable target)
            => enqueueRequest(new FocusRequest(FocusRequestType.Resign, CurrentState, target));

        internal FocusUpdateBatch BeginFocusUpdateBatch() => new FocusUpdateBatch(this);

        internal readonly ref struct FocusUpdateBatch
        {
            private readonly InputManager inputManager;

            public FocusUpdateBatch(InputManager inputManager)
            {
                this.inputManager = inputManager;
                inputManager.isBatchingUpdates++;
            }

            public void HandleClick(Drawable? drawable)
            {
                inputManager.pendingRequests.Insert(0,
                    drawable != null
                        ? new FocusRequest(FocusRequestType.Acquire, inputManager.CurrentState, drawable)
                        : new FocusRequest(FocusRequestType.Clear, inputManager.CurrentState, null));
            }

            public void Dispose()
            {
                inputManager.isBatchingUpdates--;
                inputManager.processPendingRequests();
            }
        }

        private void enqueueRequest(FocusRequest request)
        {
            pendingRequests.Add(request);
            processPendingRequests();
        }

        private void processPendingRequests()
        {
            Debug.Assert(isBatchingUpdates >= 0);
            if (isBatchingUpdates > 0)
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

        private bool isDrawableValidForFocus(Drawable drawable)
        {
            while (drawable != null)
            {
                if (!drawable.IsAlive || !drawable.IsPresent || drawable.Parent == null)
                    return false;

                if (drawable is IFocusEnvironment)
                    break;

                drawable = drawable.Parent;
            }

            return true;
        }

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
