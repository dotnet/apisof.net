using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ApiCatalog.Metadata;

public sealed class ApiEntry
{
    private ApiEntry(Guid guid, ApiKind kind, ApiEntry parent, string name, string syntax)
    {
        Fingerprint = guid;
        Kind = kind;
        Parent = parent;
        Name = name;
        Syntax = syntax;
    }

    public static ApiEntry Create(ISymbol symbol, ApiEntry parent = null)
    {
        var guid = symbol.GetCatalogGuid();
        var kind = symbol.GetApiKind();
        var name = symbol.GetCatalogName();
        var syntax = symbol.GetCatalogSyntaxMarkup();
        return new ApiEntry(guid, kind, parent, name, syntax);
    }

    public Guid Fingerprint { get; }
    public ApiKind Kind { get; }
    public ApiEntry Parent { get; }
    public string Name { get; }
    public string Syntax { get; }
    public List<ApiEntry> Children { get; } = new();
}