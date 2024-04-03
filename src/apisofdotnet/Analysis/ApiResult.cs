using Terrajobst.ApiCatalog;

internal readonly struct ApiResult
{
    public ApiResult(ApiModel api,
                     IReadOnlyList<FrameworkResult> frameworkResults)
    {
        ThrowIfNull(frameworkResults);

        Api = api;
        FrameworkResults = frameworkResults;
    }

    public ApiModel Api { get; }

    public IReadOnlyList<FrameworkResult> FrameworkResults { get; }

    public bool IsRelevant() => FrameworkResults.Any(fx => fx.IsRelevant());
}