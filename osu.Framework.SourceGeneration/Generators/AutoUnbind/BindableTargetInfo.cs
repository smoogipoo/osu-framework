// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.SourceGeneration.Generators.AutoUnbind
{
    public readonly struct BindableTargetInfo : IEquatable<BindableTargetInfo>
    {
        public readonly string FieldName;

        public BindableTargetInfo(string fieldName)
        {
            FieldName = fieldName;
        }

        public bool Equals(BindableTargetInfo other)
            => FieldName == other.FieldName;

        public override bool Equals(object? obj)
            => obj is BindableTargetInfo other && Equals(other);

        public override int GetHashCode()
            => FieldName.GetHashCode();
    }
}
