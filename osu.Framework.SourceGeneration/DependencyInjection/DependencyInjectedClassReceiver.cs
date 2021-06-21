// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.DependencyInjection
{
    public class DependencyInjectedClassReceiver : ISyntaxContextReceiver
    {
        public const string BACKGROUND_DEPENDENCY_LOADER_ATTRIBUTE_NAME = "osu.Framework.Allocation.BackgroundDependencyLoaderAttribute";
        public const string RESOLVED_ATTRIBUTE_NAME = "osu.Framework.Allocation.ResolvedAttribute";

#pragma warning disable RS1024 // Symbols are correctly compared via SymbolEqualityComparer.
        public readonly Dictionary<INamedTypeSymbol, DependencyGroup> Groups = new Dictionary<INamedTypeSymbol, DependencyGroup>(SymbolEqualityComparer.Default);
#pragma warning restore RS1024

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            switch (context.Node)
            {
                case PropertyDeclarationSyntax propSyntax:
                    if (propSyntax.AttributeLists.Count == 0)
                        return;

                    if (!isInPartialClass(propSyntax))
                        return;

                    // Resolve the symbol to check the attribute name.
                    var propSymbol = context.SemanticModel.GetDeclaredSymbol(propSyntax);
                    if (propSymbol!.GetAttributes().All(a => a.AttributeClass?.ToDisplayString() != RESOLVED_ATTRIBUTE_NAME))
                        return;

                    getGroup(propSymbol.ContainingType).Properties.Add(propSymbol);

                    break;

                case MethodDeclarationSyntax methodSyntax:
                    if (methodSyntax.AttributeLists.Count == 0)
                        return;

                    if (!isInPartialClass(methodSyntax))
                        return;

                    // Resolve the symbol to check the attribute name.
                    var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodSyntax);
                    if (methodSymbol!.GetAttributes().All(a => a.AttributeClass?.ToDisplayString() != BACKGROUND_DEPENDENCY_LOADER_ATTRIBUTE_NAME))
                        return;

                    getGroup(methodSymbol.ContainingType).Methods.Add(methodSymbol);

                    break;
            }
        }

        private bool isInPartialClass(SyntaxNode node)
        {
            return node.AncestorsAndSelf()
                       .OfType<ClassDeclarationSyntax>()
                       .All(classDeclaration => classDeclaration.Modifiers.Any(m => m.Kind() == SyntaxKind.PartialKeyword));
        }

        private DependencyGroup getGroup(INamedTypeSymbol type)
        {
            if (Groups.TryGetValue(type, out var group))
                return group;

            return Groups[type] = new DependencyGroup();
        }
    }

    public class DependencyGroup
    {
        public readonly List<IPropertySymbol> Properties = new List<IPropertySymbol>();
        public readonly List<IMethodSymbol> Methods = new List<IMethodSymbol>();
    }
}
