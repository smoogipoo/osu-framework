// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Input.States;

namespace osu.Framework.Input.Focus
{
    public class DeferredFocusUpdateContext : IFocusUpdateContext
    {
        private readonly List<FocusRequest> pendingRequests = new List<FocusRequest>();
        private readonly IFocusUpdateContext context;
        private readonly Action restoreContext;

        public DeferredFocusUpdateContext(IFocusUpdateContext context, Action restoreContext)
        {
            this.context = context;
            this.restoreContext = restoreContext;
        }

        public void HandleClick(InputState state, Drawable target)
            => pendingRequests.Insert(0, new FocusRequest(state, target, true));

        public void AcquireFocus(InputState state, Drawable target)
            => pendingRequests.Add(new FocusRequest(state, target, true));

        public void ReleaseFocus(InputState state, Drawable target)
            => pendingRequests.Add(new FocusRequest(state, target, false));

        public void Dispose()
        {
            restoreContext();

            foreach (var req in pendingRequests)
            {
                if (req.Acquire)
                    context.AcquireFocus(req.State, req.Target);
                else
                    context.ReleaseFocus(req.State, req.Target);
            }
        }

        private readonly record struct FocusRequest(InputState State, Drawable Target, bool Acquire);
    }
}
