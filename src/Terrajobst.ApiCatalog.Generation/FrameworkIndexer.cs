using System.Diagnostics;

using Microsoft.CodeAnalysis;

namespace Terrajobst.ApiCatalog;

public static class FrameworkIndexer
{
    public static FrameworkEntry Index(string frameworkName,
                                       IEnumerable<string> assemblyPaths,
                                       Dictionary<string, MetadataReference> assemblyByPath,
                                       Dictionary<string, AssemblyEntry> assemblyEntryByPath)
    {
        var references = new List<MetadataReference>();

        foreach (var path in assemblyPaths)
        {
            if (!assemblyByPath.TryGetValue(path, out var metadata))
            {
                metadata = MetadataReference.CreateFromFile(path);
                assemblyByPath.Add(path, metadata);
            }

            references.Add(metadata);
        }

        var metadataContext = MetadataContext.Create(references);

        var assemblyEntries = new List<AssemblyEntry>();

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
                assemblyEntries.Add(entry);
        }

        return FrameworkEntry.Create(frameworkName, assemblyEntries);
    }
}