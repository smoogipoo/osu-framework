// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Transforms
{
    internal static class TransformSequenceStateMachine
    {
        [ThreadStatic]
        public static AbsoluteSequenceData? CurrentAbsoluteSequence;

        [ThreadStatic]
        public static DelayedSequenceData? CurrentDelayedSequence;

        public static AbsoluteSequence BeginAbsoluteSequence(object sender, double newTransformStartTime, bool recursive)
        {
            AbsoluteSequenceData? lastSequence = CurrentAbsoluteSequence;

            double lastRecursiveStartTime = lastSequence?.RecursiveTransformStartTime ?? 0;
            double newRecursiveStartTime = recursive ? newTransformStartTime : lastRecursiveStartTime;

            CurrentAbsoluteSequence = new AbsoluteSequenceData(sender, newTransformStartTime, newRecursiveStartTime);

            return new AbsoluteSequence(lastSequence);
        }

        public static DelayedSequence BeginDelayedSequence(object sender, double delay, bool recursive)
        {
            DelayedSequenceData? lastSequence = CurrentDelayedSequence;

            double lastRecursiveDelay = lastSequence?.RecursiveDelay ?? 0;
            double newRecursiveDelay = recursive ? lastRecursiveDelay + delay : lastRecursiveDelay;

            CurrentDelayedSequence = new DelayedSequenceData(sender, lastRecursiveDelay + delay, newRecursiveDelay);

            return new DelayedSequence(lastSequence);
        }

        public static double GetTransformDelay(Transformable sender)
        {
            double delay = 0;

            if (CurrentDelayedSequence is DelayedSequenceData delayedSequence)
                delay += sender == delayedSequence.Sender ? delayedSequence.Delay : delayedSequence.RecursiveDelay;

            if (CurrentAbsoluteSequence is AbsoluteSequenceData absoluteSequence)
            {
                delay += sender == absoluteSequence.Sender ? absoluteSequence.TransformStartTime : absoluteSequence.RecursiveTransformStartTime;
                delay -= sender.Clock?.CurrentTime ?? 0;
            }

            return delay;
        }

        /// <summary>
        /// Describes the current absolute sequence.
        /// </summary>
        public readonly struct AbsoluteSequenceData
        {
            public readonly object Sender;
            public readonly double TransformStartTime;
            public readonly double RecursiveTransformStartTime;

            public AbsoluteSequenceData(object sender, double transformStartTime, double recursiveTransformStartTime)
            {
                Sender = sender;
                TransformStartTime = transformStartTime;
                RecursiveTransformStartTime = recursiveTransformStartTime;
            }
        }

        /// <summary>
        /// Describes the current delayed sequence.
        /// </summary>
        public readonly struct DelayedSequenceData
        {
            public readonly object Sender;
            public readonly double Delay;
            public readonly double RecursiveDelay;

            public DelayedSequenceData(object sender, double delay, double recursiveDelay)
            {
                Sender = sender;
                Delay = delay;
                RecursiveDelay = recursiveDelay;
            }
        }
    }
}
