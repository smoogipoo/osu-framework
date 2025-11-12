// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.Generators
{
    public class IncrementalSyntaxTarget : IEquatable<IncrementalSyntaxTarget>
    {
        public readonly ClassDeclarationSyntax Syntax;
        private readonly string syntaxKey;

        public IncrementalSyntaxTarget(ClassDeclarationSyntax syntax)
        {
            Syntax = syntax;
            syntaxKey = $"{syntax.SyntaxTree.FilePath}|{syntax.SpanStart}";
        }

        public bool Equals(IncrementalSyntaxTarget? other) =>
            other != null && syntaxKey == other.syntaxKey;

        public override bool Equals(object? obj)
            => obj is IncrementalSyntaxTarget other && Equals(other);

        public override int GetHashCode()
            => syntaxKey.GetHashCode();
    }
}
