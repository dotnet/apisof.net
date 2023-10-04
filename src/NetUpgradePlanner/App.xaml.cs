using System.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NetUpgradePlanner.Services;
using NetUpgradePlanner.ViewModels.AssemblyListView;
using NetUpgradePlanner.ViewModels.MainWindow;
using NetUpgradePlanner.Views;

using Squirrel;

namespace NetUpgradePlanner;

// TODO: Looks like we need to improve perf; analyzing VS is extremely slow.
// TODO: Support multi-selection in GraphView
// TODO: Can we be smarter about NuGet packages?

internal sealed partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        _host = Host.CreateDefaultBuilder(e.Args)
                    .ConfigureServices(ConfigureServices)
                    .Build();
        _host.Start();

        SquirrelAwareApp.HandleEvents(
            onInitialInstall: OnAppInstall,
            onAppUninstall: OnAppUninstall,
            onEveryRun: OnAppRun,
            arguments: e.Args
        );

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);

        var fileNames = e.Args.Where(a => !a.StartsWith("--")).ToArray();

        if (fileNames.Length == 1)
        {
            var fileName = e.Args[0];

            var workspaceDocumentService = _host.Services.GetRequiredService<WorkspaceDocumentService>();
            await workspaceDocumentService.LoadAsync(fileName);
        }
    }

    private static void OnAppInstall(SemanticVersion version, IAppTools tools)
    {
        tools.CreateShortcutForThisExe(ShortcutLocation.StartMenu);
        tools.CreateUninstallerRegistryEntry();
        FileExtensionManager.RegisterFileExtensions();
    }

    private static void OnAppUninstall(SemanticVersion version, IAppTools tools)
    {
        FileExtensionManager.UnregisterFileExtensions();
        tools.RemoveShortcutForThisExe(ShortcutLocation.StartMenu);
    }

    private void OnAppRun(SemanticVersion version, IAppTools tools, bool firstRun)
    {
        tools.SetProcessAppUserModelId();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.StopAsync();
        _host?.WaitForShutdown();

        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Hosted services

        services.AddHostedService(sp => sp.GetRequiredService<TelemetryService>());
        services.AddHostedService(sp => sp.GetRequiredService<UpdateService>());

        // Services

        services.AddSingleton<TelemetryService>();
        services.AddSingleton<CatalogService>();
        services.AddSingleton<ProgressService>();
        services.AddSingleton<AssemblyContextMenuService>();
        services.AddSingleton<AssemblySelectionService>();
        services.AddSingleton<WorkspaceService>();
        services.AddSingleton<WorkspaceDocumentService>();
        services.AddSingleton<IconService>();
        services.AddSingleton<SelectFrameworkDialogService>();
        services.AddSingleton<SelectPlatformsDialogService>();
        services.AddSingleton<UpdateService>();
        services.AddSingleton<OfflineDetectionService>();

        // Main Window

        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<ProgressViewModel>();

        // Views

        services.AddSingleton<AssemblyListView>();
        services.AddSingleton<AssemblyListViewModel>();
        services.AddSingleton<ProblemListView>();
        services.AddSingleton<ProblemListViewModel>();
        services.AddSingleton<GraphView>();
    }
}
