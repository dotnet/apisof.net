using System.Text.Json;
using System.Text.Json.Serialization;
using Terrajobst.ApiCatalog.PackManifest.Models;
namespace Terrajobst.ApiCatalog;

public static class FrameworkDefinitionExtension
{
    public static IReadOnlyList<FrameworkDefinition> LoadDumpPackManifest(this IReadOnlyList<FrameworkDefinition> frameworks)
    {
        var jsonFile = ResolveDumpPackManifestPath();

        if (jsonFile is null)
            return [];

        var jsonContent = File.ReadAllText(jsonFile);
        var settings = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        settings.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        var manifest = JsonSerializer.Deserialize<DumpPackManifest>(jsonContent, settings);
        if (manifest is null)
            throw new InvalidDataException($"Pack manifest '{jsonFile}' did not contain a manifest.");

        if (manifest.Errors.Count > 0)
        {
            var errors = string.Join(Environment.NewLine,
                                     manifest.Errors.Select(e => $"[{e.Severity}] {e.Error}"));
            throw new InvalidDataException($"Pack manifest '{jsonFile}' contains errors:{Environment.NewLine}{errors}");
        }

        var result = new List<FrameworkDefinition>();
        var predefinedFrameworkNames = frameworks.Select(f => f.Name)
                               .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var frameworkVersions = manifest.WorkloadPackManifests
            .Select(w => w.DotNetVersion)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var frameworkVersion in frameworkVersions)
        {
            if (predefinedFrameworkNames.Contains(frameworkVersion))
                continue;

            var builtInPacks = ConvertBuiltInPacks(frameworkVersion, manifest.BuiltInPackManifests);
            var workloadManifest = manifest.WorkloadPackManifests
                .FirstOrDefault(w => frameworkVersion.Equals(w.DotNetVersion, StringComparison.OrdinalIgnoreCase));
            var workloadPacks = ConvertWorkloadPacks(workloadManifest);

            if (builtInPacks.Count == 0 && workloadPacks.Count == 0)
                continue;

            var supportedPlatforms = ConvertSupportedPlatforms(frameworkVersion, manifest.BuiltInPackManifests, workloadManifest, workloadPacks);

            var frameworkDefinition = new FrameworkDefinition(frameworkVersion)
            {
                SupportedPlatforms = supportedPlatforms,
                BuiltInPacks = builtInPacks,
                WorkloadPacks = workloadPacks
            };

            result.Add(frameworkDefinition);
        }

        return result;

        static IReadOnlyList<PackReference> ConvertBuiltInPacks(string tfm, IEnumerable<BuiltInPackManifest> manifests)
        {
            var packs = new List<PackReference>();
            foreach (var manifest in manifests)
            {
                var references = manifest.FrameworkReferences
                    .Where(r => r.TargetFramework.Equals(tfm, StringComparison.OrdinalIgnoreCase) ||
                                r.TargetFramework.StartsWith(tfm + "-", StringComparison.OrdinalIgnoreCase));

                foreach (var reference in references)
                {
                    foreach (var pack in reference.Packs)
                    {
                        packs.Add(new PackReference(pack.PackName)
                        {
                            Version = pack.PackVersion,
                            Kind = PackKind.Framework,
                            Platforms = string.IsNullOrEmpty(reference.Platform) ? [] : [reference.Platform]
                        });
                    }
                }
            }

            return packs.GroupBy(p => (p.Name, p.Version, Platform: string.Join("|", p.Platforms)))
                        .Select(g => g.First())
                        .ToArray();
        }

        static IReadOnlyList<PackReference> ConvertWorkloadPacks(WorkloadPackManifest? manifest)
        {
            if (manifest is null)
                return [];

            var packs = new List<PackReference>();

            foreach (var pack in manifest.Packs)
            {
                if (!Enum.TryParse<PackKind>(pack.PackKind, ignoreCase: true, out var kind))
                    continue;

                // Library packs must not list platforms in FrameworkDefinition.
                var platforms = kind == PackKind.Framework
                    ? pack.Platforms.ToArray()
                    : [];

                packs.Add(new PackReference(pack.PackName)
                {
                    Version = pack.PackVersion,
                    Kind = kind,
                    Platforms = platforms,
                    Workloads = [..pack.WorkloadNames]
                });
            }

            return packs;
        }

        static IReadOnlyList<FrameworkPlatformDefinition> ConvertSupportedPlatforms(string tfm,
                                                                                    IEnumerable<BuiltInPackManifest> builtInManifests,
                                                                                    WorkloadPackManifest? workloadManifest,
                                                                                    IReadOnlyList<PackReference> workloadPacks)
        {
            IEnumerable<PlatformVersion>? source = null;

            if ((workloadManifest?.PlatformVersions?.Count ?? 0) > 0)
            {
                source = workloadManifest!.PlatformVersions;
            }
            else
            {
                var firstBuiltInWithTfm = builtInManifests.FirstOrDefault(m =>
                    m.FrameworkReferences.Any(r => r.TargetFramework.Equals(tfm, StringComparison.OrdinalIgnoreCase) ||
                                                   r.TargetFramework.StartsWith(tfm + "-", StringComparison.OrdinalIgnoreCase)));

                source = firstBuiltInWithTfm?.PlatformVersions;
            }

            if (source is null)
                return [];

            var versionByPlatform = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var platform in source)
            {
                var normalizedVersions = platform.Versions
                    .Select(v => NormalizePlatformVersion(platform.Platform, v))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (normalizedVersions.Length == 0)
                    continue;

                versionByPlatform[platform.Platform.ToLowerInvariant()] = new HashSet<string>(normalizedVersions, StringComparer.OrdinalIgnoreCase);
            }

            var result = new List<FrameworkPlatformDefinition>();

            foreach (var (platform, versions) in versionByPlatform)
            {
                if (versions.Count == 0)
                    continue;

                result.Add(new FrameworkPlatformDefinition(platform)
                {
                    Versions = versions.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToArray()
                });
            }

            return result;
        }

        static string NormalizePlatformVersion(string platform, string version)
        {
            if (platform.Equals("Windows", StringComparison.OrdinalIgnoreCase) && Version.TryParse(version, out var parsed))
            {
                if (parsed.Build >= 0)
                    return $"{parsed.Major}.{parsed.Minor}.{parsed.Build}";

                return $"{parsed.Major}.{parsed.Minor}";
            }

            return version;
        }

        static string? ResolveDumpPackManifestPath()
        {
            var candidateRoots = new[]
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            };

            foreach (var root in candidateRoots)
            {
                var current = new DirectoryInfo(root);

                while (current is not null)
                {
                    var srcDumpPacksPath = Path.Combine(current.FullName, "src", "DumpPacks", "dumppack_output.json");
                    if (File.Exists(srcDumpPacksPath))
                        return srcDumpPacksPath;

                    // Fallback for local ad-hoc runs directly from src/DumpPacks.
                    var directPath = Path.Combine(current.FullName, "dumppack_output.json");
                    if (File.Exists(directPath))
                        return directPath;

                    current = current.Parent;
                }
            }

            return null;
        }
    }
}