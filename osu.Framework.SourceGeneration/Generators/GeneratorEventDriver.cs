// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace osu.Framework.SourceGeneration.Generators
{
    public class GeneratorEventDriver
    {
        public event Action<object>? SyntaxTargetCreated;
        public event Action<object>? SemanticTargetCreated;
        public event Action<ImmutableArray<object>>? Stage2Entry;
        public event Action<ImmutableArray<object>>? Stage2GenerationIdAssigned;
        public event Action<HashSet<object>>? Stage2Exit;
        public event Action<object>? Emit;

        public void OnSyntaxTargetCreated(object target)
        {
            conditionalInvoke(SyntaxTargetCreated, target);
        }

        public void OnSemanticTargetCreated(object target)
        {
            conditionalInvoke(SemanticTargetCreated, target);
        }

        public void OnStage2Entry(ImmutableArray<object> target)
        {
            conditionalInvoke(Stage2Entry, target);
        }

        public void OnStage2GenerationIdAssigned(ImmutableArray<object> target)
        {
            conditionalInvoke(Stage2GenerationIdAssigned, target);
        }

        public void OnStage2Exit(HashSet<object> target)
        {
            conditionalInvoke(Stage2Exit, target);
        }

        public void OnEmit(object candidate)
        {
            conditionalInvoke(Emit, candidate);
        }

        // Since we're running source generators in release configuration along with tests,
        // we need this to always fire. Because we're not really worried about the compile
        // overhead (due to only incurring on release builds) this isn't seen as a huge issue.
        // [Conditional("DEBUG")]
        private void conditionalInvoke<T>(Action<T>? @event, T arg)
        {
            @event?.Invoke(arg);
        }
    }
}
