using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Terrajobst.ApiCatalog.Generation.DesignNotes;

internal readonly struct ReviewedApiParser(List<ReviewedApi> receiver)
{
    private static readonly SymbolDisplayFormat TypeFormat = new
    (
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters
    );

    private static readonly SymbolDisplayFormat MemberFormat = new
    (
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
        delegateStyle: SymbolDisplayDelegateStyle.NameOnly,
        propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
        extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod
    );

    public void Parse(string source)
    {
        receiver.Clear();

        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(null, [tree]);

        WalkNamespace(compilation.Assembly.GlobalNamespace);
    }

    private void WalkNamespace(INamespaceSymbol symbol)
    {
        foreach (var type in symbol.GetTypeMembers())
            WalkType(type);

        foreach (var ns in symbol.GetNamespaceMembers())
            WalkNamespace(ns);
    }

    private void WalkType(INamedTypeSymbol symbol)
    {
        AddType(symbol);

        foreach (var member in symbol.GetMembers())
            WalkMember(member);
    }

    private void WalkMember(ISymbol symbol)
    {
        if (!symbol.CanBeReferencedByName)
            return;

        switch (symbol.Kind)
        {
            case SymbolKind.NamedType:
                AddType((INamedTypeSymbol)symbol);
                break;
            case SymbolKind.Field:
                AddNonTypeMember(ReviewedApiKind.Field, symbol);
                break;
            case SymbolKind.Method:
                AddNonTypeMember(ReviewedApiKind.Method, symbol);
                break;
            case SymbolKind.Property:
                AddNonTypeMember(ReviewedApiKind.Property, symbol);
                break;
            case SymbolKind.Event:
                AddNonTypeMember(ReviewedApiKind.Event, symbol);
                break;
            default:
                throw new UnreachableException();
        }
    }

    private void AddType(INamedTypeSymbol type)
    {
        var reference = new ReviewedApi(ReviewedApiKind.Type,
            GetNameForNamespace(type.ContainingNamespace),
            type.ToDisplayString(TypeFormat),
            null
        );

        receiver.Add(reference);
    }

    private void AddNonTypeMember(ReviewedApiKind memberKind, ISymbol member)
    {
        var reference = new ReviewedApi(memberKind,
            GetNameForNamespace(member.ContainingNamespace),
            member.ContainingType.ToDisplayString(TypeFormat),
            member.ToDisplayString(MemberFormat)
        );

        receiver.Add(reference);
    }

    private static string? GetNameForNamespace(INamespaceSymbol? symbol)
    {
        if (symbol is null || symbol.IsGlobalNamespace)
            return null;

        return symbol.ToDisplayString();
    }
}