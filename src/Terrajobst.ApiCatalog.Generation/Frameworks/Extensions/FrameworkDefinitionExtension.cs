using System.Text.Json;
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

        var manifest = JsonSerializer.Deserialize<DumpPackManifest>(jsonContent, settings);
        if (manifest is null)
            return [];

        var result = new List<FrameworkDefinition>();

        var frameworkVersions = manifest.WorkloadPackManifests
            .Select(w => w.DotNetVersion)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var frameworkVersion in frameworkVersions)
        {
            if (IsFrameworkInPredefinedList(frameworkVersion))
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
                    var platform = reference.TargetFramework.StartsWith(tfm + "-", StringComparison.OrdinalIgnoreCase)
                        ? reference.TargetFramework[(tfm.Length + 1)..]
                        : "";

                    foreach (var pack in reference.Packs)
                    {
                        var computedPlatform = string.IsNullOrEmpty(platform)
                            ? InferBuiltInPlatform(pack.PackName)
                            : platform;

                        packs.Add(new PackReference(pack.PackName)
                        {
                            Version = pack.PackVersion,
                            Kind = PackKind.Framework,
                            Platforms = [computedPlatform]
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
                    ? InferWorkloadPlatforms(pack.PackName, pack.WorkloadNames).ToArray()
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

            // Add missing platforms referenced by framework workload packs and infer at least one version.
            foreach (var pack in workloadPacks.Where(p => p.Kind == PackKind.Framework))
            {
                foreach (var platform in pack.Platforms)
                {
                    if (string.IsNullOrWhiteSpace(platform))
                        continue;

                    if (!versionByPlatform.TryGetValue(platform, out var versions))
                    {
                        versions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        versionByPlatform.Add(platform, versions);
                    }

                    if (TryInferPlatformVersion(platform, pack, out var inferredVersion))
                        versions.Add(inferredVersion);
                }
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

        static bool TryInferPlatformVersion(string platform, PackReference pack, out string version)
        {
            // iOS/macOS/MacCatalyst/tvOS packs often encode platform version in name: ...net10.0_26.2
            var underscoreIndex = pack.Name.LastIndexOf('_');
            if (underscoreIndex >= 0 && underscoreIndex < pack.Name.Length - 1)
            {
                var fromName = pack.Name[(underscoreIndex + 1)..];
                if (Version.TryParse(fromName, out var parsed))
                {
                    version = NormalizePlatformVersion(platform, fromName);
                    return true;
                }
            }

            // Android packs often encode API level in name: Microsoft.Android.Ref.36
            if (platform.Equals("android", StringComparison.OrdinalIgnoreCase))
            {
                var segments = pack.Name.Split('.');
                if (segments.Length > 0 && int.TryParse(segments[^1], out var apiLevel))
                {
                    version = $"{apiLevel}.0";
                    return true;
                }
            }

            // Fallback to major.minor of package version.
            if (Version.TryParse(pack.Version, out var packageVersion))
            {
                version = NormalizePlatformVersion(platform, $"{packageVersion.Major}.{packageVersion.Minor}");
                return true;
            }

            version = string.Empty;
            return false;
        }

        static string InferBuiltInPlatform(string packName)
        {
            if (packName.Equals("Microsoft.WindowsDesktop.App.Ref", StringComparison.OrdinalIgnoreCase))
                return "windows";

            return "";
        }

        static IEnumerable<string> InferWorkloadPlatforms(string packName, IEnumerable<string> workloads)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var workload in workloads)
            {
                if (workload.Contains("android", StringComparison.OrdinalIgnoreCase))
                    result.Add("android");
                if (workload.Contains("ios", StringComparison.OrdinalIgnoreCase))
                    result.Add("ios");
                if (workload.Contains("maccatalyst", StringComparison.OrdinalIgnoreCase))
                    result.Add("maccatalyst");
                if (workload.Contains("macos", StringComparison.OrdinalIgnoreCase))
                    result.Add("macos");
                if (workload.Contains("tvos", StringComparison.OrdinalIgnoreCase))
                    result.Add("tvos");
                if (workload.Contains("windows", StringComparison.OrdinalIgnoreCase))
                    result.Add("windows");
            }

            if (packName.Contains("Android", StringComparison.OrdinalIgnoreCase))
                result.Add("android");
            if (packName.Contains("iOS", StringComparison.OrdinalIgnoreCase))
                result.Add("ios");
            if (packName.Contains("MacCatalyst", StringComparison.OrdinalIgnoreCase))
                result.Add("maccatalyst");
            if (packName.Contains("macOS", StringComparison.OrdinalIgnoreCase))
                result.Add("macos");
            if (packName.Contains("tvOS", StringComparison.OrdinalIgnoreCase))
                result.Add("tvos");
            if (packName.Contains("Win", StringComparison.OrdinalIgnoreCase) ||
                packName.Contains("Windows", StringComparison.OrdinalIgnoreCase))
                result.Add("windows");

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

        static bool IsFrameworkInPredefinedList(string frameworkVersion)
        {
            return frameworkVersion.Equals("netcoreapp3.0", StringComparison.OrdinalIgnoreCase)
                || frameworkVersion.Equals("netcoreapp3.1", StringComparison.OrdinalIgnoreCase)
                || frameworkVersion.Equals("net5.0", StringComparison.OrdinalIgnoreCase)
                || frameworkVersion.Equals("net6.0", StringComparison.OrdinalIgnoreCase)
                || frameworkVersion.Equals("net7.0", StringComparison.OrdinalIgnoreCase)
                || frameworkVersion.Equals("net8.0", StringComparison.OrdinalIgnoreCase)
                || frameworkVersion.Equals("net9.0", StringComparison.OrdinalIgnoreCase);
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