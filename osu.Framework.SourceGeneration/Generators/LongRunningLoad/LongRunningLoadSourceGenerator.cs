// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.CodeAnalysis;

namespace osu.Framework.SourceGeneration.Generators.LongRunningLoad
{
    [Generator]
    public class LongRunningLoadSourceGenerator : AbstractIncrementalGenerator
    {
        protected override IncrementalSemanticTarget CreateSemanticTarget(INamedTypeSymbol symbol)
            => new LongRunningLoadSemanticTarget(symbol);

        protected override IncrementalSourceEmitter CreateSourceEmitter(IncrementalSemanticTarget target)
            => new LongRunningLoadSourceEmitter(target);
    }
}
