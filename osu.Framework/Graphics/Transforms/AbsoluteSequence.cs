// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Transforms
{
    /// <summary>
    /// User-facing stack-only object returned as a result of BeginAbsoluteSequence.
    /// </summary>
    public readonly ref struct AbsoluteSequence
    {
        private readonly TransformSequenceStateMachine.AbsoluteSequenceData? lastSequence;

        internal AbsoluteSequence(TransformSequenceStateMachine.AbsoluteSequenceData? lastSequence)
        {
            this.lastSequence = lastSequence;
        }

        public void Dispose()
        {
            TransformSequenceStateMachine.CurrentAbsoluteSequence.Value = lastSequence;
        }
    }
}
