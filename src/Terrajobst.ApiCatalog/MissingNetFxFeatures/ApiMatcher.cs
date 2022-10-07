namespace Terrajobst.ApiCatalog;

public abstract class ApiMatcher
{
    public abstract bool IsMatch(string assemblyName,
                                 string namespaceName,
                                 string typeName,
                                 string memberName);
}

