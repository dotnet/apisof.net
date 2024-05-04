using System.Xml.Linq;

namespace Terrajobst.ApiCatalog;

public sealed class InMemoryStore : IndexStore
{
    private Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);

    protected override void Delete(string path)
    {
        ThrowIfNullOrEmpty(path);

        _files.Remove(path);
    }

    protected override bool Exists(string path)
    {
        ThrowIfNullOrEmpty(path);

        return _files.ContainsKey(path);
    }

    protected override IEnumerable<string> GetFiles(string path)
    {
        ThrowIfNullOrEmpty(path);

        if (!Path.EndsInDirectorySeparator(path))
            path += Path.DirectorySeparatorChar;

        foreach (var (key, value) in _files)
        {
            if (key.StartsWith(path, StringComparison.OrdinalIgnoreCase))
                yield return key;
        }
    }

    protected override string ReadAllText(string path)
    {
        ThrowIfNullOrEmpty(path);

        return _files[path];
    }

    protected override void WriteAllText(string path, string contents)
    {
        ThrowIfNullOrEmpty(path);
        ThrowIfNull(contents);

        _files[path] = contents;
    }
}

public sealed class FileSystemIndexStore : IndexStore
{
    private readonly string _rootPath;

    public FileSystemIndexStore(string path)
    {
        ThrowIfNull(path);

        _rootPath = Path.GetFullPath(path);
    }

    protected override string ReadAllText(string path)
    {
        ThrowIfNullOrEmpty(path);

        var fullPath = Path.Join(_rootPath, path);
        return File.ReadAllText(fullPath);
    }

    protected override void WriteAllText(string path, string contents)
    {
        ThrowIfNullOrEmpty(path);
        ThrowIfNull(contents);

        var fullPath = Path.Join(_rootPath, path);
        var directoryPath = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directoryPath);
        File.WriteAllText(fullPath, contents);
    }

    protected override void Delete(string path)
    {
        ThrowIfNullOrEmpty(path);

        var fullPath = Path.Join(_rootPath, path);
        if (File.Exists(path))
            File.Delete(fullPath);
    }

    protected override bool Exists(string path)
    {
        ThrowIfNullOrEmpty(path);

        var fullPath = Path.Join(_rootPath, path);
        return File.Exists(fullPath);
    }

    protected override IEnumerable<string> GetFiles(string path)
    {
        ThrowIfNullOrEmpty(path);

        var fullPath = Path.Join(_rootPath, path);
        if (!Directory.Exists(fullPath))
            return [];

        return Directory
            .EnumerateFiles(fullPath)
            .Select(p => Path.GetRelativePath(_rootPath, p));
    }
}

public abstract class IndexStore
{
    private const string AssembliesDirectory = "assemblies";
    private const string FrameworkDirectory = "frameworks";
    private const string PackageDirectory = "packages";

    protected abstract string ReadAllText(string path);

    protected abstract void WriteAllText(string path, string contents);

    protected abstract void Delete(string path);

    protected abstract bool Exists(string path);

    protected abstract IEnumerable<string> GetFiles(string path);

    public bool HasFramework(string frameworkName)
    {
        ThrowIfNullOrEmpty(frameworkName);

        var frameworkPath = GetFrameworkPath(frameworkName);
        return Exists(frameworkPath);
    }

    private string GetFrameworkPath(string frameworkName)
    {
        return Path.Join(FrameworkDirectory, $"{frameworkName}.xml");
    }

    public void Store(FrameworkEntry entry)
    {
        ThrowIfNull(entry);

        var frameworkPath = GetFrameworkPath(entry.FrameworkName);
        if (Exists(frameworkPath))
            return;

        foreach (var assembly in entry.Assemblies)
            Store(assembly);

        using var writer = new StringWriter();
        WriteFrameworkEntry(writer, entry);
        WriteAllText(frameworkPath, writer.ToString());
    }

    public void Store(PackageEntry entry)
    {
        ThrowIfNull(entry);

        if (HasPackage(entry.Id, entry.Version))
            return;

        var packagePath = GetPackagePath(entry.Id, entry.Version);
        var packageDisabledPath = GetPackageDisabledPath(entry.Id);
        var packageFailedPath = GetPackageFailedPath(entry.Id, entry.Version);

        Delete(packageDisabledPath);
        Delete(packageFailedPath);

        foreach (var frameworkEntry in entry.Entries)
        foreach (var assembly in frameworkEntry.Assemblies)
            Store(assembly);

        using var writer = new StringWriter();
        WritePackageEntry(writer, entry);
        WriteAllText(packagePath, writer.ToString());
    }

    private void Store(AssemblyEntry entry)
    {
        var assemblyPath = Path.Join(AssembliesDirectory, $"{entry.Fingerprint:N}.xml");
        if (Exists(assemblyPath))
            return;

        using var writer = new StringWriter();
        WriteAssemblyEntry(writer, entry);
        WriteAllText(assemblyPath, writer.ToString());
    }

    public IEnumerable<XDocument> GetAssemblies()
    {
        return GetDocuments(AssembliesDirectory);
    }

    public IEnumerable<XDocument> GetFrameworks()
    {
        return GetDocuments(FrameworkDirectory);
    }

    public IEnumerable<XDocument> GetPackages()
    {
        return GetDocuments(PackageDirectory);
    }

    private IEnumerable<XDocument> GetDocuments(string directory)
    {
        return GetFiles(directory)
            .Where(d => d.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .Select(ReadAllText)
            .Select(XDocument.Parse);
    }

    public void MarkPackageAsNotIndexed(string id, string version)
    {
        ResetPackage(id, version);
    }

    public void MarkPackageAsFailed(string id, string version, Exception exception)
    {
        var packagePath = GetPackagePath(id, version);
        var packageDisabledPath = GetPackageDisabledPath(id);
        var packageFailedPath = GetPackageFailedPath(id, version);

        ResetPackage(id, version);
        WriteAllText(packageFailedPath, exception.ToString());
    }

    public void MarkPackageAsDisabled(string id, string version)
    {
        var packageDisabledPath = GetPackageDisabledPath(id);
        ResetPackage(id, version);
        WriteAllText(packageDisabledPath, string.Empty);
    }

    private void ResetPackage(string id, string version)
    {
        var packagePath = GetPackagePath(id, version);
        var packageDisabledPath = GetPackageDisabledPath(id);
        var packageFailedPath = GetPackageFailedPath(id, version);

        Delete(packagePath);
        Delete(packageDisabledPath);
        Delete(packageFailedPath);
    }

    public bool IsMarkedAsIndexed(string id, string version)
    {
        var packagePath = GetPackagePath(id, version);
        return Exists(packagePath);
    }

    public bool IsMarkedAsFailed(string id, string version)
    {
        var packageFailedPath = GetPackageFailedPath(id, version);
        return Exists(packageFailedPath);
    }

    public bool IsMarkedAsDisabled(string id, string version)
    {
        var packageDisabledPath = GetPackageDisabledPath(id);
        return Exists(packageDisabledPath);
    }

    private bool HasPackage(string id, string version)
    {
        var packagePath = GetPackagePath(id, version);
        return Exists(packagePath) ||
               IsMarkedAsDisabled(id, version) ||
               IsMarkedAsFailed(id, version);
    }

    private static string GetPackagePath(string id, string version)
    {
        return Path.Join(PackageDirectory, $"{id}-{version}.xml");
    }

    private static string GetPackageDisabledPath(string id)
    {
        return Path.Join(PackageDirectory, $"{id}-all.disabled");
    }

    private static string GetPackageFailedPath(string id, string version)
    {
        return Path.Join(PackageDirectory, $"{id}-{version}.failed");
    }

    private static void WriteFrameworkEntry(TextWriter writer, FrameworkEntry frameworkEntry)
    {
        var document = new XDocument();
        var root = new XElement("framework", new XAttribute("name", frameworkEntry.FrameworkName));
        document.Add(root);

        foreach (var assembly in frameworkEntry.Assemblies)
            AddAssembly(root, assembly);

        document.Save(writer);
    }

    private static void WritePackageEntry(TextWriter writer, PackageEntry packageEntry)
    {
        var document = new XDocument();
        var root = new XElement("package",
            new XAttribute("fingerprint", packageEntry.Fingerprint),
            new XAttribute("id", packageEntry.Id),
            new XAttribute("version", packageEntry.Version)
        );
        document.Add(root);

        foreach (var fx in packageEntry.Entries)
        {
            foreach (var assembly in fx.Assemblies)
                AddAssembly(root, assembly, fx.FrameworkName);
        }

        document.Save(writer);
    }

    private static void WriteAssemblyEntry(TextWriter writer, AssemblyEntry entry)
    {
        var document = new XDocument();
        var root = new XElement("assembly",
            new XAttribute("fingerprint", entry.Fingerprint.ToString("N")),
            new XAttribute("name", entry.Identity.Name),
            new XAttribute("publicKeyToken", entry.Identity.GetPublicKeyTokenString()),
            new XAttribute("version", entry.Identity.Version.ToString())
        );
        document.Add(root);

        foreach (var api in entry.AllApis())
            AddApi(root, api);

        foreach (var extension in entry.Extensions)
            AddExtension(root, extension);


        if (entry.PlatformSupportEntry is not null)
            AddPlatformSupport(root, entry.PlatformSupportEntry);

        if (entry.PreviewRequirementEntry is not null)
            AddPreviewRequirement(root, entry.PreviewRequirementEntry);

        if (entry.ExperimentalEntry is not null)
            AddExperimental(root, entry.ExperimentalEntry);

        foreach (var api in entry.AllApis())
        {
            var fingerprint = api.Fingerprint.ToString("N");

            var syntaxElement = new XElement("syntax", new XAttribute("id", fingerprint));
            root.Add(syntaxElement);
            syntaxElement.Add(api.Syntax);

            if (api.ObsoletionEntry is not null)
                AddObsoletion(root, api.ObsoletionEntry, fingerprint);

            if (api.PlatformSupportEntry is not null)
                AddPlatformSupport(root, api.PlatformSupportEntry, fingerprint);

            if (api.PreviewRequirementEntry is not null)
                AddPreviewRequirement(root, api.PreviewRequirementEntry, fingerprint);

            if (api.ExperimentalEntry is not null)
                AddExperimental(root, api.ExperimentalEntry, fingerprint);
        }

        document.Save(writer);
    }

    private static void AddExtension(XElement root, ExtensionEntry extension)
    {
        var extensionElement = new XElement("extension",
            new XAttribute("fingerprint", extension.Fingerprint.ToString("N")),
            new XAttribute("type", extension.ExtendedTypeGuid.ToString("N")),
            new XAttribute("method", extension.ExtensionMethodGuid.ToString("N"))
        );

        root.Add(extensionElement);
    }

    private static void AddAssembly(XContainer parent, AssemblyEntry assembly, string? frameworkName = null)
    {
        var assemblyElement = new XElement("assembly",
            frameworkName is null ? null : new XAttribute("fx", frameworkName),
            new XAttribute("fingerprint", assembly.Fingerprint.ToString("N"))
        );
        parent.Add(assemblyElement);
    }

    private static void AddApi(XContainer parent, ApiEntry api)
    {
        var apiElement = new XElement("api",
            new XAttribute("fingerprint", api.Fingerprint.ToString("N")),
            new XAttribute("kind", (int)api.Kind),
            new XAttribute("name", api.Name)
        );
        parent.Add(apiElement);

        if (api.Parent is not null)
            apiElement.Add(new XAttribute("parent", api.Parent.Fingerprint.ToString("N")));
    }

    private static void AddObsoletion(XContainer parent, ObsoletionEntry obsoletion, string apiFingerprint)
    {
        var obsoletionElement = new XElement("obsolete",
            new XAttribute("id", apiFingerprint),
            new XAttribute("isError", obsoletion.IsError)
        );

        if (obsoletion.Message is not null)
            obsoletionElement.Add(new XAttribute("message", obsoletion.Message));

        if (obsoletion.DiagnosticId is not null)
            obsoletionElement.Add(new XAttribute("diagnosticId", obsoletion.DiagnosticId));

        if (obsoletion.UrlFormat is not null)
            obsoletionElement.Add(new XAttribute("urlFormat", obsoletion.UrlFormat));

        parent.Add(obsoletionElement);
    }

    private static void AddPlatformSupport(XContainer parent, PlatformSupportEntry platformSupport, string? apiFingerprint = null)
    {
        foreach (var supported in platformSupport.SupportedPlatforms)
        {
            var supportedElement = new XElement("supportedPlatform",
                apiFingerprint is null ? null : new XAttribute("id", apiFingerprint),
                new XAttribute("name", supported)
            );
            parent.Add(supportedElement);
        }

        foreach (var unsupported in platformSupport.UnsupportedPlatforms)
        {
            var unsupportedElement = new XElement("unsupportedPlatform",
                apiFingerprint is null ? null : new XAttribute("id", apiFingerprint),
                new XAttribute("name", unsupported)
            );
            parent.Add(unsupportedElement);
        }
    }

    private static void AddPreviewRequirement(XContainer parent, PreviewRequirementEntry previewRequirement, string? apiFingerprint = null)
    {
        var previewRequirementElement = new XElement("previewRequirement",
            apiFingerprint is null ? null : new XAttribute("id", apiFingerprint)
        );

        if (previewRequirement.Message is not null)
            previewRequirementElement.Add(new XAttribute("message", previewRequirement.Message));

        if (previewRequirement.Url is not null)
            previewRequirementElement.Add(new XAttribute("url", previewRequirement.Url));

        parent.Add(previewRequirementElement);
    }

    private static void AddExperimental(XContainer parent, ExperimentalEntry experimentalEntry, string? apiFingerprint = null)
    {
        var experimentalElement = new XElement("experimental",
            apiFingerprint is null ? null : new XAttribute("id", apiFingerprint)
        );

        experimentalElement.Add(new XAttribute("diagnosticId", experimentalEntry.DiagnosticId));

        if (experimentalEntry.UrlFormat is not null)
            experimentalElement.Add(new XAttribute("urlFormat", experimentalEntry.UrlFormat));

        parent.Add(experimentalElement);
    }
}
