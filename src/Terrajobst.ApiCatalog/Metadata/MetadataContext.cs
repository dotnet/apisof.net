using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Terrajobst.ApiCatalog;

public sealed class MetadataContext
{
    private MetadataContext(CSharpCompilation compilation,
        ImmutableArray<MetadataReference> assemblies,
        ImmutableArray<MetadataReference> dependencies)
    {
        Assemblies = assemblies.Select(r => compilation.GetAssemblyOrModuleSymbol(r)).OfType<IAssemblySymbol>().ToImmutableArray();
        Dependencies = dependencies.Select(r => compilation.GetAssemblyOrModuleSymbol(r)).OfType<IAssemblySymbol>().ToImmutableArray();
    }

    public ImmutableArray<IAssemblySymbol> Assemblies { get; }
    public ImmutableArray<IAssemblySymbol> Dependencies { get; }

    public static MetadataContext Create(IEnumerable<MetadataReference> assemblies)
    {
        return Create(assemblies, Enumerable.Empty<MetadataReference>());
    }

    public static MetadataContext Create(IEnumerable<MetadataReference> assemblies,
        IEnumerable<MetadataReference> dependencies)
    {
        var capturedAssemblies = assemblies.ToImmutableArray();
        var capturedDependencies = dependencies.ToImmutableArray();
        var allReferences = capturedAssemblies.AddRange(capturedDependencies);
        var compilation = CSharpCompilation.Create("dummy", references: allReferences);
        return new MetadataContext(compilation, capturedAssemblies, capturedDependencies);
    }
}