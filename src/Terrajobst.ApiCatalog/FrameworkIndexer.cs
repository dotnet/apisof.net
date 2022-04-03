using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Terrajobst.ApiCatalog;

namespace Terrajobst.ApiCatalog;

public static class FrameworkIndexer
{
    public static FrameworkEntry Index(string frameworkName, IEnumerable<string> assemblyPaths)
    {
        var references = new List<MetadataReference>();

        foreach (var path in assemblyPaths)
        {
            var metadata = MetadataReference.CreateFromFile(path);
            references.Add(metadata);
        }

        var metadataContext = MetadataContext.Create(references);

        var assemblyEntries = new List<AssemblyEntry>();

        foreach (var assembly in metadataContext.Assemblies)
        {
            var entry = AssemblyEntry.Create(assembly);
            if (entry.Apis.Any())
                assemblyEntries.Add(entry);
        }

        return FrameworkEntry.Create(frameworkName, assemblyEntries);
    }
}