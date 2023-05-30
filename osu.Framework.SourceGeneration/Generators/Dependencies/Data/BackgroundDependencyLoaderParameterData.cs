// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.CodeAnalysis;

namespace osu.Framework.SourceGeneration.Generators.Dependencies.Data
{
    public readonly struct BackgroundDependencyLoaderParameterData
    {
        public readonly string ParameterName;
        public readonly string GlobalPrefixedTypeName;
        public readonly bool CanBeNull;

        public BackgroundDependencyLoaderParameterData(string parameterName, string globalPrefixedTypeName, bool canBeNull)
        {
            ParameterName = parameterName;
            GlobalPrefixedTypeName = globalPrefixedTypeName;
            CanBeNull = canBeNull;
        }

        public static BackgroundDependencyLoaderParameterData FromParameter(IParameterSymbol parameter)
        {
            string globalPrefixedTypeName = SyntaxHelpers.GetGlobalPrefixedTypeName(parameter.Type)!;
            bool canBeNull = parameter.NullableAnnotation == NullableAnnotation.Annotated;

            return new BackgroundDependencyLoaderParameterData(parameter.Name, globalPrefixedTypeName, canBeNull);
        }
    }
}
