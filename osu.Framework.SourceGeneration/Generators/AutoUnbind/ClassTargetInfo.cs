// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.SourceGeneration.Generators.AutoUnbind
{
    public readonly struct ClassTargetInfo : IEquatable<ClassTargetInfo>
    {
        public readonly string TypeName;
        public readonly EquatableArray<BindableTargetInfo> BindableFields;

        public ClassTargetInfo(string typeName, EquatableArray<BindableTargetInfo> bindableFields)
        {
            TypeName = typeName;
            BindableFields = bindableFields;
        }

        public bool Equals(ClassTargetInfo other)
            => TypeName == other.TypeName && BindableFields.Equals(other.BindableFields);

        public override bool Equals(object? obj)
            => obj is ClassTargetInfo other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (TypeName.GetHashCode() * 397) ^ BindableFields.GetHashCode();
            }
        }
    }
}
