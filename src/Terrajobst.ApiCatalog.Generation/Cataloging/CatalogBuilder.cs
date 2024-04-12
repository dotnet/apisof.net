using System.Collections.Frozen;
using System.Xml.Linq;

namespace Terrajobst.ApiCatalog;

public sealed partial class CatalogBuilder
{
    private readonly TextWriter _logger = Console.Out;
    private readonly List<IntermediaApi> _apis = new();
    private readonly List<IntermediaApi> _rootApis = new();
    private readonly Dictionary<Guid, IntermediaApi> _apiByFingerprint = new();
    private readonly List<IntermediaAssembly> _assemblies = new();
    private readonly Dictionary<Guid, IntermediaAssembly> _assemblyByFingerprint = new();
    private readonly Dictionary<string, IntermediateFramework> _frameworkByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, IntermediatePackage> _packageByFingerprint = new();
    private readonly SortedSet<string> _platformNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, (Guid, Guid)> _extensions = new();
    private readonly Dictionary<string, IntermediateUsageSource> _usageSources = new(StringComparer.OrdinalIgnoreCase);

    public void Index(string indexPath)
    {
        ThrowIfNullOrWhiteSpace(indexPath);

        var files = Directory.GetFiles(indexPath, "*.xml");

        foreach (var path in files)
        {
            Console.WriteLine($"Processing {path}...");
            var doc = XDocument.Load(path);
            IndexDocument(doc);
        }
    }

    public void IndexDocument(XDocument doc)
    {
        ThrowIfNull(doc);

        if (doc.Root is null or { IsEmpty: true })
            return;

        if (doc.Root.Name == "package")
            IndexPackage(doc);
        else if (doc.Root.Name == "framework")
            IndexFramework(doc);
        else
            throw new Exception($"Unexpected element: {doc.Root.Name}");
    }

    private void IndexFramework(XDocument doc)
    {
        var framework = doc.Root!.Attribute("name")!.Value;
        DefineFramework(framework);
        DefineApis(doc.Root.Elements("api"));
        DefineExtensions(doc.Root.Elements("extension"));

        foreach (var assemblyElement in doc.Root.Elements("assembly"))
        {
            var assemblyFingerprint = Guid.Parse(assemblyElement.Attribute("fingerprint")!.Value);
            var name = assemblyElement.Attribute("name")!.Value;
            var version = assemblyElement.Attribute("version")!.Value;
            var publicKeyToken = assemblyElement.Attribute("publicKeyToken")!.Value;
            var assemblyCreated = DefineAssembly(assemblyFingerprint, name, version, publicKeyToken);
            DefineFrameworkAssembly(framework, assemblyFingerprint);

            if (assemblyCreated)
            {
                foreach (var syntaxElement in assemblyElement.Elements("syntax"))
                {
                    var apiFingerprint = Guid.Parse(syntaxElement.Attribute("id")!.Value);
                    var syntax = syntaxElement.Value;
                    DefineDeclaration(assemblyFingerprint, apiFingerprint, syntax);
                }

                DefinePlatformSupport(assemblyFingerprint, assemblyElement);
                DefineObsoletions(assemblyFingerprint, assemblyElement);
                DefinePreviewRequirements(assemblyFingerprint, assemblyElement);
                DefineExperimentals(assemblyFingerprint, assemblyElement);
            }
        }
    }

    private void IndexPackage(XDocument doc)
    {
        var packageFingerprint = Guid.Parse(doc.Root!.Attribute("fingerprint")!.Value);
        var packageId = doc.Root.Attribute("id")!.Value;
        var packageName = doc.Root.Attribute("name")!.Value;
        DefinePackage(packageFingerprint, packageId, packageName);
        DefineApis(doc.Root.Elements("api"));
        DefineExtensions(doc.Root.Elements("extension"));

        foreach (var assemblyElement in doc.Root.Elements("assembly"))
        {
            var framework = assemblyElement.Attribute("fx")!.Value;
            var assemblyFingerprint = Guid.Parse(assemblyElement.Attribute("fingerprint")!.Value);
            var name = assemblyElement.Attribute("name")!.Value;
            var version = assemblyElement.Attribute("version")!.Value;
            var publicKeyToken = assemblyElement.Attribute("publicKeyToken")!.Value;
            DefineFramework(framework);
            var assemblyCreated = DefineAssembly(assemblyFingerprint, name, version, publicKeyToken);
            DefinePackageAssembly(packageFingerprint, framework, assemblyFingerprint);

            if (assemblyCreated)
            {
                foreach (var syntaxElement in assemblyElement.Elements("syntax"))
                {
                    var apiFingerprint = Guid.Parse(syntaxElement.Attribute("id")!.Value);
                    var syntax = syntaxElement.Value;
                    DefineDeclaration(assemblyFingerprint, apiFingerprint, syntax);
                }

                DefinePlatformSupport(assemblyFingerprint, assemblyElement);
                DefineObsoletions(assemblyFingerprint, assemblyElement);
                DefinePreviewRequirements(assemblyFingerprint, assemblyElement);
                DefineExperimentals(assemblyFingerprint, assemblyElement);
            }
        }
    }

    private void DefineApis(IEnumerable<XElement> apiElements)
    {
        foreach (var element in apiElements)
        {
            var fingerprint = Guid.Parse(element.Attribute("fingerprint")!.Value);
            var kind = (ApiKind)int.Parse(element.Attribute("kind")!.Value);
            var parentFingerprint = element.Attribute("parent") is null
                ? (Guid?)null
                : Guid.Parse(element.Attribute("parent")!.Value);
            var name = element.Attribute("name")!.Value;

            DefineApi(fingerprint, kind, parentFingerprint, name);
        }
    }

    public void IndexUsages(string path, string name, DateOnly date)
    {
        Console.WriteLine($"Processing {path}...");
        var usages = ParseFile(path);
        IndexUsages(name, date, usages);

        static IEnumerable<(Guid ApiFingerprint, float Percentage)> ParseFile(string path)
        {
            using var streamReader = new StreamReader(path);

            while (streamReader.ReadLine() is { } line)
            {
                var tabIndex = line.IndexOf('\t');
                var lastTabIndex = line.LastIndexOf('\t');
                if (tabIndex > 0 && tabIndex == lastTabIndex)
                {
                    var guidTextSpan = line.AsSpan(0, tabIndex);
                    var percentageSpan = line.AsSpan(tabIndex + 1);

                    if (Guid.TryParse(guidTextSpan, out var apiFingerprint) &&
                        float.TryParse(percentageSpan, out var percentage))
                    {
                        yield return (apiFingerprint, percentage);
                    }
                }
            }
        }
    }

    public void IndexUsages(string name, DateOnly date, IEnumerable<(Guid ApiFingerprint, float Percentage)> usages)
    {
        if (_usageSources.TryGetValue(name, out var existingUsages))
        {
            if (existingUsages.Date < date)
                _usageSources.Remove(name);
            else
                return;
        }

        var usageDictionary = usages.ToFrozenDictionary(t => t.ApiFingerprint, t => t.Percentage);
        var usageSource = new IntermediateUsageSource(name, date, usageDictionary);
        _usageSources.Add(usageSource.Name, usageSource);
    }

    public async Task<ApiCatalogModel> BuildAsync()
    {
        await using var stream = new MemoryStream();
        Build(stream);
        stream.Position = 0;
        return await ApiCatalogModel.LoadAsync(stream);
    }

    public void Build(string path)
    {
        ThrowIfNullOrWhiteSpace(path);

        using var stream = File.Create(path);
        Build(stream);
    }

    public void Build(Stream stream)
    {
        ThrowIfNull(stream);

        ResolveExtensions();
        var writer = new CatalogWriter(this);
        writer.Write(stream);
    }

    private void DefineApi(Guid fingerprint, ApiKind kind, Guid? parentFingerprint, string name)
    {
        if (_apiByFingerprint.ContainsKey(fingerprint))
            return;

        var parentApi = (IntermediaApi?)null;

        if (parentFingerprint is not null && !_apiByFingerprint.TryGetValue(parentFingerprint.Value, out parentApi))
        {
            _logger.WriteLine($"error: can't find parent API {fingerprint}");
            return;
        }

        var index = _apis.Count;
        var api = new IntermediaApi(index, fingerprint, kind, parentApi, name);
        _apis.Add(api);
        _apiByFingerprint.Add(api.Fingerprint, api);

        if (parentApi is null)
        {
            _rootApis.Add(api);
        }
        else
        {
            parentApi.Children ??= new();
            parentApi.Children.Add(api);
        }
    }

    private bool DefineAssembly(Guid fingerprint, string name, string version, string publicKeyToken)
    {
        if (_assemblyByFingerprint.ContainsKey(fingerprint))
            return false;

        var index = _assemblies.Count;
        var assembly = new IntermediaAssembly(index, fingerprint, name, version, publicKeyToken);
        _assemblies.Add(assembly);
        _assemblyByFingerprint.Add(assembly.Fingerprint, assembly);
        return true;
    }

    private void DefinePlatformSupport(Guid assemblyFingerprint, XElement element)
    {
        DefinePlatformSupport(assemblyFingerprint, element, isSupported: true);
        DefinePlatformSupport(assemblyFingerprint, element, isSupported: false);
    }

    private void DefinePlatformSupport(Guid assemblyFingerprint, XElement element, bool isSupported)
    {
        var elementName = isSupported ? "supportedPlatform" : "unsupportedPlatform";

        foreach (var supportElement in element.Elements(elementName))
        {
            var id = supportElement.Attribute("id")?.Value;
            var apiFingerprint = id is null ? (Guid?)null : Guid.Parse(id);
            var platformName = supportElement.Attribute("name")!.Value;

            DefinePlatformSupport(apiFingerprint, assemblyFingerprint, platformName, isSupported);
        }
    }

    private string DefinePlatform(string platformName)
    {
        if (_platformNames.TryGetValue(platformName, out var result))
            return result;

        _platformNames.Add(platformName);
        return platformName;
    }

    private void DefinePlatformSupport(Guid? apiFingerprint, Guid assemblyFingerprint, string platformName, bool isSupported)
    {
        platformName = DefinePlatform(platformName);

        if (!_assemblyByFingerprint.TryGetValue(assemblyFingerprint, out var assembly))
        {
            _logger.WriteLine($"error: can't find assembly {assemblyFingerprint}");
            return;
        }

        var platformSupport = new IntermediatePlatformSupport(platformName, isSupported);

        if (apiFingerprint is null)
        {
            assembly.PlatformSupport ??= new();
            assembly.PlatformSupport.Add(platformSupport);
        }
        else
        {
            if (!_apiByFingerprint.TryGetValue(apiFingerprint.Value, out var api))
            {
                _logger.WriteLine($"error: can't find API {apiFingerprint.Value}");
                return;
            }

            if (!assembly.Declarations.TryGetValue(api, out var declaration))
            {
                _logger.WriteLine($"error: can't find declaration for API {api.Name} ({api.Fingerprint}) in assembly {assembly.Name} ({assembly.Fingerprint})");
                return;
            }

            declaration.PlatformSupport ??= new();
            declaration.PlatformSupport.Add(platformSupport);
        }
    }

    private void DefineObsoletions(Guid assemblyFingerprint, XElement element)
    {
        foreach (var obsoleteElement in element.Elements("obsolete"))
        {
            var apiFingerprint = Guid.Parse(obsoleteElement.Attribute("id")!.Value);
            var isError = bool.Parse(obsoleteElement.Attribute("isError")?.Value ?? bool.FalseString);
            var message = obsoleteElement.Attribute("message")?.Value ?? string.Empty;
            var diagnosticId = obsoleteElement.Attribute("diagnosticId")?.Value ?? string.Empty;
            var urlFormat = obsoleteElement.Attribute("urlFormat")?.Value ?? string.Empty;
            DefineObsoletion(apiFingerprint, assemblyFingerprint, isError, message, diagnosticId, urlFormat);
        }
    }

    private void DefineObsoletion(Guid apiFingerprint, Guid assemblyFingerprint, bool isError, string message, string diagnosticId, string urlFormat)
    {
        if (!_apiByFingerprint.TryGetValue(apiFingerprint, out var api))
        {
            _logger.WriteLine($"error: can't find API {apiFingerprint}");
            return;
        }

        if (!_assemblyByFingerprint.TryGetValue(assemblyFingerprint, out var assembly))
        {
            _logger.WriteLine($"error: can't find assembly {assemblyFingerprint}");
            return;
        }

        if (!assembly.Declarations.TryGetValue(api, out var declaration))
        {
            _logger.WriteLine($"error: can't find declaration for API {api.Name} ({api.Fingerprint}) in assembly {assembly.Name} ({assembly.Fingerprint})");
            return;
        }

        declaration.Obsoletion = new IntermediateObsoletion(isError, message, diagnosticId, urlFormat);
    }

    private void DefinePreviewRequirements(Guid assemblyFingerprint, XElement element)
    {
        foreach (var previewRequirementElement in element.Elements("previewRequirement"))
        {
            var id = previewRequirementElement.Attribute("id")?.Value;
            var apiFingerprint = id is null ? (Guid?)null : Guid.Parse(id);
            var message = previewRequirementElement.Attribute("message")?.Value ?? string.Empty;
            var url = previewRequirementElement.Attribute("url")?.Value ?? string.Empty;

            DefinePreviewRequirements(apiFingerprint, assemblyFingerprint, message, url);
        }
    }

    private void DefinePreviewRequirements(Guid? apiFingerprint, Guid assemblyFingerprint, string message, string url)
    {
        if (!_assemblyByFingerprint.TryGetValue(assemblyFingerprint, out var assembly))
        {
            _logger.WriteLine($"error: can't find assembly {assemblyFingerprint}");
            return;
        }

        var previewRequirement = new IntermediatePreviewRequirement(message, url);

        if (apiFingerprint is null)
        {
            assembly.PreviewRequirement = previewRequirement;
        }
        else
        {
            if (!_apiByFingerprint.TryGetValue(apiFingerprint.Value, out var api))
            {
                _logger.WriteLine($"error: can't find API {apiFingerprint.Value}");
                return;
            }

            if (!assembly.Declarations.TryGetValue(api, out var declaration))
            {
                _logger.WriteLine($"error: can't find declaration for API {api.Name} ({api.Fingerprint}) in assembly {assembly.Name} ({assembly.Fingerprint})");
                return;
            }

            declaration.PreviewRequirement = previewRequirement;
        }
    }

    private void DefineExperimentals(Guid assemblyFingerprint, XElement element)
    {
        foreach (var experimentalElement in element.Elements("experimental"))
        {
            var id = experimentalElement.Attribute("id")?.Value;
            var apiFingerprint = id is null ? (Guid?)null : Guid.Parse(id);
            var diagnosticId = experimentalElement.Attribute("diagnosticId")?.Value ?? string.Empty;
            var urlFormat = experimentalElement.Attribute("urlFormat")?.Value ?? string.Empty;

            DefineExperimentals(apiFingerprint, assemblyFingerprint, diagnosticId, urlFormat);
        }
    }

    private void DefineExperimentals(Guid? apiFingerprint, Guid assemblyFingerprint, string diagnosticId, string urlFormat)
    {
        if (!_assemblyByFingerprint.TryGetValue(assemblyFingerprint, out var assembly))
        {
            _logger.WriteLine($"error: can't find assembly {assemblyFingerprint}");
            return;
        }

        var experimental = new IntermediateExperimental(diagnosticId, urlFormat);

        if (apiFingerprint is null)
        {
            assembly.Experimental = experimental;
        }
        else
        {
            if (!_apiByFingerprint.TryGetValue(apiFingerprint.Value, out var api))
            {
                _logger.WriteLine($"error: can't find API {apiFingerprint.Value}");
                return;
            }

            if (!assembly.Declarations.TryGetValue(api, out var declaration))
            {
                _logger.WriteLine($"error: can't find declaration for API {api.Name} ({api.Fingerprint}) in assembly {assembly.Name} ({assembly.Fingerprint})");
                return;
            }

            declaration.Experimental = experimental;
        }
    }

    private void DefineExtensions(IEnumerable<XElement> extensionElements)
    {
        foreach (var experimentalElement in extensionElements)
        {
            var guid = Guid.Parse(experimentalElement.Attribute("fingerprint")!.Value);
            var typeGuid = Guid.Parse(experimentalElement.Attribute("type")!.Value);
            var methodGuid = Guid.Parse(experimentalElement.Attribute("method")!.Value);
            DefineExtension(guid, typeGuid, methodGuid);
        }
    }

    private void DefineExtension(Guid extensionGuid, Guid typeGuid, Guid methodGuid)
    {
        if (extensionGuid == Guid.Empty || typeGuid == Guid.Empty || methodGuid == Guid.Empty)
            return;

        _extensions.TryAdd(extensionGuid, (typeGuid, methodGuid));
    }

    private void ResolveExtensions()
    {
        foreach (var (extensionMethodGuid, (typeFingerprint, methodFingerprint)) in _extensions)
        {
            if (!_apiByFingerprint.TryGetValue(typeFingerprint, out var type))
            {
                Console.WriteLine($"error: can't resolve extension type {typeFingerprint}");
                continue;
            }

            if (!_apiByFingerprint.TryGetValue(methodFingerprint, out var method))
            {
                Console.WriteLine($"error: can't resolve extension method {method}");
                continue;
            }

            var extension = new IntermediateExtension(extensionMethodGuid, method);
            type.Extensions ??= new();
            type.Extensions.Add(extension);
        }
    }

    private void DefineDeclaration(Guid assemblyFingerprint, Guid apiFingerprint, string syntax)
    {
        if (!_assemblyByFingerprint.TryGetValue(assemblyFingerprint, out var assembly))
        {
            _logger.WriteLine($"error: can't find assembly {assemblyFingerprint}");
            return;
        }

        if (!_apiByFingerprint.TryGetValue(apiFingerprint, out var api))
        {
            _logger.WriteLine($"error: can't find API {apiFingerprint}");
            return;
        }

        if (assembly.Declarations.TryGetValue(api, out var declaration))
        {
            _logger.WriteLine($"error: declaration for API {api.Name} ({api.Fingerprint}) in assembly {assembly.Name} ({assembly.Fingerprint}) already exists");
            return;
        }

        declaration = new IntermediateDeclaration(api, assembly, syntax);
        assembly.Declarations.Add(api, declaration);
    }

    private void DefineFramework(string frameworkName)
    {
        if (_frameworkByName.TryGetValue(frameworkName, out var framework))
            return;

        framework = new IntermediateFramework(frameworkName);
        _frameworkByName.Add(framework.Name, framework);
    }

    private void DefineFrameworkAssembly(string frameworkName, Guid assemblyFingerprint)
    {
        if (!_frameworkByName.TryGetValue(frameworkName, out var framework))
        {
            _logger.WriteLine($"error: can't find framework {frameworkName}");
            return;
        }

        if (!_assemblyByFingerprint.TryGetValue(assemblyFingerprint, out var assembly))
        {
            _logger.WriteLine($"error: can't find assembly {assemblyFingerprint}");
            return;
        }

        framework.Assemblies.Add(assembly);
        assembly.Frameworks.Add(framework);
    }

    private void DefinePackage(Guid fingerprint, string name, string version)
    {
        if (_packageByFingerprint.TryGetValue(fingerprint, out var package))
            return;

        package = new IntermediatePackage(fingerprint, name, version);
        _packageByFingerprint.Add(package.Fingerprint, package);
    }

    private void DefinePackageAssembly(Guid packageFingerprint, string frameworkName, Guid assemblyFingerprint)
    {
        if (!_packageByFingerprint.TryGetValue(packageFingerprint, out var package))
        {
            _logger.WriteLine($"error: can't find package {packageFingerprint}");
            return;
        }

        if (!_assemblyByFingerprint.TryGetValue(assemblyFingerprint, out var assembly))
        {
            _logger.WriteLine($"error: can't find assembly {assemblyFingerprint}");
            return;
        }

        DefineFramework(frameworkName);

        var framework = _frameworkByName[frameworkName];
        package.Assemblies.Add((framework, assembly));
        assembly.Packages.Add((package, framework));
    }

    private sealed class IntermediaApi(int index, Guid fingerprint, ApiKind kind, IntermediaApi? parent, string name)
    {
        public int Index { get; } = index;
        public Guid Fingerprint { get; } = fingerprint;
        public ApiKind Kind { get; } = kind;
        public IntermediaApi? Parent { get; } = parent;
        public string Name { get; } = name;
        public List<IntermediaApi>? Children { get; set; }
        public List<IntermediateExtension>? Extensions { get; set; }
    }

    private sealed class IntermediaAssembly(int index, Guid fingerprint, string name, string version, string publicKeyToken)
    {
        public int Index { get; } = index;
        public Guid Fingerprint { get; } = fingerprint;
        public string Name { get; } = name;
        public string Version { get; } = version;
        public string PublicKeyToken { get; } = publicKeyToken;

        public Dictionary<IntermediaApi, IntermediateDeclaration> Declarations { get; } = new();

        public List<IntermediateFramework> Frameworks { get; } = new();

        public List<(IntermediatePackage, IntermediateFramework)> Packages { get; } = new();

        public IntermediatePreviewRequirement? PreviewRequirement { get; set; }
        public IntermediateExperimental? Experimental { get; set; }
        public List<IntermediatePlatformSupport>? PlatformSupport { get; set; }

        public IEnumerable<IntermediaApi> RootApis => Declarations.Select(d => d.Key).Where(d => d.Parent is null);
    }

    private sealed class IntermediateDeclaration(IntermediaApi api, IntermediaAssembly assembly, string syntax)
    {
        public IntermediaApi Api { get; } = api;
        public IntermediaAssembly Assembly { get; } = assembly;
        public string Syntax { get; } = syntax;
        public IntermediateObsoletion? Obsoletion { get; set; }
        public IntermediatePreviewRequirement? PreviewRequirement { get; set; }
        public IntermediateExperimental? Experimental { get; set; }
        public List<IntermediatePlatformSupport>? PlatformSupport { get; set; }
    }

    private sealed class IntermediateObsoletion(bool isError, string message, string diagnosticId, string urlFormat)
    {
        public bool IsError { get; } = isError;
        public string Message { get; } = message;
        public string DiagnosticId { get; } = diagnosticId;
        public string UrlFormat { get; } = urlFormat;
    }

    private sealed class IntermediatePreviewRequirement(string message, string url)
    {
        public string Message { get; } = message;
        public string Url { get; } = url;
    }

    private sealed class IntermediateExperimental(string diagnosticId, string urlFormat)
    {
        public string DiagnosticId { get; } = diagnosticId;
        public string UrlFormat { get; } = urlFormat;
    }

    private sealed class IntermediatePlatformSupport(string platformName, bool isSupported)
    {
        public string PlatformName { get; } = platformName;
        public bool IsSupported { get; } = isSupported;
    }

    private sealed class IntermediateFramework(string name)
    {
        public string Name { get; } = name;

        public List<IntermediaAssembly> Assemblies { get; } = new();
    }

    private sealed class IntermediatePackage(Guid fingerprint, string name, string version)
    {
        public Guid Fingerprint { get; } = fingerprint;
        public string Name { get; } = name;
        public string Version { get; } = version;
        public List<(IntermediateFramework, IntermediaAssembly)> Assemblies { get; } = new();
    }

    private sealed class IntermediateExtension(Guid fingerprint, IntermediaApi method)
    {
        public Guid Fingerprint { get; } = fingerprint;
        public IntermediaApi Method { get; } = method;
    }

    private sealed class IntermediateUsageSource(string name, DateOnly date, FrozenDictionary<Guid, float> usages)
    {
        public string Name { get; } = name;
        public DateOnly Date { get; } = date;
        public FrozenDictionary<Guid, float> Usages { get; } = usages;
    }
}