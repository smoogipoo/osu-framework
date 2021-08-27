// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Transforms
{
    public readonly ref struct DelayedSequence
    {
        private readonly TransformSequenceStateMachine.DelayedSequenceData? lastSequence;

        internal DelayedSequence(TransformSequenceStateMachine.DelayedSequenceData? lastSequence)
        {
            this.lastSequence = lastSequence;
        }

        public void Dispose()
        {
            TransformSequenceStateMachine.CurrentDelayedSequence = lastSequence;
        }
    }
}
