// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.CodeAnalysis;

namespace osu.Framework.SourceGeneration
{
    public static class DiagnosticRules
    {
        // Disable's roslyn analyser release tracking. Todo: Temporary? The analyser doesn't behave well with Rider :/
        // Read more: https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md
#pragma warning disable RS2008

        public static readonly DiagnosticDescriptor MAKE_CLASS_PARTIAL = new DiagnosticDescriptor(
            "OFOPT001",
            "Make class partial",
            "Make class partial",
            "Performance",
            DiagnosticSeverity.Warning,
            true,
            "Classes with dependencies should be made partial for compile-time optimisation.");

#pragma warning restore RS2008
    }
}
