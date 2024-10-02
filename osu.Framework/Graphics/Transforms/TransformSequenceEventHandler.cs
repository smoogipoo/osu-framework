// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Transforms
{
    public class TransformSequenceEventHandler : Transform
    {
        public const string GROUP = "transform-sequence-event-handler";

        public event Action? OnAbort;
        public event Action? OnComplete;

        public override ITransformable TargetTransformable { get; }

        public override string TargetMember => SequenceID.ToString()!;
        public override string TargetGrouping => GROUP;

        public TransformSequenceEventHandler(ITransformable target, Guid sequenceId)
        {
            TargetTransformable = target;
            SequenceID = sequenceId;
        }

        public void TriggerAbort() => OnAbort?.Invoke();

        public void TriggerComplete() => OnComplete?.Invoke();

        public override void Apply(double time)
        {
        }

        public override void ReadIntoStartValue()
        {
        }
    }
}
