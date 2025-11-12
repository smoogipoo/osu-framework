// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.CodeAnalysis;
using osu.Framework.SourceGeneration.Generators.Dependencies.Emitters;

namespace osu.Framework.SourceGeneration.Generators.Dependencies
{
    [Generator]
    public class DependencyInjectionSourceGenerator : AbstractIncrementalGenerator
    {
        protected override IncrementalSemanticTarget CreateSemanticTarget(INamedTypeSymbol symbol)
            => new DependenciesClassCandidate(symbol);

        protected override IncrementalSourceEmitter CreateSourceEmitter(IncrementalSemanticTarget target)
            => new DependenciesFileEmitter((DependenciesClassCandidate)target);
    }
}
