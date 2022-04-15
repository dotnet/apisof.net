internal sealed class AssemblyAvailabilityResult
{
    public AssemblyAvailabilityResult(string assemblyName,
                                      string? assemblyIssues,
                                      IReadOnlyList<ApiAvailabilityResult> apiResults)
    {
        AssemblyName = assemblyName;
        AssemblyIssues = assemblyIssues;
        ApiResults = apiResults;
    }

    public string AssemblyName { get; }

    public string? AssemblyIssues { get; }

    public IReadOnlyList<ApiAvailabilityResult> ApiResults { get; }
}