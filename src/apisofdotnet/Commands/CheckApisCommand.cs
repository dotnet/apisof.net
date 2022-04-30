using Mono.Options;

using NuGet.Frameworks;

using Spectre.Console;
using Terrajobst.ApiCatalog;

internal sealed class CheckApisCommand : Command
{
    private readonly CatalogService _catalogService;
    private readonly List<string> _inputPaths = new();
    private readonly List<string> _targetFrameworkNames = new();
    private bool _analyzeObsoletion;
    private readonly List<string> _targetPlatformNames = new();
    private string _outputPath = "";

    public CheckApisCommand(CatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public override string Name => "check-apis";

    public override string Description => "Checks availability of used APIs";

    public override void AddOptions(OptionSet options)
    {
        var commandName = Path.GetFileNameWithoutExtension(Environment.ProcessPath);
        
        options.Add("t|target=", "The {target} framework to check availability for", v => _targetFrameworkNames.Add(v));
        options.Add("obs|obsoletion", "Include information about obsoleted APIs", v => _analyzeObsoletion = true);
        options.Add("p|platform=", "The OS {platform} to check availability for", v => _targetPlatformNames.Add(v));
        options.Add("o|out=", "The {filename} of the report, in CSV format", v => _outputPath = v);
        options.Add("<>", null, v => _inputPaths.Add(v));
        options.Add("");
        options.Add("Examples:");
        options.Add("");
        options.Add($"  {commandName} check-apis D:\\myapp C:\\src\\file.dll -t net6.0 -o result.csv");
        options.Add("");
        options.Add("  Analyze file.dll and all assemblies in directory D:\\myapp (and its");
        options.Add("  subdirectories) against .NET 6.0");
    }

    public override async Task ExecuteAsync()
    {
        var catalog = await _catalogService.LoadCatalogAsync();

        if (_inputPaths.Count == 0)
        {
            Console.Error.WriteLine($"error: need to specify at least one input path");
            return;
        }

        if (_targetFrameworkNames.Count == 0)
            _targetFrameworkNames.AddRange(Defaults.TargetFrameworks);

        foreach (var targetFramework in _targetFrameworkNames)
        {
            var isValid = catalog.Frameworks.Any(fx => string.Equals(fx.Name, targetFramework, StringComparison.OrdinalIgnoreCase));
            if (!isValid)
            {
                Console.Error.WriteLine($"error: '{targetFramework}' isn't a known target framework.");
                return;
            }
        }

        var frameworks = _targetFrameworkNames.Select(NuGetFramework.Parse).ToArray();

        var catalogPlatforms = catalog.Platforms.Select(p => PlatformAnnotationContext.ParsePlatform(p.Name).Name)
                                                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var platform in _targetPlatformNames)
        {
            var (platformName, _) = PlatformAnnotationContext.ParsePlatform(platform);
            if (!catalogPlatforms.Contains(platformName))
            {
                Console.Error.WriteLine($"error: '{platformName}' isn't a known platform");
                return;
            }
        }
        
        var platforms = _targetPlatformNames.ToArray();

        if (string.IsNullOrEmpty(_outputPath))
        {
            Console.Error.WriteLine($"error: need to specify output path");
            return;
        }

        _outputPath = Path.GetFullPath(_outputPath);

        var outputDirectory = Path.GetDirectoryName(_outputPath);
        if (outputDirectory is not null)
            Directory.CreateDirectory(outputDirectory);

        using var writer = new CsvWriter(_outputPath);

        writer.Write("Calling Assembly");
        writer.Write("Note");
        writer.Write("Namespace");
        writer.Write("Type");
        writer.Write("Member");

        foreach (var framework in frameworks)
            writer.Write(framework);

        if (_analyzeObsoletion)
        {
            foreach (var framework in frameworks)
                writer.Write($"{framework} obsoletion");
        }
        
        foreach (var framework in frameworks)
            foreach (var platform in platforms)
                writer.Write($"{framework} on {platform}");

        writer.WriteLine();

        var filePaths =
            AnsiConsole
                .Status()
                .Start("Discovering files", _ => AssemblyFileSet.Create(_inputPaths));

        AnsiConsole
            .Progress()
            .Columns(new ProgressColumn[]
            {
                new SpinnerColumn(),
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
            })
            .Start(c =>
            {
                var task = c.AddTask("Analyzing assemblies", maxValue: filePaths.Count);

                ApiChecker.Run(catalog, filePaths, frameworks, _analyzeObsoletion, platforms, result =>
                {
                    if (!string.IsNullOrEmpty(result.AssemblyIssues))
                    {
                        writer.Write(result.AssemblyName);
                        writer.Write(result.AssemblyIssues);

                        // Namespace
                        writer.Write();

                        // Type
                        writer.Write();

                        // Member
                        writer.Write();

                        // Frameworks
                        for (var i = 0; i < frameworks.Length; i++)
                            writer.Write();

                        // Obsoletion
                        if (_analyzeObsoletion)
                        {
                            for (var i = 0; i < frameworks.Length; i++)
                                writer.Write();
                        }

                        // Platforms
                        for (var i = 0; i < frameworks.Length * platforms.Length; i++)
                            writer.Write();

                        writer.WriteLine();
                    }
                    else
                    {
                        foreach (var apiResult in result.Apis)
                        {
                            if (!apiResult.IsRelevant())
                                continue;

                            var namespaceName = apiResult.Api.GetNamespaceName();
                            var typeName = apiResult.Api.GetTypeName();
                            var memberName = apiResult.Api.GetMemberName();

                            writer.Write(result.AssemblyName);
                            writer.Write();
                            writer.Write(namespaceName);
                            writer.Write(typeName);
                            writer.Write(memberName);

                            foreach (var frameworkResult in apiResult.FrameworkResults)
                                writer.Write(frameworkResult.Availability);

                            if (_analyzeObsoletion)
                            {
                                for (var i = 0; i < apiResult.FrameworkResults.Count; i++)
                                {
                                    var obsoletion = apiResult.FrameworkResults[i].Obsoletion;

                                    if (obsoletion is null)
                                        writer.Write();
                                    else
                                        writer.Write(obsoletion.Value.Message);
                                }
                            }

                            foreach (var frameworkResult in apiResult.FrameworkResults)
                            {
                                foreach (var platformResult in frameworkResult.Platforms)
                                    writer.Write(platformResult);
                            }

                            writer.WriteLine();
                        }
                    }

                    task.Increment(1);
                });
            });
    }
}