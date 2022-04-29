internal sealed class AssemblyResult
{
    public AssemblyResult(string assemblyName,
                          string? assemblyIssues,
                          IReadOnlyList<ApiResult> apis)
    {
        AssemblyName = assemblyName;
        AssemblyIssues = assemblyIssues;
        Apis = apis;
    }

    public string AssemblyName { get; }

    public string? AssemblyIssues { get; }

    public IReadOnlyList<ApiResult> Apis { get; }
}