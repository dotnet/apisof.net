using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

namespace ApiCatalog
{
    public static class FrameworkIndexer
    {
        public static async Task<FrameworkEntry> Index(string frameworkName, FileSet fileSet)
        {
            var references = new List<MetadataReference>();

            foreach (var (path, stream) in fileSet.GetFiles())
            {
                var metadata = await AssemblyStream.CreateAsync(stream, path);
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
}
