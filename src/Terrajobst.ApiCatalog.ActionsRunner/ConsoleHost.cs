using System.Reflection;
using GenUsagePlanner;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;

namespace Terrajobst.ApiCatalog.ActionsRunner;

public static class ConsoleHost
{
    public static HostApplicationBuilder CreateApplicationBuilder()
    {
        var builder = Host.CreateApplicationBuilder();

        if (!IsRunningInsideOfGitHubActions())
            builder.Environment.EnvironmentName = Environments.Development;

        var logFilePath = Path.Join(Path.GetDirectoryName(Environment.ProcessPath), "log.txt");

        builder.Logging.ClearProviders();
        builder.Services.AddSerilog((services, lc) => lc
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.File(logFilePath));

        builder.Configuration.AddUserSecrets(Assembly.GetEntryAssembly()!);
        builder.Services.AddHostedService<MainDispatcher>();
        builder.Services.AddSingleton<GitHubActionsEnvironment>();
        builder.Services.AddSingleton<GitHubActionsLog>();
        builder.Services.AddSingleton<GitHubActionsSummaryTable>();

        return builder;
    }

    private static bool IsRunningInsideOfGitHubActions()
    {
        return Environment.GetEnvironmentVariable("GITHUB_RUN_ID") is not null;
    }

    public static void AddApisOfDotNetPathProvider(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ApisOfDotNetPathProvider>();
    }

    public static void AddApisOfDotNetStore(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ApisOfDotNetStore>();
        builder.Services.AddOptions<ApisOfDotNetStoreOptions>()
            .BindConfiguration("")
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    public static void AddApisOfDotNetWebHook(this IHostApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddSingleton<ApisOfDotNetWebHook, ApisOfDotNetWebHookFake>();
        }
        else
        {
            builder.Services.AddSingleton<ApisOfDotNetWebHook, ApisOfDotNetWebHookReal>();
            builder.Services.AddOptions<ApisOfDotNetWebHookOptions>()
                .BindConfiguration("")
                .ValidateDataAnnotations()
                .ValidateOnStart();
        }
    }

    public static void AddScratchFileProvider(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ScratchFileProvider>();
    }

    public static void AddMain<T>(this IHostApplicationBuilder builder)
        where T: class, IConsoleMain
    {
        builder.Services.AddSingleton<IConsoleMain, T>();
    }

    public static void AddMainWithCommands(this IHostApplicationBuilder builder)
    {
        builder.AddMain<ConsoleMainForCommands>();

        var commandType = typeof(ConsoleCommand);
        var assembly = Assembly.GetEntryAssembly();
        if (assembly is null)
            return;

        var derivedTypes = assembly.GetTypes()
                                   .Where(t => !t.IsAbstract && t.IsAssignableTo(commandType));
        foreach (var derivedType in derivedTypes)
            builder.Services.AddSingleton(commandType, derivedType);
    }

    internal sealed class MainDispatcher : BackgroundService
    {
        private readonly IConsoleMain _main;
        private readonly IHost _host;

        public MainDispatcher(IConsoleMain main, IHostEnvironment environment, IHost host)
        {
            ThrowIfNull(main);
            ThrowIfNull(environment);
            ThrowIfNull(host);

            _main = main;
            _host = host;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
            try
            {
                await _main.RunAsync(args, stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Environment.ExitCode = 1;
            }
            finally
            {
                await _host.StopAsync(stoppingToken);
            }
        }
    }
}