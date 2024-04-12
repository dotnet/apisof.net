using System.Collections.Frozen;

namespace Terrajobst.ApiCatalog.Generation.DesignNotes;

internal sealed class ApiResolver
{
    private readonly FrozenDictionary<string, ApiModel> _namespaceByName;
    private readonly FrozenDictionary<string, ApiModel> _types;

    public ApiResolver(ApiCatalogModel catalog)
    {
        ThrowIfNull(catalog);

        _namespaceByName = catalog.RootApis.ToFrozenDictionary(a => a.Name);

        var types = new Dictionary<string, ApiModel>();

        foreach (var api in catalog.AllApis)
        {
            if (!api.Kind.IsType())
                continue;

            var ns = api.GetNamespaceName();
            if (!ns.StartsWith("System") && !ns.StartsWith("Microsoft"))
                continue;

            types.TryAdd(api.GetTypeName(), api);
        }

        _types = types.ToFrozenDictionary();
    }

    public ApiModel? Resolve(ReviewedApi api)
    {
        ApiModel? current = null;

        if (api.NamespaceName is not null)
        {
            current = ResolveNamespace(api.NamespaceName);
            if (current is null)
                return null;
        }

        if (api.TypeName is not null)
        {
            current = ResolveType(current, api.TypeName);
            if (current is null)
                return null;
        }

        if (api.MemberName is not null)
        {
            current = ResolveMember(current, api.MemberName);
            if (current is null)
                return null;
        }

        return current;
    }

    private ApiModel? ResolveNamespace(string namespaceName)
    {
        return _namespaceByName.TryGetValue(namespaceName, out var api) ? api : null;
    }

    private ApiModel? ResolveType(ApiModel? current, string typeName)
    {
        if (current is null)
            return _types.TryGetValue(typeName, out var result) ? result : null;

        return current.Value
            .Children
            .Where(a => a.Kind.IsType() &&
                        string.Equals(a.GetTypeName(), typeName, StringComparison.OrdinalIgnoreCase))
            .Cast<ApiModel?>()
            .FirstOrDefault();
    }

    private ApiModel? ResolveMember(ApiModel? current, string memberName)
    {
        if (current is null)
            return null;

        return current.Value
            .Children
            .Where(a => !a.Kind.IsType() && string.Equals(a.GetMemberName(), memberName, StringComparison.OrdinalIgnoreCase))
            .Cast<ApiModel?>()
            .FirstOrDefault();
    }
}