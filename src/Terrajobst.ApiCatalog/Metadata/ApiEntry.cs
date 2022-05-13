using Microsoft.CodeAnalysis;

namespace Terrajobst.ApiCatalog;

public sealed class ApiEntry
{
    private ApiEntry(Guid guid,
                     ApiKind kind,
                     ApiEntry parent,
                     string name,
                     string syntax,
                     ObsoletionEntry obsoletionEntry,
                     PlatformSupportEntry platformSupportEntry,
                     PreviewRequirementEntry previewRequirementEntry)
    {
        Fingerprint = guid;
        Kind = kind;
        Parent = parent;
        Name = name;
        Syntax = syntax;
        ObsoletionEntry = obsoletionEntry;
        PlatformSupportEntry = platformSupportEntry;
        PreviewRequirementEntry = previewRequirementEntry;
    }

    public static ApiEntry Create(ISymbol symbol, ApiEntry parent = null)
    {
        var guid = symbol.GetCatalogGuid();
        var kind = symbol.GetApiKind();
        var name = symbol.GetCatalogName();
        var syntax = symbol.GetCatalogSyntaxMarkup();
        var obsoletionEntry = ObsoletionEntry.Create(symbol);
        var platformSupportEntry = PlatformSupportEntry.Create(symbol);
        var previewRequirementEntry = PreviewRequirementEntry.Create(symbol);
        return new ApiEntry(guid, kind, parent, name, syntax, obsoletionEntry, platformSupportEntry, previewRequirementEntry);
    }

    public Guid Fingerprint { get; }
    public ApiKind Kind { get; }
    public ApiEntry Parent { get; }
    public string Name { get; }
    public string Syntax { get; }
    public ObsoletionEntry ObsoletionEntry { get; }
    public PlatformSupportEntry PlatformSupportEntry { get; }
    public PreviewRequirementEntry PreviewRequirementEntry { get; }
    public List<ApiEntry> Children { get; } = new();
}