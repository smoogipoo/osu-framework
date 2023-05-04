// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.CodeAnalysis;

namespace osu.Framework.SourceGeneration.Analysers
{
    public class DiagnosticRules
    {
        // Disable's roslyn analyser release tracking. Todo: Temporary? The analyser doesn't behave well with Rider :/
        // Read more: https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md
#pragma warning disable RS2008

        public static readonly DiagnosticDescriptor MAKE_DI_CLASS_PARTIAL = new DiagnosticDescriptor(
            "OFSG001",
            "This class is a candidate for dependency injection and should be partial",
            "This class is a candidate for dependency injection and should be partial",
            "Performance",
            DiagnosticSeverity.Warning,
            true,
            "Classes that are candidates for dependency injection should be made partial to benefit from compile-time optimisations.");

        public static readonly DiagnosticDescriptor ASYNC_BDL_MUST_RETURN_TASK = new DiagnosticDescriptor(
            "OFSG002",
            "Async BackgroundDependencyLoader methods must return Task",
            "Async BackgroundDependencyLoader methods must return Task",
            "Performance",
            DiagnosticSeverity.Error,
            true,
            "Async BackgroundDependencyLoader methods must return Task.");

        public static readonly DiagnosticDescriptor CONFIGURE_AWAIT_MUST_BE_TRUE = new DiagnosticDescriptor(
            "OFSG003",
            "Direct awaits in a BackgroundDependencyLoader must use ConfigureAwait(true)",
            "Direct awaits in a BackgroundDependencyLoader must use ConfigureAwait(true)",
            "Performance",
            DiagnosticSeverity.Error,
            true,
            "Direct awaits in a BackgroundDependencyLoader must use ConfigureAwait(true).");

#pragma warning restore RS2008
    }
}
