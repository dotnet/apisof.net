namespace Terrajobst.ApiCatalog.Generation.DesignNotes;

public readonly struct ReviewedApi
{
    public ReviewedApi(ReviewedApiKind kind, string? namespaceName, string? typeName, string? memberName)
    {
        Kind = kind;
        NamespaceName = namespaceName;
        TypeName = typeName;
        MemberName = memberName;
    }

    public ReviewedApiKind Kind { get; }

    public string? NamespaceName { get; }

    public string? TypeName { get; }

    public string? MemberName { get; }

    public override string ToString()
    {
        return $"{Kind}, N={NamespaceName}, T={TypeName}, M={MemberName}";
    }
}