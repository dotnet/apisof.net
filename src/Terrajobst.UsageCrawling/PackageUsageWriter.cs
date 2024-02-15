using Microsoft.Data.Sqlite;

using NuGet.Packaging.Core;

namespace Terrajobst.UsageCrawling;

public sealed class PackageUsageWriter : IDisposable
{
    private readonly SqliteTransaction _transaction;

    private readonly SqliteCommand _packageCommand;
    private readonly SqliteParameter _packagePackageIdParameter;
    private readonly SqliteParameter _packageNameParameter;
    private readonly SqliteParameter _packageVersionParameter;

    private readonly SqliteCommand _usageCommand;
    private readonly SqliteParameter _usagePackageIdParameter;
    private readonly SqliteParameter _usageApiIdParameter;

    public PackageUsageWriter(SqliteConnection connection)
    {
        _transaction = connection.BeginTransaction();

        _packageCommand = new SqliteCommand(@"
            INSERT INTO Packages
                (PackageId, Name, Version)
            VALUES
                (@PackageId, @Name, @Version)
        ", connection, _transaction);

        _packagePackageIdParameter = _packageCommand.Parameters.Add("PackageId", SqliteType.Integer);
        _packageNameParameter = _packageCommand.Parameters.Add("Name", SqliteType.Text);
        _packageVersionParameter = _packageCommand.Parameters.Add("Version", SqliteType.Text);

        _usageCommand = new SqliteCommand(@"
            INSERT INTO Usages
                (PackageId, ApiId)
            VALUES
                (@PackageId, @ApiId)
        ", connection, _transaction);

        _usagePackageIdParameter = _usageCommand.Parameters.Add("PackageId", SqliteType.Integer);
        _usageApiIdParameter = _usageCommand.Parameters.Add("ApiId", SqliteType.Integer);
    }

    public Task WritePackageAsync(int packageId, PackageIdentity identity)
    {
        _packagePackageIdParameter.Value = packageId;
        _packageNameParameter.Value = identity.Id;
        _packageVersionParameter.Value = identity.Version.ToString();
        return _packageCommand.ExecuteNonQueryAsync();
    }

    public Task WriteUsageAsync(int packageId, int apiId)
    {
        _usagePackageIdParameter.Value = packageId;
        _usageApiIdParameter.Value = apiId;
        return _usageCommand.ExecuteNonQueryAsync();
    }

    public Task SaveAsync()
    {
        return _transaction.CommitAsync();
    }

    public void Dispose()
    {
        _packageCommand.Dispose();
        _usageCommand.Dispose();
        _transaction.Dispose();
    }
}