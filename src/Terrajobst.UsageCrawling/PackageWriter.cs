using Microsoft.Data.Sqlite;
using NuGet.Packaging.Core;

namespace Terrajobst.UsageCrawling;

public sealed class PackageWriter : IDisposable
{
    private readonly SqliteCommand _command;
    private readonly SqliteTransaction _transaction;
    private readonly SqliteParameter _packageIdParameter;
    private readonly SqliteParameter _nameParameter;
    private readonly SqliteParameter _versionParameter;

    public PackageWriter(SqliteConnection connection)
    {
        _transaction = connection.BeginTransaction();
        _command = new SqliteCommand(@"
            INSERT INTO Packages
                (PackageId, Name, Version)
            VALUES
                (@PackageId, @Name, @Version)
        ", connection, _transaction);

        _packageIdParameter = _command.Parameters.Add("PackageId", SqliteType.Integer);
        _nameParameter = _command.Parameters.Add("Name", SqliteType.Text);
        _versionParameter = _command.Parameters.Add("Version", SqliteType.Text);
    }

    public Task WriteAsync(int packageId, PackageIdentity identity)
    {
        _packageIdParameter.Value = packageId;
        _nameParameter.Value = identity.Id;
        _versionParameter.Value = identity.Version.ToString();
        return _command.ExecuteNonQueryAsync();
    }

    public Task SaveAsync()
    {
        return _transaction.CommitAsync();
    }

    public void Dispose()
    {
        _command.Dispose();
        _transaction.Dispose();
    }
}