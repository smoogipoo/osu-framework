// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace osu.Framework.SourceGeneration.Generators
{
    public abstract class IncrementalSemanticTarget
    {
        public readonly string FullyQualifiedTypeName = string.Empty;
        public readonly string GlobalPrefixedTypeName = string.Empty;
        public readonly bool NeedsOverride;
        public readonly string? ContainingNamespace;
        public readonly bool IsValid;

        public readonly List<string> TypeHierarchy = new List<string>();

        protected IncrementalSemanticTarget(INamedTypeSymbol symbol)
        {
            IsValid = CheckValid(symbol);

            if (!IsValid)
                return;

            FullyQualifiedTypeName = SyntaxHelpers.GetFullyQualifiedTypeName(symbol);
            GlobalPrefixedTypeName = SyntaxHelpers.GetGlobalPrefixedTypeName(symbol)!;
            NeedsOverride = symbol.BaseType != null && CheckNeedsOverride(symbol);
            ContainingNamespace = symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString();

            ITypeSymbol? containingType = symbol;

            while (containingType != null)
            {
                TypeHierarchy.Add(createTypeName(containingType));
                containingType = containingType.ContainingType ?? null;
            }

            Process(symbol);
        }

        /// <summary>
        /// Determines whether a source should be generated for this semantic target.
        /// </summary>
        /// <param name="symbol">The symbol represented by this semantic target.</param>
        protected abstract bool CheckValid(INamedTypeSymbol symbol);

        /// <summary>
        /// Determines whether the source generator should override any methods of the semantic target.
        /// This can happen if the base and sub types are both valid (<see cref="CheckValid"/>) semantic targets.
        /// </summary>
        /// <param name="symbol">The symbol represented by this semantic target.</param>
        protected abstract bool CheckNeedsOverride(INamedTypeSymbol symbol);

        /// <summary>
        /// Processes the symbol to extract any data that could be used in source generation.
        /// The extracted data <b>must not</b> reference the symbol.
        /// </summary>
        /// <param name="symbol">The symbol represented by this semantic target.</param>
        protected abstract void Process(INamedTypeSymbol symbol);

        private static string createTypeName(ITypeSymbol typeSymbol)
        {
            string name = typeSymbol.Name;

            if (typeSymbol is INamedTypeSymbol named && named.TypeParameters.Length > 0)
                name += $@"<{string.Join(@", ", named.TypeParameters)}>";

            return name;
        }
    }
}
