using System.Diagnostics;
using System.Security.Cryptography;

using Microsoft.CodeAnalysis;

namespace Terrajobst.ApiCatalog;

public sealed class AssemblyEntry
{
    private AssemblyEntry(AssemblyIdentity identity,
                          PlatformSupportEntry? platformSupportEntry,
                          PreviewRequirementEntry? previewRequirementEntry,
                          ExperimentalEntry? experimentalEntry,
                          List<ApiEntry> apis,
                          List<ExtensionEntry> extensions)
    {
        Fingerprint = ComputeFingerprint(identity, apis);
        Identity = identity;
        PlatformSupportEntry = platformSupportEntry;
        PreviewRequirementEntry = previewRequirementEntry;
        ExperimentalEntry = experimentalEntry;
        Apis = apis;
        Extensions = extensions;
    }

    public Guid Fingerprint { get; }
    public AssemblyIdentity Identity { get; }
    public PlatformSupportEntry? PlatformSupportEntry { get; }
    public PreviewRequirementEntry? PreviewRequirementEntry { get; }
    public ExperimentalEntry? ExperimentalEntry { get; }
    public List<ApiEntry> Apis { get; }
    public List<ExtensionEntry> Extensions { get; }

    public static AssemblyEntry Create(IAssemblySymbol assembly)
    {
        var identity = assembly.Identity;
        var platformSupportEntry = PlatformSupportEntry.Create(assembly.Modules.First()) ?? PlatformSupportEntry.Create(assembly);
        var previewRequirementEntry = PreviewRequirementEntry.Create(assembly.Modules.First()) ?? PreviewRequirementEntry.Create(assembly);
        var experimentalEntry = ExperimentalEntry.Create(assembly.Modules.First()) ?? ExperimentalEntry.Create(assembly);
        var apis = GetApis(assembly);
        var extensions = GetExtensions(assembly);
        return new AssemblyEntry(identity, platformSupportEntry, previewRequirementEntry, experimentalEntry, apis, extensions);
    }

    private static List<ApiEntry> GetApis(IAssemblySymbol symbol)
    {
        var result = new List<ApiEntry>();
        var types = symbol.GetAllTypes()
            .Where(t => t.IsIncludedInCatalog())
            .GroupBy(t => t.ContainingNamespace, SymbolEqualityComparer.Default);

        foreach (var namespaceGroup in types)
        {
            var entry = ApiEntry.Create(namespaceGroup.Key!);
            result.Add(entry);

            foreach (var type in namespaceGroup)
                AddType(entry, type);
        }

        return result;
    }

    private static List<ExtensionEntry> GetExtensions(IAssemblySymbol symbol)
    {
        var result = new List<ExtensionEntry>();
        var methods = symbol.GetAllTypes()
                            .Where(t => t.IsStatic && t.IsIncludedInCatalog())
                            .SelectMany(t => t.GetMembers())
                            .OfType<IMethodSymbol>()
                            .Where(m => m.IsExtensionMethod && m.IsIncludedInCatalog());

        foreach (var extensionMethod in methods)
        {
            var extendedType = extensionMethod.Parameters[0].Type;
            var extendedTypeGuid = extendedType.GetCatalogGuid();
            var extensionMethodGuid = extensionMethod.GetCatalogGuid();

            if (extendedTypeGuid == Guid.Empty || extensionMethodGuid == Guid.Empty)
                continue;

            var fingerprint = ComputeFingerprint(extendedTypeGuid, extensionMethodGuid);

            var entry = new ExtensionEntry(fingerprint, extendedTypeGuid, extensionMethodGuid);
            result.Add(entry);
        }

        return result;
    }

    private static void AddType(ApiEntry parent, ITypeSymbol symbol)
    {
        if (!symbol.IsIncludedInCatalog())
            return;

        var apiEntry = ApiEntry.Create(symbol, parent);
        parent.Children.Add(apiEntry);

        foreach (var member in symbol.GetMembers())
            AddMember(apiEntry, member);
    }

    private static void AddMember(ApiEntry parent, ISymbol symbol)
    {
        if (symbol is INamedTypeSymbol type)
        {
            AddType(parent, type);
            return;
        }

        if (!symbol.IsIncludedInCatalog())
            return;

        // We don't want to include accessors at the type level; we'll add them as children
        // under properties and events.
        if (symbol.IsAccessor())
            return;

        var entry = ApiEntry.Create(symbol, parent);
        parent.Children.Add(entry);

        switch (symbol)
        {
            case IPropertySymbol property:
                AddOptionalAccessor(property.GetMethod, entry);
                AddOptionalAccessor(property.SetMethod, entry);
                break;
            case IEventSymbol @event:
                AddOptionalAccessor(@event.AddMethod, entry);
                AddOptionalAccessor(@event.RemoveMethod, entry);
                AddOptionalAccessor(@event.RaiseMethod, entry);
                break;
        }
    }

    private static void AddOptionalAccessor(IMethodSymbol? accessor, ApiEntry parent)
    {
        if (accessor is null)
            return;

        var entry = ApiEntry.Create(accessor, parent);
        parent.Children.Add(entry);
    }

    private static Guid ComputeFingerprint(AssemblyIdentity identity, List<ApiEntry> roots)
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);

        writer.WriteLine(identity.ToString());
        WriteDeclarations(writer, roots);

        writer.Flush();

        stream.Position = 0;

        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(stream);
        return new Guid(hashBytes);

        static void WriteDeclarations(TextWriter writer, List<ApiEntry> roots)
        {
            foreach (var a in roots)
            {
                writer.WriteLine(a.Syntax);
                WriteDeclarations(writer, a.Children);
            }
        }
    }

    private static Guid ComputeFingerprint(Guid guid1, Guid guid2)
    {
        var buffer = (Span<byte>)stackalloc byte[32];
        var success1 = guid1.TryWriteBytes(buffer);
        Debug.Assert(success1);

        var success2 = guid2.TryWriteBytes(buffer[16..]);
        Debug.Assert(success2);

        var hashBytes = (Span<byte>)stackalloc byte[16];
        var written = MD5.HashData(buffer, hashBytes);
        Debug.Assert(written == hashBytes.Length);

        return new Guid(hashBytes);
    }

    public IEnumerable<ApiEntry> AllApis()
    {
        var stack = new Stack<ApiEntry>();
        foreach (var api in Apis.AsEnumerable().Reverse())
            stack.Push(api);

        while (stack.Count > 0)
        {
            var api = stack.Pop();
            yield return api;

            foreach (var child in api.Children.AsEnumerable().Reverse())
                stack.Push(child);
        }
    }
}