// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.SourceGeneration.Generators
{
    public class GeneratorEventDriver
    {
        public event Action<object>? SyntaxTargetCreated;
        public event Action<object>? SemanticTargetCreated;
        public event Action<object>? Emit;

        public void OnSyntaxTargetCreated(object target)
        {
            conditionalInvoke(SyntaxTargetCreated, target);
        }

        public void OnSemanticTargetCreated(object target)
        {
            conditionalInvoke(SemanticTargetCreated, target);
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
