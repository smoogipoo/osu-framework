// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;

namespace osu.Framework.Graphics.Transforms
{
    internal static class TransformSequenceStateMachine
    {
        public static ThreadLocal<AbsoluteSequenceData?> CurrentAbsoluteSequence = new ThreadLocal<AbsoluteSequenceData?>();
        public static ThreadLocal<DelayedSequenceData?> CurrentDelayedSequence = new ThreadLocal<DelayedSequenceData?>();

        public static AbsoluteSequence BeginAbsoluteSequence(object sender, double newTransformStartTime, bool recursive)
        {
            AbsoluteSequenceData? lastSequence = CurrentAbsoluteSequence.Value;

            double lastRecursiveStartTime = lastSequence?.RecursiveTransformStartTime ?? 0;
            double newRecursiveStartTime = recursive ? newTransformStartTime : lastRecursiveStartTime;

            CurrentAbsoluteSequence.Value = new AbsoluteSequenceData(sender, newTransformStartTime, newRecursiveStartTime);

            return new AbsoluteSequence(lastSequence);
        }

        public static DelayedSequence BeginDelayedSequence(object sender, double delay, bool recursive)
        {
            DelayedSequenceData? lastSequence = CurrentDelayedSequence.Value;

            double lastRecursiveDelay = lastSequence?.RecursiveDelay ?? 0;
            double newRecursiveDelay = recursive ? lastRecursiveDelay + delay : lastRecursiveDelay;

            CurrentDelayedSequence.Value = new DelayedSequenceData(sender, lastRecursiveDelay + delay, newRecursiveDelay);

            return new DelayedSequence(lastSequence);
        }

        public static double GetTransformStartTime(Transformable sender)
        {
            if (!(CurrentAbsoluteSequence.Value is AbsoluteSequenceData sequence))
                return 0;

            return (sender == sequence.Sender ? sequence.TransformStartTime : sequence.RecursiveTransformStartTime) - (sender.Clock?.CurrentTime ?? 0);
        }

        public static double GetTransformDelay(ITransformable sender)
        {
            if (!(CurrentDelayedSequence.Value is DelayedSequenceData sequence))
                return 0;

            return sender == sequence.Sender ? sequence.Delay : sequence.RecursiveDelay;
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
