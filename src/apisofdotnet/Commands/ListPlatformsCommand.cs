using Terrajobst.ApiCatalog;

internal sealed class ListPlatformsCommand : Command
{
    private readonly CatalogService _catalogService;

    public ListPlatformsCommand(CatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public override string Name => "list-platforms";

    public override string Description => "Lists known platforms";

    public override async Task ExecuteAsync()
    {
        var zero = new Version(0, 0, 0, 0);

        var catalog = await _catalogService.LoadCatalogAsync();
        var platforms = catalog.Platforms.Select(p => PlatformAnnotationContext.ParsePlatform(p.Name))
                                         .GroupBy(p => p.Name, p => p.Version)
                                         .Select(g => (Name: g.Key, Min: g.Where(v => v != zero).DefaultIfEmpty(zero).Min(), Max: g.Max()))
                                         .OrderBy(n => n.Name);

        Console.WriteLine("Available platforms:");

        foreach (var platform in platforms)
        {
            if (platform.Min == zero && platform.Max == zero)
                Console.WriteLine($"  {platform.Name}");
            else
                Console.WriteLine($"  {platform.Name,-25}{Prettify(platform.Min)}-{Prettify(platform.Max)}");
        }

        string Prettify(Version? version)
        {
            if (version is null)
                return string.Empty;

            if (version.Revision == 0)
            {
                if (version.Build == 0)
                    return $"{version.Major}.{version.Minor}";

                return $"{version.Major}.{version.Minor}.{version.Build}";
            }

            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
    }
}