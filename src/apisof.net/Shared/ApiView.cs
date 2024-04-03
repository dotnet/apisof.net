using NuGet.Frameworks;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Shared;

public sealed class ApiView
{
    private readonly ApiAvailabilityContext _context;

    public ApiView(ApiAvailabilityContext context, string framework)
    {
        ThrowIfNull(context);
        ThrowIfNullOrWhiteSpace(framework);
        
        _context = context;
        Framework = NuGetFramework.Parse(framework);
    }

    public NuGetFramework Framework { get; }
    
    public bool IsSupported(ApiModel api)
    {
        ThrowIfDefault(api);

        return _context.IsAvailable(api, Framework);
    }
}