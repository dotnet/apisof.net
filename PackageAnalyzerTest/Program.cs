using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using NuGet.Frameworks;
using NuGet.Packaging;

namespace PackageAnalyzerTest
{
    class Program
    {
        private static readonly string v3_flatContainer_nupkg_template = "https://api.nuget.org/v3-flatcontainer/{0}/{1}/{0}.{1}.nupkg";
        private static readonly HttpClient httpClient = new HttpClient();

        private static readonly string[] frameworkNames = new[] {

            // .NET Framework
            "net10",
            "net11",
            "net20",
            "net30",
            "net35",
            "net40",
            "net45",
            "net451",
            "net452",
            "net46",
            "net461",
            "net462",
            "net47",
            "net471",
            "net472",
            "net48",

            // .NET Standard
            "netstandard1.0",
            "netstandard1.1",
            "netstandard1.2",
            "netstandard1.3",
            "netstandard1.4",
            "netstandard1.5",
            "netstandard1.6",
            "netstandard2.0",
            "netstandard2.1",

            // .NET Core
            "netcoreapp1.0",
            "netcoreapp1.1",
            "netcoreapp2.0",
            "netcoreapp2.1",
            "netcoreapp2.2",
            "netcoreapp3.0",
            "netcoreapp3.1",
            "net5.0"
        };

        static async Task Main(string[] args)
        {
            //var id = "System.Collections.Immutable";
            //var version = "5.0.0-preview.7.20364.11";

            var id = "System.Memory";
            var version = "4.5.4";
            await IndexPackage(id, version);
        }

        private static async Task IndexPackage(string id, string version)
        {
            var frameworks = frameworkNames.Select(NuGetFramework.Parse);

            var frameworkGroups = frameworks.GroupBy(fx => fx.Framework)
                                            .Select(g => (Framework: g.Key, Versions: g.OrderByDescending(fx => fx.Version)))
                                            .ToArray();

            var dependencies = new List<PackageArchiveReader>();
            try
            {
                using (var root = await FetchPackageAsync(id, version))
                {
                    var targets = new List<NuGetFramework>();

                    foreach (var frameworkGroup in frameworkGroups)
                    {
                        foreach (var frameworkVersion in frameworkGroup.Versions)
                        {
                            var versionReferenceGroup = GetReferenceItems(root, frameworkVersion);
                            if (versionReferenceGroup != null)
                            {
                                targets.Add(versionReferenceGroup.TargetFramework);
                                break;
                            }
                        }
                    }

                    foreach (var target in targets)
                    {
                        Console.WriteLine(target);

                        var referenceGroup = GetReferenceItems(root, target);
                        Debug.Assert(referenceGroup != null);
                        await FetchDependenciesAsync(dependencies, root, target);

                        Console.WriteLine("Index:");

                        foreach (var path in referenceGroup.Items)
                            Console.WriteLine($"    {path,-70}{referenceGroup.TargetFramework.GetShortFolderName()}");

                        Console.WriteLine("Dependencies:");

                        foreach (var dependency in dependencies)
                        {
                            var dependencyReferences = GetReferenceItems(dependency, target);
                            if (dependencyReferences != null)
                            {
                                foreach (var path in dependencyReferences.Items)
                                    Console.WriteLine($"    {path,-70}{dependencyReferences.TargetFramework.GetShortFolderName()}");
                            }
                        }

                        // Add references to index

                        var compilation = CSharpCompilation.Create("dummy");
                        var toBeIndexed = new List<MetadataReference>();

                        foreach (var path in referenceGroup.Items)
                        {
                            var metadata = CreateReference(root.GetStream(path), filePath: path);
                            toBeIndexed.Add(metadata);
                            compilation = compilation.AddReferences(metadata);
                        }

                        // Add dependencies to index

                        foreach (var dependency in dependencies)
                        {
                            var dependencyReferences = GetReferenceItems(dependency, target);
                            if (dependencyReferences != null)
                            {
                                foreach (var path in dependencyReferences.Items)
                                {
                                    var metadata = CreateReference(dependency.GetStream(path), filePath: path);
                                    compilation = compilation.AddReferences(metadata);
                                }
                            }
                        }

                        // Add framework references

                        var plaformSet = await GetPlatformSet(target);
                        foreach (var (path, stream) in plaformSet.GetFiles())
                        {
                            var metadata = CreateReference(stream, filePath: path);
                            compilation = compilation.AddReferences(metadata);
                        }

                        foreach (var reference in toBeIndexed)
                        {
                            var assemblyOrModule = compilation.GetAssemblyOrModuleSymbol(reference);
                            if (assemblyOrModule is IAssemblySymbol a)
                            {
                                DumpAllTypes(a);
                            }
                        }
                    }
                }
            }
            finally
            {
                foreach (var package in dependencies)
                    package.Dispose();
            }
        }

        private static FrameworkSpecificGroup GetReferenceItems(PackageArchiveReader root, NuGetFramework current)
        {
            var referenceItems = root.GetReferenceItems();
            var referenceGroup = NuGetFrameworkUtility.GetNearest(referenceItems, current);
            return referenceGroup;
        }

        private static async Task FetchDependenciesAsync(List<PackageArchiveReader> packages, PackageArchiveReader root, NuGetFramework target)
        {
            var dependencies = root.GetPackageDependencies();
            var dependencyGroup = NuGetFrameworkUtility.GetNearest(dependencies, target);
            if (dependencyGroup != null)
            {
                foreach (var d in dependencyGroup.Packages)
                {
                    var dependency = await FetchPackageAsync(d.Id, d.VersionRange.MinVersion.ToNormalizedString());
                    packages.Add(dependency);
                    await FetchDependenciesAsync(packages, dependency, target);
                }
            }
        }

        private static async Task<PackageArchiveReader> FetchPackageAsync(string id, string version)
        {
            var url = GetFlatContainerNupkgUrl(id, version);
            var nupkgStream = await httpClient.GetStreamAsync(url);
            return new PackageArchiveReader(nupkgStream);
        }

        private static Uri GetFlatContainerNupkgUrl(string id, string version)
        {
            var url = string.Format(v3_flatContainer_nupkg_template, id, version);
            return new Uri(url);
        }

        private static Task<PlatformPackageSet> GetPlatformSet(NuGetFramework framework)
        {
            if (framework.Framework == ".NETCoreApp")
                return GetNetCore();
            else if (framework.Framework == ".NETStandard")
                return GetNetStandard();
            else if (framework.Framework == ".NETFramework")
                return GetNetFramework();
            else
                return Task.FromResult<PlatformPackageSet>(null);
        }

        private static async Task<PlatformPackageSet> GetNetCore()
        {
            return new PlatformPackageSet
            {
                Packages = new[]
                {
                    await FetchPackageAsync("Microsoft.AspNetCore.App.Ref", "3.1.0"),
                    await FetchPackageAsync("Microsoft.NETCore.App.Ref", "3.1.0"),
                    await FetchPackageAsync("Microsoft.WindowsDesktop.App.Ref", "3.1.0")
                },
                Selector = pr => pr.GetFiles()
                                   .Where(path => path.StartsWith("ref/", StringComparison.OrdinalIgnoreCase) &&
                                                  path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                                   .Select(p => (p, pr.GetStream(p)))
            };
        }

        private static async Task<PlatformPackageSet> GetNetStandard()
        {
            return new PlatformPackageSet
            {
                Packages = new[]
                {
                    await FetchPackageAsync("NETStandard.Library.Ref", "2.1.0")
                },
                Selector = pr => pr.GetFiles()
                                   .Where(path => path.StartsWith("ref/", StringComparison.OrdinalIgnoreCase) &&
                                                  path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                                   .Select(p => (p, pr.GetStream(p)))
            };
        }

        private static async Task<PlatformPackageSet> GetNetFramework()
        {
            return new PlatformPackageSet
            {
                Packages = new[]
                {
                    await FetchPackageAsync("Microsoft.NETFramework.ReferenceAssemblies.net48", "1.0.0")
                },
                Selector = pr => pr.GetFiles()
                                   .Where(path => path.StartsWith("build/.NETFramework/", StringComparison.OrdinalIgnoreCase) &&
                                                  path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                                   .Select(p => (p, pr.GetStream(p)))
            };
        }

        private static void DumpAllTypes(IAssemblySymbol a)
        {
            var apis = GetApis(a);
            DumpAllApis(apis);
        }

        private static void DumpAllApis(IEnumerable<ApiEntry> entries, int indent = 0)
        {
            var x = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType
            );

            var indentStr = new string(' ', indent * 2);
            foreach (var entry in entries)
            {               
                var name = entry.Symbol.ToDisplayString(x);
                if (entry.Symbol is INamespaceSymbol ns)
                    name = ns.ToString();

                Console.WriteLine($"{indentStr}{name}");
                DumpAllApis(entry.Children, indent + 1);
            }
        }

        private static IEnumerable<INamedTypeSymbol> GetAllTypes(IAssemblySymbol symbol)
        {
            var stack = new Stack<INamespaceSymbol>();
            stack.Push(symbol.GlobalNamespace);

            while (stack.Count > 0)
            {
                var ns = stack.Pop();
                foreach (var member in ns.GetMembers())
                {
                    if (member is INamespaceSymbol childNs)
                        stack.Push(childNs);
                    else if (member is INamedTypeSymbol type)
                        yield return type;
                }
            }
        }

        private static List<ApiEntry> GetApis(IAssemblySymbol symbol)
        {
            var result = new List<ApiEntry>();
            var types = GetAllTypes(symbol)
                         .Where(t => t.DeclaredAccessibility == Accessibility.Public)
                         .GroupBy(t => t.ContainingNamespace);

            foreach (var namespaceGroup in types)
            {
                var entry = new ApiEntry(namespaceGroup.Key);
                result.Add(entry);

                foreach (var type in namespaceGroup)
                    AddApi(entry.Children, type);
            }

            return result;
        }

        private static void AddApi(List<ApiEntry> target, ITypeSymbol symbol)
        {
            if (symbol.DeclaredAccessibility != Accessibility.Public &&
                symbol.DeclaredAccessibility != Accessibility.Protected)
                return;

            var apiEntry = new ApiEntry(symbol);
            target.Add(apiEntry);

            foreach (var member in symbol.GetMembers())
                AddMember(apiEntry.Children, member);
        }

        private static void AddMember(List<ApiEntry> target, ISymbol symbol)
        {
            if (symbol is INamedTypeSymbol type)
            {
                AddApi(target, type);
                return;
            }

            if (symbol.DeclaredAccessibility != Accessibility.Public &&
                symbol.DeclaredAccessibility != Accessibility.Protected)
                return;

            var entry = new ApiEntry(symbol);
            target.Add(entry);
        }

        private static MetadataReference CreateReference(Stream stream, string filePath)
        {
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            memoryStream.Position = 0;
            return MetadataReference.CreateFromStream(memoryStream, filePath: filePath);
        }
    }

    class PlatformPackageSet
    {
        public PackageArchiveReader[] Packages;
        public Func<PackageArchiveReader, IEnumerable<(string Path, Stream data)>> Selector;

        public IEnumerable<(string Path, Stream data)> GetFiles()
        {
            return Packages.Select(Selector).SelectMany(s => s);
        }
    }

    class ApiEntry
    {
        public ApiEntry(ISymbol symbol)
        {
            Symbol = symbol;
        }

        public ISymbol Symbol { get; set; }
        public List<ApiEntry> Children { get; } = new List<ApiEntry>();
    }
}
