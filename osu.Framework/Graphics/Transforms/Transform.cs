// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Transforms
{
    public abstract class Transform
    {
        internal ulong TransformID;

        /// <summary>
        /// Whether this <see cref="Transform"/> has been applied to an <see cref="ITransformable"/>.
        /// </summary>
        internal bool Applied;

        /// <summary>
        /// Whether this <see cref="Transform"/> has been applied completely to an <see cref="ITransformable"/>.
        /// Used to track whether we still need to apply for targets which allow rewind.
        /// </summary>
        internal bool AppliedToEnd;

        /// <summary>
        /// Whether this <see cref="Transform"/> can be rewound.
        /// </summary>
        public bool Rewindable = true;

        public abstract ITransformable TargetTransformable { get; }

        public double StartTime { get; internal set; }
        public double EndTime { get; internal set; }

        public bool IsLooping => LoopCount == -1 || LoopCount > 0;
        public double LoopDelay { get; internal set; }

        /// <summary>
        /// The remaining number of loops, including the current loop. If -1, then this loops indefinitely.
        /// </summary>
        public int LoopCount { get; internal set; }

        public abstract string TargetMember { get; }

        /// <summary>
        /// The name of the grouping this <see cref="Transform"/> belongs to.
        /// Defaults to <see cref="TargetMember"/>.
        /// </summary>
        /// <remarks>
        /// Transforms in a single group affect the same property (or properties) of a <see cref="Transformable"/>.
        /// It is assumed that transforms in different groups are independent from each other
        /// in that they affect different properties, and therefore they can be applied independently
        /// in any order without affecting the end result.
        /// </remarks>
        public virtual string TargetGrouping => TargetMember;

        public abstract void Apply(double time);

        public abstract void ReadIntoStartValue();

        internal bool HasStartValue;

        public Transform Clone() => (Transform)MemberwiseClone();

        public static readonly IComparer<Transform> COMPARER = new TransformTimeComparer();

        private class TransformTimeComparer : IComparer<Transform>
        {
            public int Compare(Transform x, Transform y)
            {
                ArgumentNullException.ThrowIfNull(x);
                ArgumentNullException.ThrowIfNull(y);

                int compare = x.StartTime.CompareTo(y.StartTime);
                if (compare != 0) return compare;

                compare = x.TransformID.CompareTo(y.TransformID);

                return compare;
            }
        }

        internal void TriggerComplete() => TransformSequenceCallbacks?.TransformCompleted();

        internal void TriggerAbort() => TransformSequenceCallbacks?.TransformAborted();

        internal Transform StartOfSequence;
        internal Transform NextInSequence;

        private TransformSequenceCallbacks transformSequenceCallbacks;
        internal TransformSequenceCallbacks TransformSequenceCallbacks => StartOfSequence.transformSequenceCallbacks ??= new TransformSequenceCallbacks(StartOfSequence);
    }

    public abstract class Transform<TValue> : Transform
    {
        public TValue StartValue { get; protected set; }
        public TValue EndValue { get; protected internal set; }
    }

    public abstract class Transform<TValue, TEasing, T> : Transform<TValue>
        where TEasing : IEasingFunction
        where T : class, ITransformable
    {
        public override ITransformable TargetTransformable => Target;

        public T Target { get; internal set; }

        public TEasing Easing { get; internal set; }

        public sealed override void Apply(double time)
        {
            Apply(Target, time);
            Applied = true;
        }

        public sealed override void ReadIntoStartValue() => ReadIntoStartValue(Target);

        protected abstract void Apply(T d, double time);

        protected abstract void ReadIntoStartValue(T d);

        public override string ToString() => $"{Target.GetType().Name}.{TargetMember} {StartTime:0.000}-{EndTime:0.000}ms {StartValue} -> {EndValue}";
    }

    public abstract class Transform<TValue, T> : Transform<TValue, DefaultEasingFunction, T>
        where T : class, ITransformable
    {
    }

    internal class TransformSequenceCallbacks
    {
        /// <summary>
        /// The transform at which the sequence ends. Set by <see cref="TransformSequence{T}"/>.
        /// </summary>
        public Transform End;

        /// <summary>
        /// Whether the sequence has been aborted.
        /// </summary>
        private bool aborted;

        /// <summary>
        /// Whether the sequence has completed.
        /// </summary>
        private bool completed;

        /// <summary>
        /// The transform at which the sequence starts. Set by <see cref="TransformSequence{T}"/>.
        /// </summary>
        private readonly Transform start;

        private Action<object> onAbort;
        private Action<object> onCompleted;

        public TransformSequenceCallbacks(Transform start)
        {
            this.start = start;
        }

        public void SetAbortCallback<T>(Action<T> callback)
        {
            if (onAbort != null)
                throw new InvalidOperationException("May not subscribe abort multiple times.");

            // No need to worry about new transforms immediately aborting, so
            // we can just subscribe here and be sure abort couldn't have been
            // triggered already.
            onAbort = o => callback((T)o);
        }

        public Action<object> ClearAbortCallback()
        {
            Action<object> tmpOnAbort = onAbort;
            onAbort = null;
            return tmpOnAbort;
        }

        public void SetCompletionCallback<T>(Action<T> callback)
        {
            if (onCompleted != null)
            {
                throw new InvalidOperationException(
                    "May not subscribe completion multiple times." +
                    $"This exception is also caused by calling {nameof(TransformSequence<Drawable>.Then)} or {nameof(TransformSequence<Drawable>.Finally)} on an infinitely looping {nameof(TransformSequence<Drawable>)}.");
            }

            onCompleted = o => callback((T)o);

            // Completion can be immediately triggered by instant transforms,
            // and therefore when subscribing we need to take into account
            // potential previous completions.
            if (completed)
                onCompleted?.Invoke(start.TargetTransformable);
        }

        public Action<object> ClearCompletionCallback()
        {
            Action<object> tmpOnCompleted = onCompleted;
            onCompleted = null;
            return tmpOnCompleted;
        }

        public void TransformAborted()
        {
            if (aborted || completed)
                return;

            aborted = true;

            Transform current = start;

            while (current != End)
            {
                if (!current.HasStartValue)
                    current.TargetTransformable.RemoveTransform(current);

                current = current.NextInSequence;
            }

            onAbort?.Invoke(start.TargetTransformable);
        }

        public void TransformCompleted()
        {
            if (aborted || completed)
                return;

            completed = true;

            onCompleted?.Invoke(start.TargetTransformable);
        }
    }
}
