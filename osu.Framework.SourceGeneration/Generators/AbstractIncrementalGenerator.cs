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

            // 2: Get syntax targets for all classes.
            IncrementalValuesProvider<ClassDeclarationSyntax> syntaxTargets =
                context.SyntaxProvider
                       .CreateSyntaxProvider(
                           (n, _) => isSyntaxTarget(n),
                           (ctx, _) => returnWithEvent((ClassDeclarationSyntax)ctx.Node, EventDriver.OnSyntaxTargetCreated));

            // 3. Filter syntax targets to release-mode compilations.
            IncrementalValuesProvider<ClassDeclarationSyntax> releaseOnlySyntaxTargets =
                syntaxTargets
                    .Combine(isReleaseBuild)
                    .Where(x => x.Right)
                    .Select((x, _) => x.Left);

            // 4. Retrieve symbols for the syntax targets.
            IncrementalValuesProvider<INamedTypeSymbol> releaseOnlySymbols =
                releaseOnlySyntaxTargets
                    .Combine(context.CompilationProvider)
                    .Select((x, token) => x.Right.GetSemanticModel(x.Left.SyntaxTree).GetDeclaredSymbol(x.Left, token)!);

            // 5. De-duplicate symbols across partial definitions.
            IncrementalValuesProvider<INamedTypeSymbol> uniqueSymbols =
                releaseOnlySymbols
                    .Collect()
                    .SelectMany((symbols, _) => symbols.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default));

            // 6. Create final semantic targets from symbols.
            IncrementalValuesProvider<IncrementalSemanticTarget> uniqueSemanticTargets =
                uniqueSymbols
                    .Select((x, _) => returnWithEvent(CreateSemanticTarget(x), EventDriver.OnSemanticTargetCreated));

            context.RegisterImplementationSourceOutput(uniqueSemanticTargets, emit);
        }

        protected abstract IncrementalSemanticTarget CreateSemanticTarget(INamedTypeSymbol symbol);

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
