using NuGet.Frameworks;

using Terrajobst.ApiCatalog;

internal sealed class ListFrameworksCommand : Command
{
    private readonly CatalogService _catalogService;

    public ListFrameworksCommand(CatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public override string Name => "list-frameworks";

    public override string Description => "Lists known frameworks";

    public override void Execute()
    {
        var catalog = _catalogService.LoadCatalog();
        var frameworks = catalog.Frameworks.Select(fx => NuGetFramework.Parse(fx.Name))
                                           .Select(fx => (DisplayName: GetDisplayName(fx), Framework: fx))
                                           .Where(t => t.DisplayName is not null)
                                           .GroupBy(fx => fx.DisplayName, fx => fx.Framework)
                                           .OrderBy(g => g.Key)
                                           .ToArray();

        var isFirstGroup = true;

        foreach (var frameworkGroup in frameworks)
        {
            if (isFirstGroup)
            {
                Console.WriteLine("Available frameworks:");
                isFirstGroup = false;
            }
            else
            {
                Console.WriteLine();
            }

            var tfms = frameworkGroup.OrderBy(fx => fx.Version)
                                     .ThenBy(fx => fx.Platform)
                                     .ThenBy(fx => fx.Version)
                                     .Select(fx => fx.GetShortFolderName());
            var isFirstTfm = true;

            foreach (var tfm in tfms)
            {
                if (isFirstTfm)
                {
                    Console.WriteLine($"  {frameworkGroup.Key,-25}{tfm}");
                    isFirstTfm = false;
                }
                else
                {
                    Console.WriteLine($"  {string.Empty,-25}{tfm}");
                }
            }
        }

        static string? GetDisplayName(NuGetFramework framework)
        {
            if (!framework.HasProfile)
            {
                if (framework.Framework == ".NETFramework" ||
                    framework.Framework == ".NETCoreApp" ||
                    framework.Framework == ".NETStandard")
                {
                    return framework.GetFrameworkDisplayString();
                }
            }

            return null;
        }
    }
}