// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace osu.Framework.SourceGeneration.DependencyInjection
{
    [Generator]
    public class DependencyInjectionSourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            // if (!attachedOnce)
            // {
            //     while (!Debugger.IsAttached)
            //         attachedOnce = true;
            // }

            context.RegisterForSyntaxNotifications(() => new DependencyInjectedClassReceiver());
        }

        private bool attachedOnce;

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is not DependencyInjectedClassReceiver dependenciesReceiver)
                return;

            // if (!attachedOnce)
            // {
            //     while (!Debugger.IsAttached)
            //         attachedOnce = true;
            // }

            var resolvedSymbol = context.Compilation.GetTypeByMetadataName(DependencyInjectedClassReceiver.RESOLVED_ATTRIBUTE_NAME);
            var backgroundDependencyLoaderSymbol = context.Compilation.GetTypeByMetadataName(DependencyInjectedClassReceiver.BACKGROUND_DEPENDENCY_LOADER_ATTRIBUTE_NAME);

            foreach (var kvp in dependenciesReceiver.Groups)
            {
                var classType = kvp.Key;
                var requiredDependencies = kvp.Value!;
                string classTypeName = classType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

                var injectionBuilder = new StringBuilder();

                foreach (var p in requiredDependencies.Properties)
                {
                    var attributeData = p.GetAttributes().Single(a => a.AttributeClass!.Equals(resolvedSymbol, SymbolEqualityComparer.Default));
                    Type? resolvedParentType = (Type?)attributeData.NamedArguments.SingleOrDefault(arg => arg.Key == "Parent").Value.Value;
                    string? resolvedName = (string?)attributeData.NamedArguments.SingleOrDefault(arg => arg.Key == "Name").Value.Value;
                    bool resolvedCanBeNull = (bool)(attributeData.NamedArguments.SingleOrDefault(arg => arg.Key == "CanBeNull").Value.Value ?? false);

                    injectionBuilder.Append($"\t\t\tthis.{p.Name} = dependencyContainer.Get<{p.Type.ToDisplayString()}>(");

                    if (resolvedParentType != null || resolvedName != null)
                    {
                        injectionBuilder.Append("new CacheInfo(");

                        if (resolvedName != null)
                            injectionBuilder.Append($"name: \"{resolvedName}\"");
                        if (resolvedParentType != null)
                            injectionBuilder.Append($"parent: typeof({resolvedParentType.Name})");

                        injectionBuilder.Append(')');
                    }

                    injectionBuilder.Append(')');

                    if (!resolvedCanBeNull)
                        injectionBuilder.Append($" ?? throw new DependencyNotRegisteredException(typeof({classType.ToDisplayString()}), typeof({p.Type.ToDisplayString()}))");

                    injectionBuilder.Append(';');
                    injectionBuilder.AppendLine();
                }

                injectionBuilder.AppendLine();

                foreach (var m in requiredDependencies.Methods)
                {
                    var attributeData = m.GetAttributes().Single(a => a.AttributeClass!.Equals(backgroundDependencyLoaderSymbol, SymbolEqualityComparer.Default));
                    bool resolvedCanBeNull = (bool)(attributeData.ConstructorArguments.FirstOrDefault().Value ?? false);

                    injectionBuilder.AppendLine($"\t\t\t{m.Name}(");

                    for (int i = 0; i < m.Parameters.Length; i++)
                    {
                        var parameterType = m.Parameters[i].Type.ToDisplayString();

                        injectionBuilder.Append($"\t\t\t\t({parameterType})(dependencyContainer.Get(typeof({parameterType}))");
                        if (!resolvedCanBeNull && m.Parameters[i].NullableAnnotation != NullableAnnotation.Annotated)
                            injectionBuilder.Append($" ?? throw new DependencyNotRegisteredException(typeof({classType.ToDisplayString()}), typeof({parameterType}))");
                        injectionBuilder.Append(')');

                        if (i != m.Parameters.Length - 1)
                            injectionBuilder.Append(',');

                        injectionBuilder.AppendLine();
                    }

                    injectionBuilder.AppendLine("\t\t\t);");
                }

                string fileName = $"{classTypeName.Replace('<', '{').Replace('>', '}')}.deps.g.cs";

                string contents = $@"
using osu.Framework.Allocation;

namespace {classType.ContainingNamespace.ToDisplayString()}
{{
    partial class {classTypeName} : IGeneratedDependencyInjector<{classTypeName}>
    {{
        void IGeneratedDependencyInjector<{classTypeName}>.Inject(IReadOnlyDependencyContainer dependencyContainer)
        {{
{injectionBuilder}
        }}
    }}
}}";

                context.AddSource(fileName, SourceText.From(contents, Encoding.UTF8));
            }
        }
    }
}
