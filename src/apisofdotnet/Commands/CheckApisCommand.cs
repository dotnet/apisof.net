using Mono.Options;

using NuGet.Frameworks;

using Spectre.Console;

internal sealed class CheckApisCommand : Command
{
    private readonly CatalogService _catalogService;
    private readonly List<string> _inputPaths = new();
    private readonly List<string> _targetFrameworkNames = new();
    private string _outputPath = "";

    public CheckApisCommand(CatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public override string Name => "check-apis";

    public override string Description => "Checks availability of used APIs";

    public override void AddOptions(OptionSet options)
    {
        options.Add("t|target=", "The {target} framework to check availability for", v => _targetFrameworkNames.Add(v));
        options.Add("o|out=", "The {filename} of the report", v => _outputPath = v);
        options.Add("<>", null, v => _inputPaths.Add(v));
    }

    public override void Execute()
    {
        var catalog = _catalogService.LoadCatalog();

        foreach (var targetFramework in _targetFrameworkNames)
        {
            var isValid = catalog.Frameworks.Any(fx => string.Equals(fx.Name, targetFramework, StringComparison.OrdinalIgnoreCase));
            if (!isValid)
            {
                Console.WriteLine($"error: '{targetFramework}' isn't a known target framework.");
                return;
            }
        }

        var frameworks = _targetFrameworkNames.Select(NuGetFramework.Parse).ToArray();

        var outputDirectory = Path.GetDirectoryName(_outputPath);
        if (outputDirectory is not null)
            Directory.CreateDirectory(outputDirectory);

        using var writer = new StreamWriter(_outputPath);

        writer.Write("Assembly");
        writer.Write('\t');
        writer.Write("Assembly Issue");
        writer.Write('\t');
        writer.Write("Namespace");
        writer.Write('\t');
        writer.Write("Type");
        writer.Write('\t');
        writer.Write("Member");

        foreach (var framework in _targetFrameworkNames)
        {
            writer.Write('\t');
            writer.Write(framework);
        }

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

                ApiAvailabilityChecker.Run(catalog, filePaths, frameworks, result =>
                {
                    if (!string.IsNullOrEmpty(result.AssemblyIssues))
                    {
                        writer.Write(result.AssemblyName);
                        writer.Write('\t');
                        writer.Write(result.AssemblyIssues);
                        writer.Write('\t');
                        writer.Write('\t');
                        writer.Write('\t');

                        foreach (var frameworkResult in frameworks)
                            writer.Write('\t');

                        writer.WriteLine();
                    }
                    else
                    {
                        foreach (var apiResult in result.ApiResults)
                        {
                            var namespaceName = apiResult.Api.GetNamespaceName();
                            var typeName = apiResult.Api.GetTypeName();
                            var memberName = apiResult.Api.GetMemberName();

                            writer.Write(result.AssemblyName);
                            writer.Write('\t');
                            writer.Write('\t');
                            writer.Write(namespaceName);
                            writer.Write('\t');
                            writer.Write(typeName);
                            writer.Write('\t');
                            writer.Write(memberName);

                            foreach (var frameworkResult in apiResult.FrameworkResults)
                            {
                                writer.Write('\t');
                                writer.Write(frameworkResult);
                            }

                            writer.WriteLine();
                        }
                    }

                    task.Increment(1);
                });
            });
    }
}
