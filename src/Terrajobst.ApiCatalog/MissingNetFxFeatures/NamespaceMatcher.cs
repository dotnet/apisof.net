namespace Terrajobst.ApiCatalog;

public sealed class NamespaceMatcher : ApiMatcher
{
    public NamespaceMatcher(string namespaceName)
    {
        ThrowIfNull(namespaceName);

        NamespaceName = namespaceName;

        if (NamespaceName.EndsWith(".*"))
        {
            NamespaceName = NamespaceName[..^2];
            IsWildcard = true;
        }
    }

    public string NamespaceName { get; }

    public bool IsWildcard { get; }

    public override bool IsMatch(string assemblyName,
                                 string namespaceName,
                                 string typeName,
                                 string memberName)
    {
        if (string.Equals(namespaceName, NamespaceName, StringComparison.Ordinal))
            return true;

        if (IsWildcard)
        {
            if (namespaceName is not null &&
                namespaceName.Length > NamespaceName.Length &&
                namespaceName.StartsWith(NamespaceName, StringComparison.OrdinalIgnoreCase) &&
                namespaceName[NamespaceName.Length] == '.')
            {
                return true;
            }
        }

        return false;
    }

    public override string ToString()
    {
        var wildcard = IsWildcard ? ".*" : string.Empty;
        return $"Namespace {NamespaceName}{wildcard}";
    }
}

