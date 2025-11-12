// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.Generators
{
    public abstract class AbstractIncrementalGenerator : IIncrementalGenerator
    {
        public readonly GeneratorEventDriver EventDriver = new GeneratorEventDriver();

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 1. Get release-mode compilations.
            IncrementalValueProvider<bool> isReleaseBuild =
                context.CompilationProvider
                       .Select((comp, _) =>
                           comp.Options.OptimizationLevel == OptimizationLevel.Release);

            // 2: Create syntax targets for all classes.
            IncrementalValuesProvider<IncrementalSyntaxTarget> syntaxTargets =
                context.SyntaxProvider.CreateSyntaxProvider(
                    (n, _) => isSyntaxTarget(n),
                    (ctx, _) => returnWithEvent(new IncrementalSyntaxTarget((ClassDeclarationSyntax)ctx.Node), EventDriver.OnSyntaxTargetCreated));

            // 3. Get release-only syntax targets.
            IncrementalValuesProvider<IncrementalSyntaxTarget> releaseOnlySyntaxTargets =
                syntaxTargets.Combine(isReleaseBuild)
                             .Where(x => x.Right)
                             .Select((x, _) => x.Left);

            // 4. Create release-only semantic targets.
            IncrementalValuesProvider<IncrementalSemanticTarget> releaseOnlySemanticTargets =
                releaseOnlySyntaxTargets
                    .Combine(context.CompilationProvider)
                    .Select((x, _) => CreateSemanticTarget(x.Left.Syntax, x.Right.GetSemanticModel(x.Left.Syntax.SyntaxTree)));

            context.RegisterImplementationSourceOutput(releaseOnlySemanticTargets, emit);
        }

        protected abstract IncrementalSemanticTarget CreateSemanticTarget(ClassDeclarationSyntax node, SemanticModel semanticModel);

        protected abstract IncrementalSourceEmitter CreateSourceEmitter(IncrementalSemanticTarget target);

        private static bool isSyntaxTarget(SyntaxNode syntaxNode)
        {
            if (syntaxNode is not ClassDeclarationSyntax classSyntax)
                return false;

            if (classSyntax.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().Any(c => !c.Modifiers.Any(SyntaxKind.PartialKeyword)))
                return false;

            return true;
        }

        private void emit(SourceProductionContext context, IncrementalSemanticTarget target)
        {
            EventDriver.OnEmit(target);
            CreateSourceEmitter(target).Emit(context.AddSource);
        }

        private static T returnWithEvent<T>(T arg, Action<T> @event)
        {
            @event(arg);
            return arg;
        }
    }
}
