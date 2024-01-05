// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    public readonly record struct DisposalEvent(RendererResource Target, RendererResource DisposalAction) : IRenderEvent
    {
        public RenderEventType Type => RenderEventType.Disposal;

        public void Run(DeferredRenderer current, IRenderer target)
        {
            target.ScheduleDisposal(DisposalAction.Resolve<Action<object>>(current), Target.Resolve<object>(current));
        }

        public static DisposalEvent Create<T>(DeferredRenderer renderer, T target, Action<T> action)
            where T : class
        {
            return new DisposalEvent(renderer.Reference(target), renderer.Reference(trampoline));

            void trampoline(object o) => action((T)o);
        }
    }
}
