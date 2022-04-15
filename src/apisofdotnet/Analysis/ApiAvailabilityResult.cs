using Terrajobst.ApiCatalog;

internal struct ApiAvailabilityResult
{
    public ApiAvailabilityResult(ApiModel api,
                                 IReadOnlyList<AvailabilityResult> frameworkResults)
    {
        Api = api;
        FrameworkResults = frameworkResults;
    }

    public ApiModel Api { get; }

    public IReadOnlyList<AvailabilityResult> FrameworkResults { get; }
}