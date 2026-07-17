// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace osu.Framework.SourceGeneration.Generators.AutoUnbind
{
    public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
    {
        private readonly IReadOnlyList<T> source;

        public EquatableArray(IReadOnlyList<T> source)
        {
            this.source = source;
        }

        public static implicit operator EquatableArray<T>(T[] array)
            => new EquatableArray<T>(array);

        public static implicit operator EquatableArray<T>(List<T> array)
            => new EquatableArray<T>(array);

        public static implicit operator EquatableArray<T>(ImmutableArray<T> array)
            => new EquatableArray<T>(array);

        public bool Equals(EquatableArray<T> other)
        {
            if (source.Count != other.source.Count)
                return false;

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;

            for (int i = 0; i < source.Count; i++)
            {
                if (!comparer.Equals(source[i], other.source[i]))
                    return false;
            }

            return true;
        }

        public override bool Equals(object? obj)
            => obj is EquatableArray<T> other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int result = 17;
                for (int i = 0; i < source.Count; i++)
                    result = result * 17 + (source[i]?.GetHashCode() ?? 0);
                return result;
            }
        }

        public int Count
            => source.Count;

        public T this[int index]
            => source[index];

        public IEnumerator<T> GetEnumerator()
            => source.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => source.GetEnumerator();
    }
}
