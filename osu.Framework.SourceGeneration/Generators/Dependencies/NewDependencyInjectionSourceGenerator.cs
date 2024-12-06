// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using osu.Framework.SourceGeneration.Generators.Dependencies.Emitters;

namespace osu.Framework.SourceGeneration.Generators.Dependencies
{
    [Generator]
    public class NewDependencyInjectionSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // All interfaces that have a [Cached] attribute.
            // IncrementalValuesProvider<DependencyInjectionSemanticTarget> cachedInterfaces =
            //     context.SyntaxProvider
            //            .CreateSyntaxProvider(
            //                (n, _) => n.IsKind(SyntaxKind.InterfaceDeclaration),
            //                (ctx, _) => new DependencyInjectionSemanticTarget(ctx))
            //            .Where(target => target.AnyAttributes());

            // Classes containing [Cached], [Resolved], or [BackgroundDependencyLoader] attributes.
            IncrementalValuesProvider<ActivatorCandidate> candidates =
                context.SyntaxProvider
                       .CreateSyntaxProvider(
                           (n, _) => n.IsKind(SyntaxKind.ClassDeclaration),
                           (ctx, _) => new ActivatorCandidate(ctx))
                       .Where(candidate => candidate.HasAnyAttributes);

            // Classes with semantic information.
            IncrementalValuesProvider<DependenciesClassCandidate> semanticCandidates =
                candidates.Select((candidate, _) => new DependenciesClassCandidate((ClassDeclarationSyntax)candidate.Context.Node, candidate.Context.SemanticModel));

            context.RegisterImplementationSourceOutput(
                semanticCandidates,
                (ctx, candidate) => new DependenciesFileEmitter(candidate).Emit(ctx.AddSource));
        }

        private readonly struct ActivatorCandidate : IEquatable<ActivatorCandidate>
        {
            public readonly GeneratorSyntaxContext Context;

            private readonly ClassDeclarationSyntax classSyntax;
            private readonly List<CachedMemberInfo> cachedMembers = new List<CachedMemberInfo>();
            private readonly List<ResolvedMemberInfo> resolvedMembers = new List<ResolvedMemberInfo>();
            private readonly List<LoaderMemberInfo> loaderMembers = new List<LoaderMemberInfo>();

            public ActivatorCandidate(GeneratorSyntaxContext context)
            {
                Context = context;
                classSyntax = (ClassDeclarationSyntax)context.Node;

                foreach (var attrib in NewSyntaxHelpers.EnumerateNamedAttributes(classSyntax, "Cached"))
                    cachedMembers.Add(new CachedMemberInfo(attrib, classSyntax));

                foreach (var member in classSyntax.Members)
                {
                    switch (member)
                    {
                        case PropertyDeclarationSyntax property:
                            foreach (var attrib in NewSyntaxHelpers.EnumerateNamedAttributes(property, "Cached"))
                                cachedMembers.Add(new CachedMemberInfo(attrib, property));
                            foreach (var attrib in NewSyntaxHelpers.EnumerateNamedAttributes(property, "Resolved"))
                                resolvedMembers.Add(new ResolvedMemberInfo(attrib, property));
                            break;

                        case FieldDeclarationSyntax field:
                            foreach (var attrib in NewSyntaxHelpers.EnumerateNamedAttributes(field, "Cached"))
                                cachedMembers.Add(new CachedMemberInfo(attrib, field));
                            break;

                        case MethodDeclarationSyntax method:
                            foreach (var attrib in NewSyntaxHelpers.EnumerateNamedAttributes(method, "BackgroundDependencyLoader"))
                                loaderMembers.Add(new LoaderMemberInfo(attrib, method));
                            break;
                    }
                }
            }

            public bool HasAnyAttributes => cachedMembers.Any() || resolvedMembers.Any() || loaderMembers.Any();

            public bool Equals(ActivatorCandidate other)
                => classSyntax.Identifier.IsEquivalentTo(other.classSyntax.Identifier)
                   && cachedMembers.SequenceEqual(other.cachedMembers)
                   && resolvedMembers.SequenceEqual(other.resolvedMembers)
                   && loaderMembers.SequenceEqual(other.loaderMembers);
        }

        private readonly struct CachedMemberInfo : IEquatable<CachedMemberInfo>
        {
            private readonly AttributeSyntax attributeSyntax;
            private readonly MemberDeclarationSyntax memberSyntax;

            public CachedMemberInfo(AttributeSyntax attributeSyntax, MemberDeclarationSyntax memberSyntax)
            {
                this.attributeSyntax = attributeSyntax;
                this.memberSyntax = memberSyntax;
            }

            public bool Equals(CachedMemberInfo other)
                => attributeSyntax.IsEquivalentTo(other.attributeSyntax)
                   && memberSyntax switch
                   {
                       ClassDeclarationSyntax c => c.Identifier.IsEquivalentTo(((ClassDeclarationSyntax)other.memberSyntax).Identifier),
                       InterfaceDeclarationSyntax i => i.Identifier.IsEquivalentTo(((InterfaceDeclarationSyntax)other.memberSyntax).Identifier),
                       PropertyDeclarationSyntax p => p.IsEquivalentTo((PropertyDeclarationSyntax)other.memberSyntax),
                       FieldDeclarationSyntax f => f.IsEquivalentTo((FieldDeclarationSyntax)other.memberSyntax),
                       _ => false
                   };
        }

        private readonly struct ResolvedMemberInfo : IEquatable<ResolvedMemberInfo>
        {
            private readonly AttributeSyntax attributeSyntax;
            private readonly PropertyDeclarationSyntax propertySyntax;

            public ResolvedMemberInfo(AttributeSyntax attributeSyntax, PropertyDeclarationSyntax propertySyntax)
            {
                this.attributeSyntax = attributeSyntax;
                this.propertySyntax = propertySyntax;
            }

            public bool Equals(ResolvedMemberInfo other)
                => attributeSyntax.IsEquivalentTo(other.attributeSyntax)
                   && propertySyntax.IsEquivalentTo(other.propertySyntax);
        }

        private readonly struct LoaderMemberInfo : IEquatable<LoaderMemberInfo>
        {
            private readonly AttributeSyntax attributeSyntax;
            private readonly MethodDeclarationSyntax methodSyntax;

            public LoaderMemberInfo(AttributeSyntax attributeSyntax, MethodDeclarationSyntax methodSyntax)
            {
                this.attributeSyntax = attributeSyntax;
                this.methodSyntax = methodSyntax;
            }

            public bool Equals(LoaderMemberInfo other)
                => attributeSyntax.IsEquivalentTo(other.attributeSyntax)
                   && methodSyntax.Identifier.IsEquivalentTo(other.methodSyntax.Identifier)
                   && methodSyntax.ParameterList.IsEquivalentTo(other.methodSyntax.ParameterList);
        }
    }

    public static class NewSyntaxHelpers
    {
        public static string GetUnqualifiedName(NameSyntax name)
        {
            return name switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
                QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
                SimpleNameSyntax simple => simple.Identifier.ValueText,
                _ => throw new ArgumentException("Unexpected name syntax.", nameof(name))
            };
        }

        public static IEnumerable<AttributeSyntax> EnumerateNamedAttributes(MemberDeclarationSyntax syntax, string name)
        {
            foreach (var list in syntax.AttributeLists)
            {
                foreach (var attribute in list.Attributes)
                {
                    string attribName = GetUnqualifiedName(attribute.Name);

                    // Note that this is somewhat "wide" for brevity, because any time we see "Cached", "Resolved", or "BackgroundDependencyLoader",
                    // it's generally going to be one of our own attributes (which otherwise cover 95% of classes anyway).
                    if (attribName.StartsWith(name, StringComparison.Ordinal))
                        yield return attribute;
                }
            }
        }
    }
}
