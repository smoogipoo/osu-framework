// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Transforms
{
    /// <summary>
    /// User-facing stack-only object returned as a result of BeginAbsoluteSequence.
    /// </summary>
    public readonly ref struct AbsoluteSequence
    {
        private readonly TransformSequenceStateMachine.AbsoluteSequenceData? lastAbsoluteSequence;
        private readonly TransformSequenceStateMachine.DelayedSequenceData? lastDelayedSequence;

        internal AbsoluteSequence(TransformSequenceStateMachine.AbsoluteSequenceData? lastAbsoluteSequence, TransformSequenceStateMachine.DelayedSequenceData? lastDelayedSequence)
        {
            this.lastAbsoluteSequence = lastAbsoluteSequence;
            this.lastDelayedSequence = lastDelayedSequence;
        }

        public void Dispose()
        {
            TransformSequenceStateMachine.CurrentAbsoluteSequence = lastAbsoluteSequence;
            TransformSequenceStateMachine.CurrentDelayedSequence = lastDelayedSequence;
        }
    }
}
