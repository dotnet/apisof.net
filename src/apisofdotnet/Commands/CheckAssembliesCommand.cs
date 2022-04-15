using System.Collections.Concurrent;

using Microsoft.Cci.Extensions;

using Mono.Options;

using NuGet.Frameworks;

using Spectre.Console;

internal sealed class CheckAssembliesCommand : Command
{
    private readonly List<string> _inputPaths = new();
    private readonly List<string> _targetFrameworkNames = new();
    private string _outputPath = "";

    public override string Name => "check-assemblies";

    public override string Description => "Checks framework compatibility of assemblies";

    public override void AddOptions(OptionSet options)
    {
        options.Add("t|target=", "The {target} framework to check availability for", v => _targetFrameworkNames.Add(v));
        options.Add("o|out=", "The {filename} of the report ", v => _outputPath = v);
        options.Add("<>", null, v => _inputPaths.Add(v));
    }

    public override void Execute()
    {
        if (_inputPaths.Count == 0)
        {
            Console.Error.WriteLine($"error: need to specify at least one input path");
            return;
        }

        if (_targetFrameworkNames.Count == 0)
            _targetFrameworkNames.AddRange(Defaults.TargetFrameworks);

        var targetFrameworks = _targetFrameworkNames.Select(NuGetFramework.Parse)
                                                    .ToArray();

        if (string.IsNullOrEmpty(_outputPath))
        {
            Console.Error.WriteLine($"error: need to specify output path");
            return;
        }

        var filePaths =
            AnsiConsole
                .Status()
                .Start("Discovering files", _ => AssemblyFileSet.Create(_inputPaths));

        var report =
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

                    var report = new ConcurrentBag<AssemblyResult>();

                    Parallel.ForEach(filePaths, filePath =>
                    {
                        using var env = new HostEnvironment();
                        var assembly = env.LoadAssemblyFrom(filePath);
                        var assemblyName = assembly is not null
                                            ? assembly.Name.Value
                                            : Path.GetFileName(filePath);

                        if (assembly is null)
                        {
                            var issue = "Not a valid .NET assembly";
                            var results = new AssemblyResult(assemblyName, issue, null, Array.Empty<AssemblyTargetFrameworkCompatibility>());
                            report.Add(results);
                        }
                        else
                        {
                            var tfm = assembly.GetTargetFrameworkMoniker();
                            var assemblyFramework = string.IsNullOrEmpty(tfm) ? null : NuGetFramework.Parse(tfm);
                            var assemblyFrameworkName = assemblyFramework?.GetShortFolderName();
                            var frameworkResults = targetFrameworks.Select(fx => AssemblyTargetFrameworkCompatibility.Compute(assemblyFramework, fx))
                                                                   .ToArray();
                            var results = new AssemblyResult(assemblyName, null, assemblyFrameworkName, frameworkResults);
                            report.Add(results);
                        }

                        task.Increment(1);
                    });

                    return report;
                });

        var reportRows = report.Select(r => (
                                    r.AssemblyName,
                                    r.AssemblyIssue,
                                    r.AssemblyFramework,
                                    r.FrameworkResults
                                ))
                               .OrderBy(t => t.AssemblyName);

        var outputDirectory = Path.GetDirectoryName((string?)_outputPath);
        if (outputDirectory is not null)
            Directory.CreateDirectory(outputDirectory);

        using var writer = new CsvWriter(_outputPath);

        writer.Write("Assembly");
        writer.Write("Assembly Issue");
        writer.Write("Assembly Framework");

        foreach (var tfm in _targetFrameworkNames)
        {
            writer.Write(tfm);
        }

        writer.WriteLine();

        foreach (var row in reportRows)
        {
            writer.Write(row.AssemblyName);
            writer.Write(row.AssemblyIssue);
            writer.Write(row.AssemblyFramework);

            foreach (var tfm in row.FrameworkResults)
                writer.Write(tfm);

            writer.WriteLine();
        }
    }

    private record struct AssemblyResult(string AssemblyName,
                                         string? AssemblyIssue,
                                         string? AssemblyFramework,
                                         IReadOnlyList<AssemblyTargetFrameworkCompatibility> FrameworkResults);
}