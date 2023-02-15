# .NET Upgrade Planner Programmatic Access

I highly recommend that you start with the UI application, which you can find
[here]. However, you can also use the NuGet packages in order to query the
API catalog and the analysis engine programmatically.

In order to use it, you'll need to reference

* [Terrajobst.NetUpgradePlanner](https://nuget.org/packages/Terrajobst.NetUpgradePlanner)
* [Terrajobst.NetUpgradePlanner.Excel](https://nuget.org/packages/Terrajobst.NetUpgradePlanner.Excel)
  (Only needed if you want to produce an Excel report).

The code looks as follows:

```C#
using Terrajobst.ApiCatalog;
using Terrajobst.NetUpgradePlanner;

// This points to the directory that contains the binaries you want to
// analyze:
var binDirectory = @"C:\path\to\my\binaries";

// This is where the reports are being written to:
var reportDirectory = @"C:\path\to\my\reports";

// Load the catalog. To download the catalog only once, use
// DownloadFromWebAsync() instead.
var catalog = await ApiCatalogModel.LoadFromWebAsync();

// Load your assemblies.
var files = Directory.GetFiles(binDirectory, "*", SearchOption.AllDirectories);
var assemblySet = AssemblySet.Create(files);

// Set configuration. This is where you decide what framework/platform
// you want to target. You can set all assemblies to the same or make
// different decisions. For example, you may want to set your
// WinForms/WPF apps to target `net6.0-windows` and your business logic
// to `net6.0` or even `netstandard2.0`.

var configuration = AssemblyConfiguration.Empty;
foreach (var assembly in assemblySet.Entries)
{
    configuration = configuration.SetDesiredFramework(assembly, "net6.0")
                                 .SetDesiredPlatforms(assembly, PlatformSet.Any);
}

// Analyze the binaries. This will give you the list of problems.
var report = AnalysisReport.Analyze(catalog, assemblySet, configuration);

// The results are per assembly and can be accessed via AnalyzedAssemblies:
foreach (var assembly in report.AnalyzedAssemblies)
    Console.WriteLine($"{assembly.Entry.Name}, Score={assembly.Score:P1}, Problems={assembly.Problems.Count}");

// In order to save the results we need to construct a workspace:
var workspace = new Workspace(assemblySet, configuration, report);

// You can now save the report as a .nupproj file, which can be read by
// .NET Upgrade Planner application
await WorkspacePersistence.SaveAsync(workspace, Path.Join(reportDirectory, "result.nupproj"));

// You can also save the report as an Excel file. The machine that produces the file
// doesn't need to have Excel installed.
//
// Note: This requires adding a package reference to Terrajobst.NetUpgradePlanner.Excel.
await WorkspacePersistenceExcel.SaveAsync(workspace, Path.Join(reportDirectory, "result.xlsx"));
```

[planner-app]: https://apisof.net/upgrade-planner
