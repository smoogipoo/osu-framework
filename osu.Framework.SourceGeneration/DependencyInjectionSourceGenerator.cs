// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using osu.Framework.SourceGeneration.Emitters;

namespace osu.Framework.SourceGeneration
{
    [Generator]
    public class DependencyInjectionSourceGenerator : IIncrementalGenerator
    {
        protected virtual bool AddUniqueNameSuffix => true;

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<GeneratorClassCandidate> classDeclarations =
                context.SyntaxProvider.CreateSyntaxProvider(selectSyntax, transformSyntax)
                       .Where(c => c != null);

            IncrementalValueProvider<(Compilation Compilation, ImmutableArray<GeneratorClassCandidate> classes)> compilationAndClasses =
                context.CompilationProvider.Combine(classDeclarations.Collect());

            context.RegisterSourceOutput(compilationAndClasses, emit);
        }

        private bool selectSyntax(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            if (syntaxNode is not ClassDeclarationSyntax classSyntax)
                return false;

            if (classSyntax.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().Any(c => !c.Modifiers.Any(SyntaxKind.PartialKeyword)))
                return false;

            return true;
        }

        private GeneratorClassCandidate transformSyntax(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            ClassDeclarationSyntax classSyntax = (ClassDeclarationSyntax)context.Node;
            INamedTypeSymbol? symbol = context.SemanticModel.GetDeclaredSymbol(classSyntax);

            if (symbol == null)
                return null!;

            GeneratorClassCandidate? candidate = null;

            // Determine if the class is a candidate for the source generator.
            // Classes may be candidates even if they don't resolve/cache anything themselves, but a base type does.
            foreach (var iFace in symbol.AllInterfaces)
            {
                // All classes that derive from IDrawable need to use the source generator.
                // This is conservative for all other (i.e. non-Drawable) classes to avoid polluting irrelevant classes.
                if (SyntaxHelpers.IsIDrawableInterface(iFace) || SyntaxHelpers.IsITransformableInterface(iFace) || SyntaxHelpers.IsISourceGeneratedDependencyActivatorInterface(iFace))
                {
                    addCandidate();
                    break;
                }
            }

            // Process any [Cached] attributes on any interface on the class excluding base types.
            foreach (var iFace in symbol.Interfaces)
            {
                // Add an interface entry for all interfaces that have a cached attribute.
                if (iFace.GetAttributes().Any(attrib => SyntaxHelpers.IsCachedAttribute(attrib.AttributeClass)))
                    addCandidate().CachedInterfaces.Add(iFace);
            }

            // Process any [Cached] attributes on the class.
            foreach (var attrib in enumerateAttributes(context.SemanticModel, classSyntax))
            {
                if (SyntaxHelpers.IsCachedAttribute(context.SemanticModel, attrib))
                    addCandidate().CachedClasses.Add(new SyntaxWithSymbol(context, classSyntax));
            }

            // Process any attributes of members of the class.
            foreach (var member in classSyntax.Members)
            {
                foreach (var attrib in enumerateAttributes(context.SemanticModel, member))
                {
                    if (SyntaxHelpers.IsBackgroundDependencyLoaderAttribute(context.SemanticModel, attrib))
                        addCandidate().DependencyLoaderMemebers.Add(new SyntaxWithSymbol(context, member));

                    if (member is not PropertyDeclarationSyntax && member is not FieldDeclarationSyntax)
                        continue;

                    if (SyntaxHelpers.IsResolvedAttribute(context.SemanticModel, attrib))
                        addCandidate().ResolvedMembers.Add(new SyntaxWithSymbol(context, member));

                    if (SyntaxHelpers.IsCachedAttribute(context.SemanticModel, attrib))
                        addCandidate().CachedMembers.Add(new SyntaxWithSymbol(context, member));
                }
            }

            GeneratorClassCandidate addCandidate() => candidate ??= new GeneratorClassCandidate(classSyntax, symbol);

            return candidate!;
        }

        private static IEnumerable<AttributeSyntax> enumerateAttributes(SemanticModel semanticModel, MemberDeclarationSyntax member)
        {
            return member.AttributeLists
                         .SelectMany(attribList =>
                             attribList.Attributes
                                       .Where(attrib =>
                                           SyntaxHelpers.IsBackgroundDependencyLoaderAttribute(semanticModel, attrib)
                                           || SyntaxHelpers.IsResolvedAttribute(semanticModel, attrib)
                                           || SyntaxHelpers.IsCachedAttribute(semanticModel, attrib)));
        }

        private void emit(SourceProductionContext context, (Compilation compilation, ImmutableArray<GeneratorClassCandidate> candidates) items)
        {
            if (items.candidates.IsDefaultOrEmpty)
                return;

            IEnumerable<GeneratorClassCandidate> distinctCandidates = items.candidates.Distinct();
            ImmutableHashSet<ISymbol?> allClasses = items.candidates.Select(c => c.Symbol).ToImmutableHashSet(SymbolEqualityComparer.Default);

            foreach (var candidate in distinctCandidates)
            {
                string suffix = AddUniqueNameSuffix ? $"_{Guid.NewGuid()}" : string.Empty;
                string filename = $"g_{candidate.ClassSyntax.Identifier.ValueText}_Dependencies{suffix}.cs";

                context.AddSource(filename, new DependenciesFileEmitter(candidate, items.compilation, allClasses).Emit());
            }
        }
    }
}
