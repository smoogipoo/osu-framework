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
                        env.ChangeFocus(CurrentState, null);
                }

                activeEnvironments.Clear();
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

        public void ResignFocusImmediately(Drawable target)
        {
            Debug.Assert(target.HasFocus);
            resign(CurrentState, getEnvironment(target)!.CurrentFocus!);
        }

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

                    FirstResponder = target;
                }

                IEnumerable<Drawable> lastFocusSet = [];
                IEnumerable<Drawable> nextFocusSet = [];

                if (target != environment.CurrentFocus)
                {
                    lastFocusSet = enumerateFocusSet(environment.CurrentFocus);
                    nextFocusSet = enumerateFocusSet(target);

                    foreach (var drawable in lastFocusSet)
                    {
                        drawable.HadFocus = drawable.HasFocus;
                        drawable.HasFocus = false;
                    }

                    foreach (var drawable in nextFocusSet)
                    {
                        drawable.HadFocus = drawable.HasFocus;
                        drawable.HasFocus = true;
                    }

                    environment.CurrentFocus = target;
                }

                // 1. Resign old responder.
                lastFirstResponder?.TriggerEvent(new ResignFirstResponderEvent(state, nextFirstResponder));

                // 2. Resign focus on old drawables.
                foreach (var drawable in lastFocusSet)
                {
                    if (drawable.HadFocus && !drawable.HasFocus)
                        drawable.TriggerEvent(new FocusLostEvent(state, target));
                }

                // 3. Acquire focus on new drawables.
                foreach (var drawable in nextFocusSet.Reverse())
                {
                    if (!drawable.HadFocus && drawable.HadFocus)
                        drawable.TriggerEvent(new FocusLostEvent(state, target));
                }

                // 4. Acquire new responder.
                nextFirstResponder?.TriggerEvent(new BecomeFirstResponderEvent(state, lastFirstResponder));
            }
        }

        private void resign(InputState state, Drawable target)
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

        /// <summary>
        /// Enumerates self and all parenting drawables from a target drawable until the first focus environment is found.
        /// When the target drawable's focus is changed, the returned set contains all targets that must receive focus change events
        /// in order from target to environment.
        /// </summary>
        /// <param name="target">The drawable whose focus state is changed.</param>
        /// <returns>The set of all drawables that must receive focus change events in order from target to environment.</returns>
        private static IEnumerable<Drawable> enumerateFocusSet(Drawable? target)
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
