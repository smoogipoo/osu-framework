// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Threading;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    public readonly record struct ExpensiveOperationEvent(RendererResource Operation) : IRenderEvent
    {
        public RenderEventType Type => RenderEventType.ExpensiveOperation;

        public void Run(DeferredRenderer current, IRenderer target)
        {
            target.ScheduleExpensiveOperation(Operation.Resolve<ScheduledDelegate>(current));
        }
    }
}
