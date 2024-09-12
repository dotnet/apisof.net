// NOTE: We need to run this from inside the Microsoft VPN as the Kusto cluster/DB isn't public.

using Kusto.Data.Net.Client;
using Kusto.Data;

using Terrajobst.ApiCatalog;

var repo = FindRepoRoot();
if (repo is null)
{
    Console.WriteLine("Can't find repository root");
    return;
}

var outputFileName = Path.Join(repo, "src", "Terrajobst.ApiCatalog.Generation", "Packages", "PackageIds.txt");

var cluster = "ddteldata.kusto.windows.net";
var databaseName = "ClientToolsInsights";
var predicate = string.Join(" or ", PlatformPackageDefinition.Owners.Select(n => $"set_has_element(Owners, \"{n}\")"));
var query = $"""
    NiPackageOwners
    | where {predicate}
    | project Id
    | order by Id asc
    """;

var connectionString = new KustoConnectionStringBuilder(cluster).WithAadUserPromptAuthentication();
using var queryProvider = KustoClientFactory.CreateCslQueryProvider(connectionString);
using var reader = queryProvider.ExecuteQuery(databaseName, query, null);

using var outputWriter = new StreamWriter(outputFileName);

while (reader.Read())
{
    var packageId = reader.GetString(0);
    if (PlatformPackageDefinition.Filter.IsMatch(packageId))
        outputWriter.WriteLine(packageId);
}

static string? FindRepoRoot()
{
    var dir = Path.GetDirectoryName(Environment.ProcessPath);

    while (dir is not null && !Directory.Exists(Path.Join(dir, ".git")))
        dir = Path.GetDirectoryName(dir);

    return dir;
}