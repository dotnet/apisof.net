using Mono.Options;

internal sealed class UpdateCatalogCommand : Command
{
    private readonly CatalogService _catalogService;
    private bool _force;

    public UpdateCatalogCommand(CatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public override string Name => "update-catalog";

    public override string Description => "Updates local copy of the API catalog";

    public override void AddOptions(OptionSet options)
    {
        options.Add("f|force", "Forces downloading the catalog, even if the local version is up-to-date.", _ => _force = true);
    }

    public override void Execute()
    {
        _catalogService.DownloadCatalog(_force);
    }
}
