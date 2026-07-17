// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.Generators.AutoUnbind
{
    [Generator]
    public class AutoUnbindGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var classDeclarations = context.SyntaxProvider.CreateSyntaxProvider(isSyntaxTarget, getSemanticTarget);
        }

        private static bool isSyntaxTarget(SyntaxNode node, CancellationToken token)
        {
            return node is ClassDeclarationSyntax;
        }

        private static ClassTargetInfo? getSemanticTarget(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;

            var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken) as INamedTypeSymbol;
            if (classSymbol == null)
                return null;

            var drawableType = context.SemanticModel.Compilation.GetTypeByMetadataName("osu.Framework.Graphics.Drawable");
            if (drawableType is null)
                return null;

            if (!isSubTypeOf(classSymbol, drawableType))
                return null;

            var bindableType = context.SemanticModel.Compilation.GetTypeByMetadataName("osu.Framework.Bindables.IUnbindable");
            if (bindableType == null)
                return null;

            var bindableFields = new List<BindableTargetInfo>();

            foreach (var member in classSymbol.GetMembers())
            {
                if (member is IFieldSymbol fieldSymbol && isSubTypeOf(fieldSymbol.Type, bindableType))
                    bindableFields.Add(new BindableTargetInfo(fieldSymbol.Name));
            }

            if (bindableFields.Count == 0)
                return null;

            return new ClassTargetInfo(classSymbol.ToDisplayString(), bindableFields);
        }

        private static bool isSubTypeOf(ITypeSymbol type, INamedTypeSymbol target)
        {
            if (SymbolEqualityComparer.Default.Equals(type, target))
                return true;

            if (target.TypeKind == TypeKind.Interface)
            {
                foreach (var iface in type.AllInterfaces)
                {
                    if (SymbolEqualityComparer.Default.Equals(iface, target))
                        return true;
                }
            }

            var current = type.BaseType;

            while (current != null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, target))
                    return true;

                current = current.BaseType;
            }

            return false;
        }
    }
}
