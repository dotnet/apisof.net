namespace Terrajobst.ApiCatalog;

public sealed class TypeMatcher : ApiMatcher
{
    public TypeMatcher(string namespaceName, string typeName)
    {
        ThrowIfNull(namespaceName);
        ThrowIfNull(typeName);

        NamespaceName = namespaceName;
        TypeName = typeName;
    }

    public string NamespaceName { get; }
    public string TypeName { get; }

    public override bool IsMatch(string assemblyName,
                                 string namespaceName,
                                 string typeName,
                                 string memberName)
    {
        return string.Equals(typeName, TypeName, StringComparison.Ordinal) &&
               string.Equals(namespaceName, NamespaceName, StringComparison.Ordinal);
    }

    public override string ToString()
    {
        return $"Type {NamespaceName}.{TypeName}";
    }
}

