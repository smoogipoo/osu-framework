// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.Generators
{
    public class IncrementalSyntaxTarget : IEquatable<IncrementalSyntaxTarget>
    {
        public ClassDeclarationSyntax Syntax { get; }
        public string FullyQualifiedName { get; }

        public IncrementalSyntaxTarget(ClassDeclarationSyntax syntax)
        {
            Syntax = syntax;
            FullyQualifiedName = SyntaxHelpers.GetFullyQualifiedSyntaxName(syntax);
        }

        public bool Equals(IncrementalSyntaxTarget? other)
            => other != null && FullyQualifiedName == other.FullyQualifiedName;

        public override bool Equals(object? obj)
            => obj is IncrementalSyntaxTarget other && Equals(other);

        public override int GetHashCode()
            => FullyQualifiedName.GetHashCode();
    }
}
