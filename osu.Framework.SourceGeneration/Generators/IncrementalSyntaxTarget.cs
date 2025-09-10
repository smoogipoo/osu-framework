// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.Generators
{
    public class IncrementalSyntaxTarget : IEquatable<IncrementalSyntaxTarget>
    {
        public readonly GeneratorSyntaxContext Context;
        public readonly OptimizationLevel OptimisationLevel;
        public readonly SyntaxNode Node;

        public string? SyntaxName { get; private set; }
        public IncrementalSemanticTarget? SemanticTarget { get; private set; }

        public long? GenerationId;

        public IncrementalSyntaxTarget(GeneratorSyntaxContext context)
        {
            Context = context;
            OptimisationLevel = context.SemanticModel.Compilation.Options.OptimizationLevel;
            Node = context.Node;
        }

        public IncrementalSyntaxTarget WithSemanticInformation(Func<ClassDeclarationSyntax, SemanticModel, IncrementalSemanticTarget> createTarget)
        {
            ClassDeclarationSyntax classSyntax = (ClassDeclarationSyntax)Context.Node;

            return new IncrementalSyntaxTarget(Context)
            {
                SyntaxName = SyntaxHelpers.GetFullyQualifiedSyntaxName(classSyntax),
                SemanticTarget = createTarget(classSyntax, Context.SemanticModel)
            };
        }

        public bool Equals(IncrementalSyntaxTarget? other)
            => other != null && SyntaxFactory.AreEquivalent(Node, other.Node);

        public override bool Equals(object? obj)
            => obj is IncrementalSyntaxTarget other && Equals(other);

        public override int GetHashCode()
            => Node.GetHashCode();

        public class SyntaxNameComparer : IEqualityComparer<IncrementalSyntaxTarget>
        {
            public static readonly SyntaxNameComparer DEFAULT = new SyntaxNameComparer();

            public bool Equals(IncrementalSyntaxTarget? x, IncrementalSyntaxTarget? y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;

                return x.SyntaxName == y.SyntaxName;
            }

            public int GetHashCode(IncrementalSyntaxTarget obj)
            {
                return obj.SyntaxName!.GetHashCode();
            }
        }
    }
}
