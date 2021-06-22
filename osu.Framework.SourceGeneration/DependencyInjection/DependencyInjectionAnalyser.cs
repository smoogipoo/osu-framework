// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace osu.Framework.SourceGeneration.DependencyInjection
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DependencyInjectionAnalyser : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticRules.MAKE_CLASS_PARTIAL);

        public override void Initialize(AnalysisContext context)
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(analyseClass, SyntaxKind.ClassDeclaration);
        }

        private void analyseClass(SyntaxNodeAnalysisContext context)
        {
            var classSyntax = (ClassDeclarationSyntax)context.Node;

            if (classSyntax.Modifiers.Any(m => m.Kind() == SyntaxKind.PartialKeyword))
                return;

            foreach (var n in context.Node.DescendantNodes())
            {
                switch (n.Kind())
                {
                    case SyntaxKind.PropertyDeclaration:
                        if (((PropertyDeclarationSyntax)n).AttributeLists.Any(a => a.Attributes.Any(attr => attr.Name.ToString() == "Resolved")))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.MAKE_CLASS_PARTIAL, context.Node.GetLocation(), context.Node));
                            return;
                        }

                        break;

                    case SyntaxKind.MethodDeclaration:
                        if (((MethodDeclarationSyntax)n).AttributeLists.Any(a => a.Attributes.Any(attr => attr.Name.ToString() == "BackgroundDependencyLoader")))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.MAKE_CLASS_PARTIAL, context.Node.GetLocation(), context.Node));
                            return;
                        }

                        break;
                }
            }
        }
    }
}
