namespace Terrajobst.ApiCatalog;

public sealed class AssemblyMatcher : ApiMatcher
{
    public AssemblyMatcher(string assemblyName)
    {
        ThrowIfNull(assemblyName);

        AssemblyName = assemblyName;

        if (AssemblyName.EndsWith(".*"))
        {
            AssemblyName = AssemblyName[..^2];
            IsWildcard = true;
        }
    }

    public string AssemblyName { get; }

    public bool IsWildcard { get; }

    public override bool IsMatch(string assemblyName,
                                 string namespaceName,
                                 string typeName,
                                 string memberName)
    {
        if (string.Equals(assemblyName, AssemblyName, StringComparison.OrdinalIgnoreCase))
            return true;

        if (IsWildcard)
        {
            if (assemblyName.Length > AssemblyName.Length &&
                assemblyName.StartsWith(AssemblyName, StringComparison.OrdinalIgnoreCase) &&
                assemblyName[AssemblyName.Length] == '.')
            {
                return true;
            }
        }

        return false;
    }

    public override string ToString()
    {
        var wildcard = IsWildcard ? ".*" : string.Empty;
        return $"Assembly {AssemblyName}{wildcard}";
    }
}

