using System.Diagnostics;

using Microsoft.CodeAnalysis;

namespace Terrajobst.ApiCatalog;

public static class FrameworkIndexer
{
    public static FrameworkEntry Index(string frameworkName,
                                       IEnumerable<FrameworkAssembly> assemblies,
                                       Dictionary<string, MetadataReference> assemblyByPath,
                                       Dictionary<string, AssemblyEntry> assemblyEntryByPath)
    {
        var references = new List<MetadataReference>();
        var frameworkAssemblyByPath = new Dictionary<string, FrameworkAssembly>();

        foreach (var assembly in assemblies)
        {
            if (!assemblyByPath.TryGetValue(assembly.Path, out var metadata))
            {
                metadata = MetadataReference.CreateFromFile(assembly.Path);
                assemblyByPath.Add(assembly.Path, metadata);
            }

            references.Add(metadata);
            frameworkAssemblyByPath.Add(assembly.Path, assembly);
        }

        var metadataContext = MetadataContext.Create(references);

        var assemblyEntries = new List<FrameworkAssemblyEntry>();

        foreach (var assembly in metadataContext.Assemblies)
        {
            var metadata = metadataContext.Compilation.GetMetadataReference(assembly) as PortableExecutableReference;
            Debug.Assert(metadata is not null);

            var path = metadata.FilePath;
            Debug.Assert(path is not null);

            if (!assemblyEntryByPath.TryGetValue(path, out var entry))
            {
                entry = AssemblyEntry.Create(assembly);
                assemblyEntryByPath.Add(path, entry);
            }

            if (entry.Apis.Any())
            {
                var frameworkAssembly = frameworkAssemblyByPath[path];
                assemblyEntries.Add(new FrameworkAssemblyEntry(frameworkAssembly.PackName, frameworkAssembly.Profiles, entry));
            }
        }

        return FrameworkEntry.Create(frameworkName, assemblyEntries);
    }
}
