namespace Terrajobst.ApiCatalog;

public sealed class MemberMatcher : ApiMatcher
{
    public MemberMatcher(string namespaceName, string typeName, string memberName)
    {
        ArgumentNullException.ThrowIfNull(namespaceName);
        ArgumentNullException.ThrowIfNull(typeName);
        ArgumentNullException.ThrowIfNull(memberName);

        NamespaceName = namespaceName;
        TypeName = typeName;
        MemberName = memberName;
    }

    public string NamespaceName { get; }
    public string TypeName { get; }
    public string MemberName { get; }

    public override bool IsMatch(string assemblyName,
                                 string namespaceName,
                                 string typeName,
                                 string memberName)
    {
        return string.Equals(memberName, MemberName, StringComparison.Ordinal) &&
               string.Equals(typeName, TypeName, StringComparison.Ordinal) &&
               string.Equals(namespaceName, NamespaceName, StringComparison.Ordinal);
    }

    public override string ToString()
    {
        return $"Member {NamespaceName}.{TypeName}.{MemberName}";
    }
}

