// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#if NETCOREAPP
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using osu.Framework.Logging;

namespace osu.Framework.Testing
{
    public class RoslynTypeReferenceBuilder : ITypeReferenceBuilder
    {
        // The "Attribute" suffix disappears when used via a nuget package, so it is trimmed here.
        private static readonly string exclude_attribute_name = nameof(ExcludeFromDynamicCompileAttribute).Replace(nameof(Attribute), string.Empty);

        private readonly Logger logger;

        private readonly Dictionary<TypeReference, IReadOnlyCollection<TypeReference>> referenceMap = new Dictionary<TypeReference, IReadOnlyCollection<TypeReference>>();
        private readonly Dictionary<Project, Compilation> compilationCache = new Dictionary<Project, Compilation>();
        private readonly Dictionary<SyntaxTree, SemanticModel> semanticModelCache = new Dictionary<SyntaxTree, SemanticModel>();
        private readonly Dictionary<TypeReference, bool> typeInheritsFromGameCache = new Dictionary<TypeReference, bool>();
        private readonly Dictionary<string, bool> syntaxExclusionMap = new Dictionary<string, bool>();

        private Solution solution;

        public RoslynTypeReferenceBuilder()
        {
            logger = Logger.GetLogger("dynamic-compilation");
            logger.OutputToListeners = false;
        }

        public async Task Initialise(string solutionFile)
        {
            MSBuildLocator.RegisterDefaults();
            solution = await MSBuildWorkspace.Create().OpenSolutionAsync(solutionFile);
        }

        public async Task<IReadOnlyCollection<string>> GetReferencedFiles(Type testType, string changedFile)
        {
            clearCaches();
            updateFile(changedFile);

            await buildReferenceMapAsync(testType, changedFile);

            var directedGraph = getDirectedGraph();

            return getReferencedFiles(getTypesFromFile(changedFile), directedGraph);
        }

        public async Task<IReadOnlyCollection<string>> GetReferencedAssemblies(Type testType, string changedFile) => await Task.Run(() =>
        {
            // Todo: This is temporary, and is potentially missing assemblies.

            var assemblies = new HashSet<string>();

            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic))
                assemblies.Add(ass.Location);
            assemblies.Add(typeof(JetBrains.Annotations.NotNullAttribute).Assembly.Location);

            return assemblies;
        });

        public void Reset()
        {
            clearCaches();
            referenceMap.Clear();
        }

        /// <summary>
        /// Builds the reference map, connecting all types to their immediate references. Results are placed inside <see cref="referenceMap"/>.
        /// </summary>
        /// <param name="testType">The test target - the top-most level.</param>
        /// <param name="changedFile">The file that was changed.</param>
        /// <exception cref="InvalidOperationException">If <paramref name="testType"/> could not be retrieved from the solution.</exception>
        private async Task buildReferenceMapAsync(Type testType, string changedFile)
        {
            // We want to find a graph of types from the testType symbol (P) to all the types which it references recursively.
            //
            //                            P
            //                          /  \
            //                         /    \
            //                        /      \
            //                      C1       C2 ---
            //                    /   \      |    /
            //                  C3    C4    C5   /
            //                          \  /    /
            //                           C6 ---
            //
            // The reference map is a key-value pairing of all types to their immediate references. A directed graph can be built by traversing through types.
            //
            // P -> { C1, C2 }
            // C1 -> { C3, C4 }
            // C2 -> { C5, C6 }
            // C3 -> { }
            // C4 -> { C6 }
            // C5 -> { C6 }
            // C6 -> { C2 }

            logger.Add("Building reference map...");

            var compiledTestProject = await compileProjectAsync(findTestProject());
            var compiledTestType = compiledTestProject.GetTypeByMetadataName(testType.FullName);

            if (compiledTestType == null)
                throw new InvalidOperationException("Failed to retrieve test type from the solution.");

            if (referenceMap.Count > 0)
            {
                logger.Add("Attempting to use cache...");

                // We already have some references, so we can do a partial re-process of the map for only the changed file.
                var oldTypes = getTypesFromFile(changedFile).ToArray();

                foreach (var t in oldTypes)
                {
                    referenceMap.Remove(t);
                    typeInheritsFromGameCache.Remove(t);
                }

                foreach (var t in oldTypes)
                {
                    string typePath = t.Symbol.Locations.First().SourceTree?.FilePath;

                    // The type we have is on an old compilation, we need to re-retrieve it on the new one.
                    var project = getProjectFromFile(typePath);

                    if (project == null)
                    {
                        logger.Add("File has been renamed. Rebuilding reference map from scratch...");
                        Reset();
                        break;
                    }

                    var compilation = await compileProjectAsync(project);
                    var syntaxTree = compilation.SyntaxTrees.First(tree => tree.FilePath == typePath);
                    var semanticModel = await getSemanticModelAsync(syntaxTree);
                    var referencedTypes = await getReferencedTypesAsync(semanticModel);

                    referenceMap[TypeReference.FromSymbol(t.Symbol)] = referencedTypes;

                    foreach (var referenced in referencedTypes)
                        await buildReferenceMapRecursiveAsync(referenced);
                }
            }

            if (referenceMap.Count == 0)
            {
                // We have no cache available, so we must rebuild the whole map.
                await buildReferenceMapRecursiveAsync(TypeReference.FromSymbol(compiledTestType));
            }
        }

        /// <summary>
        /// Builds the reference map starting from a root type reference, connecting all types to their immediate references. Results are placed inside <see cref="referenceMap"/>.
        /// </summary>
        /// <remarks>
        /// This should not be used by itself. Use <see cref="buildReferenceMapAsync"/> instead.
        /// </remarks>
        /// <param name="rootReference">The root, where the map should start being build from.</param>
        private async Task buildReferenceMapRecursiveAsync(TypeReference rootReference)
        {
            var searchQueue = new Queue<TypeReference>();
            searchQueue.Enqueue(rootReference);

            while (searchQueue.Count > 0)
            {
                var toCheck = searchQueue.Dequeue();
                var referencedTypes = await getReferencedTypesAsync(toCheck);

                referenceMap[toCheck] = referencedTypes;

                foreach (var referenced in referencedTypes)
                {
                    // We don't want to cycle over types that have already been explored.
                    if (!referenceMap.ContainsKey(referenced))
                    {
                        // Used for de-duping, so it must be added to the dictionary immediately.
                        referenceMap[referenced] = null;
                        searchQueue.Enqueue(referenced);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves all <see cref="TypeReference"/>s referenced by a given <see cref="TypeReference"/>, across all symbol sources.
        /// </summary>
        /// <param name="typeReference">The target <see cref="TypeReference"/>.</param>
        /// <returns>All <see cref="TypeReference"/>s referenced to across all symbol sources by <paramref name="typeReference"/>.</returns>
        private async Task<HashSet<TypeReference>> getReferencedTypesAsync(TypeReference typeReference)
        {
            var result = new HashSet<TypeReference>();

            foreach (var reference in typeReference.Symbol.DeclaringSyntaxReferences)
            {
                foreach (var type in await getReferencedTypesAsync(await getSemanticModelAsync(reference.SyntaxTree)))
                    result.Add(type);
            }

            return result;
        }

        /// <summary>
        /// Retrieves all <see cref="TypeReference"/>s referenced by a given <see cref="SemanticModel"/>.
        /// </summary>
        /// <param name="semanticModel">The target <see cref="SemanticModel"/>.</param>
        /// <returns>All <see cref="TypeReference"/>s referenced by <paramref name="semanticModel"/>.</returns>
        private async Task<HashSet<TypeReference>> getReferencedTypesAsync(SemanticModel semanticModel)
        {
            var result = new HashSet<TypeReference>();

            var root = await semanticModel.SyntaxTree.GetRootAsync();
            var descendantNodes = root.DescendantNodes(n =>
            {
                var kind = n.Kind();

                // Ignored:
                // - Entire using lines.
                // - Namespace names (not entire namespaces).
                // - Entire static classes.

                return kind != SyntaxKind.UsingDirective
                       && kind != SyntaxKind.NamespaceKeyword
                       && (kind != SyntaxKind.ClassDeclaration || ((ClassDeclarationSyntax)n).Modifiers.All(m => m.Kind() != SyntaxKind.StaticKeyword));
            });

            // Find all the named type symbols in the syntax tree, and mark + recursively iterate through them.
            foreach (var node in descendantNodes)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.GenericName:
                    case SyntaxKind.IdentifierName:
                    {
                        if (semanticModel.GetSymbolInfo(node).Symbol is INamedTypeSymbol t)
                            addTypeSymbol(t);
                        break;
                    }

                    case SyntaxKind.AsExpression:
                    case SyntaxKind.IsExpression:
                    case SyntaxKind.SizeOfExpression:
                    case SyntaxKind.TypeOfExpression:
                    case SyntaxKind.CastExpression:
                    case SyntaxKind.ObjectCreationExpression:
                    {
                        if (semanticModel.GetTypeInfo(node).Type is INamedTypeSymbol t)
                            addTypeSymbol(t);
                        break;
                    }
                }
            }

            return result;

            void addTypeSymbol(INamedTypeSymbol typeSymbol)
            {
                // Enums, interfaces, and abstract classes typically break class isolation models and cause too many files to recompile.
                if (typeSymbol.TypeKind == TypeKind.Enum || typeSymbol.TypeKind == TypeKind.Interface || typeSymbol.IsAbstract)
                {
                    logger.Add($"Type {typeSymbol.Name} is an enum, interface, or abstract class and has been excluded.");
                    return;
                }

                var reference = TypeReference.FromSymbol(typeSymbol);

                // Exclude all Game types as they are overly far-reaching.
                if (typeInheritsFromGame(reference))
                {
                    logger.Add($"Type {typeSymbol.Name} inherits from game and is marked for exclusion.");
                    return;
                }

                // Exclude types marked with the [ExcludeFromDynamicCompile] attribute.
                // When multiple types exist in one file, the exclusion attribute may be omitted from some types, causing references to those types to indirectly compile explicitly excluded types.
                // If this type hasn't been seen before, do a manual pass over all its syntaxes to determine if an exclusion attribute is present anywhere in the file.
                if (!referenceMap.ContainsKey(reference))
                {
                    foreach (var syntax in typeSymbol.DeclaringSyntaxReferences)
                    {
                        if (!syntaxExclusionMap.TryGetValue(syntax.SyntaxTree.FilePath, out bool containsExclusion))
                            containsExclusion = syntaxExclusionMap[syntax.SyntaxTree.FilePath] = syntax.SyntaxTree.ToString().Contains(exclude_attribute_name);

                        if (containsExclusion)
                        {
                            logger.Add($"Type {typeSymbol.Name} referenced but marked for exclusion.");
                            return;
                        }
                    }
                }

                result.Add(reference);
            }
        }

        /// <summary>
        /// Traverses <see cref="referenceMap"/> to build a directed graph of <see cref="DirectedTypeNode"/> joined by their parents.
        /// </summary>
        /// <returns>A dictionary containing the directed graph from each <see cref="TypeReference"/> in <see cref="referenceMap"/>.</returns>
        private Dictionary<TypeReference, DirectedTypeNode> getDirectedGraph()
        {
            // Given the reference map (from above):
            //
            // P -> { C1, C2 }
            // C1 -> { C3, C4 }
            // C2 -> { C5, C6 }
            // C3 -> { }
            // C4 -> { C6 }
            // C5 -> { C6 }
            // C6 -> { C2 }
            //
            // The respective directed graph is built by traversing upwards and finding all incoming references at each type, such that:
            //
            // P -> { }
            // C1 -> { P }
            // C2 -> { C6, P, C5, C4, C2, C1 }
            // C3 -> { C1, P }
            // C4 -> { C1, P }
            // C5 -> { C2, P }
            // C6 -> { C5, C4, C2, C1, C6, P }
            //
            // The directed graph may contain cycles where multiple paths lead to the same node (e.g. C2, C6).

            logger.Add("Retrieving reference graph...");

            var result = new Dictionary<TypeReference, DirectedTypeNode>();

            // Traverse through the reference map and assign parents to all children referenced types.
            foreach (var kvp in referenceMap)
            {
                var parentNode = getNode(kvp.Key);
                foreach (var typeRef in kvp.Value)
                    getNode(typeRef).Parents.Add(parentNode);
            }

            return result;

            DirectedTypeNode getNode(TypeReference typeSymbol)
            {
                if (!result.TryGetValue(typeSymbol, out var existing))
                    result[typeSymbol] = existing = new DirectedTypeNode(typeSymbol);
                return existing;
            }
        }

        /// <summary>
        /// Traverses a directed graph to find all direct and indirect references to a set of <see cref="TypeReference"/>s. References are returned as file names.
        /// </summary>
        /// <param name="sources">The <see cref="TypeReference"/>s to search from.</param>
        /// <param name="directedGraph">The directed graph generated through <see cref="getDirectedGraph"/>.</param>
        /// <returns>All files containing direct or indirect references to the given <paramref name="sources"/>.</returns>
        private HashSet<string> getReferencedFiles(IEnumerable<TypeReference> sources, IReadOnlyDictionary<TypeReference, DirectedTypeNode> directedGraph)
        {
            logger.Add("Retrieving referenced files...");

            var result = new HashSet<string>();

            foreach (var s in sources)
                getReferencedFilesRecursive(directedGraph[s], result);

            return result;
        }

        private void getReferencedFilesRecursive(DirectedTypeNode node, HashSet<string> result, HashSet<DirectedTypeNode> seenTypes = null,
                                                 int level = 0)
        {
            // A '.' is prepended since the logger trims lines.
            logger.Add($"{(level > 0 ? $".{new string(' ', level * 2 - 1)}| " : string.Empty)}: {node}");

            // Don't go through duplicate nodes (multiple references from different types).
            seenTypes ??= new HashSet<DirectedTypeNode>();
            if (seenTypes.Contains(node))
                return;

            seenTypes.Add(node);

            // Add all the current type's locations to the resulting set.
            foreach (var location in node.Reference.Symbol.Locations)
            {
                var syntaxTree = location.SourceTree;
                if (syntaxTree != null)
                    result.Add(syntaxTree.FilePath);
            }

            // Follow through the process for all parents.
            foreach (var p in node.Parents)
                getReferencedFilesRecursive(p, result, seenTypes, level + 1);
        }

        private bool typeInheritsFromGame(TypeReference reference)
        {
            if (typeInheritsFromGameCache.TryGetValue(reference, out var existing))
                return existing;

            // When used via a nuget package, the local type name seems to always be more qualified than the symbol's type name.
            // E.g. Type name: osu.Framework.Game, symbol name: Framework.Game.
            if (typeof(Game).FullName?.Contains(reference.Symbol.ToString()) == true)
                return typeInheritsFromGameCache[reference] = true;

            if (reference.Symbol.BaseType == null)
                return typeInheritsFromGameCache[reference] = false;

            return typeInheritsFromGameCache[reference] = typeInheritsFromGame(TypeReference.FromSymbol(reference.Symbol.BaseType));
        }

        /// <summary>
        /// Finds all the <see cref="TypeReference"/>s which list a given filename as any of their sources.
        /// </summary>
        /// <param name="fileName">The target filename.</param>
        /// <returns>All <see cref="TypeReference"/>s with <paramref name="fileName"/> listed as one of their symbol locations.</returns>
        private IEnumerable<TypeReference> getTypesFromFile(string fileName) => referenceMap
                                                                                .Select(kvp => kvp.Key)
                                                                                .Where(t => t.Symbol.Locations.Any(l => l.SourceTree?.FilePath == fileName));

        /// <summary>
        /// Compiles a <see cref="Project"/>.
        /// </summary>
        /// <param name="project">The <see cref="Project"/> to compile.</param>
        /// <returns>The resulting <see cref="Compilation"/>.</returns>
        private async Task<Compilation> compileProjectAsync(Project project)
        {
            if (compilationCache.TryGetValue(project, out var existing))
                return existing;

            logger.Add($"Compiling project {project.Name}...");
            return compilationCache[project] = await project.GetCompilationAsync();
        }

        /// <summary>
        /// Retrieves a <see cref="SemanticModel"/> from a given <see cref="SyntaxTree"/>.
        /// </summary>
        /// <param name="syntaxTree">The target <see cref="SyntaxTree"/>.</param>
        /// <returns>The corresponding <see cref="SemanticModel"/>.</returns>
        private async Task<SemanticModel> getSemanticModelAsync(SyntaxTree syntaxTree)
        {
            if (semanticModelCache.TryGetValue(syntaxTree, out var existing))
                return existing;

            return semanticModelCache[syntaxTree] = (await compileProjectAsync(getProjectFromFile(syntaxTree.FilePath))).GetSemanticModel(syntaxTree, true);
        }

        /// <summary>
        /// Retrieves the <see cref="Project"/> which contains a given filename as a document.
        /// </summary>
        /// <param name="fileName">The target filename.</param>
        /// <returns>The <see cref="Project"/> that contains <paramref name="fileName"/>.</returns>
        private Project getProjectFromFile(string fileName) => solution.Projects.FirstOrDefault(p => p.Documents.Any(d => d.FilePath == fileName));

        /// <summary>
        /// Retrieves the project which contains the currently-executing test.
        /// </summary>
        /// <returns>The <see cref="Project"/> containing the currently-executing test.</returns>
        private Project findTestProject()
        {
            var executingAssembly = Assembly.GetEntryAssembly()?.GetName().Name;
            return solution.Projects.FirstOrDefault(p => p.AssemblyName == executingAssembly);
        }

        private void clearCaches()
        {
            compilationCache.Clear();
            semanticModelCache.Clear();
            syntaxExclusionMap.Clear();
        }

        /// <summary>
        /// Updates a file in the solution with its new on-disk contents.
        /// </summary>
        /// <param name="fileName">The file to update.</param>
        private void updateFile(string fileName)
        {
            logger.Add($"Updating file {fileName} in solution...");

            var changedDoc = solution.GetDocumentIdsWithFilePath(fileName)[0];
            solution = solution.WithDocumentText(changedDoc, SourceText.From(File.ReadAllText(fileName)));
        }

        /// <summary>
        /// Wraps a <see cref="INamedTypeSymbol"/> for stable inter-<see cref="Compilation"/> hashcode and equality comparisons.
        /// </summary>
        private readonly struct TypeReference : IEquatable<TypeReference>
        {
            public readonly INamedTypeSymbol Symbol;

            public TypeReference(INamedTypeSymbol symbol)
            {
                Symbol = symbol;
            }

            public bool Equals(TypeReference other)
                => Symbol.ContainingNamespace.ToString() == other.Symbol.ContainingNamespace.ToString()
                   && Symbol.ToString() == other.Symbol.ToString();

            public override int GetHashCode()
            {
                var hash = new HashCode();
                hash.Add(Symbol.ToString(), StringComparer.Ordinal);
                return hash.ToHashCode();
            }

            public override string ToString() => Symbol.ToString();

            public static TypeReference FromSymbol(INamedTypeSymbol symbol) => new TypeReference(symbol);
        }

        /// <summary>
        /// A single node in the directed graph of <see cref="TypeReference"/>s, linked upwards by its parenting <see cref="DirectedTypeNode"/>.
        /// </summary>
        private class DirectedTypeNode : IEquatable<DirectedTypeNode>
        {
            public readonly TypeReference Reference;
            public readonly List<DirectedTypeNode> Parents = new List<DirectedTypeNode>();

            public DirectedTypeNode(TypeReference reference)
            {
                Reference = reference;
            }

            public bool Equals(DirectedTypeNode other)
                => other != null
                   && Reference.Equals(other.Reference);

            public override int GetHashCode() => Reference.GetHashCode();

            public override string ToString() => Reference.ToString();
        }
    }
}
#endif
