using Terrajobst.ApiCatalog;

namespace NetUpgradePlanner.Analysis;

internal sealed class Problem
{
    public Problem(ProblemId problemId,
                   ApiModel api,
                   string details)
        : this(problemId, api, null, null, details)
    {
    }

    public Problem(ProblemId problemId,
                   string unresolvedReference,
                   string details)
        : this(problemId, null, unresolvedReference, null, details)
    {
    }

    public Problem(ProblemId problemId,
                   AssemblySetEntry resolvedReference,
                   string details)
        : this(problemId, null, resolvedReference.Name, resolvedReference, details)
    {
    }

    private Problem(ProblemId problemId,
                    ApiModel? api,
                    string? unresolvedReference,
                    AssemblySetEntry? resolvedReference,
                    string details)
    {
        ProblemId = problemId;
        Api = api;
        UnresolvedReference = unresolvedReference;
        ResolvedReference = resolvedReference;
        Details = details;
    }

    public ProblemId ProblemId { get; }

    public ApiModel? Api { get; }

    public string? UnresolvedReference { get; }

    public AssemblySetEntry? ResolvedReference { get; }

    public string Details { get; }
}
