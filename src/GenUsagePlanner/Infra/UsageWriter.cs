using Microsoft.Data.Sqlite;

namespace GenUsagePlanner.Infra;

public sealed class UsageWriter : IDisposable
{
    private readonly SqliteCommand _command;
    private readonly SqliteTransaction _transaction;
    private readonly SqliteParameter _referenceUnitIdParameter;
    private readonly SqliteParameter _apiIdParameter;

    public UsageWriter(SqliteConnection connection)
    {
        _transaction = connection.BeginTransaction();
        _command = new SqliteCommand(@"
            INSERT INTO Usages
                (ReferenceUnitId, ApiId)
            VALUES
                (@ReferenceUnitId, @ApiId)
        ", connection, _transaction);

        _referenceUnitIdParameter = _command.Parameters.Add("ReferenceUnitId", SqliteType.Integer);
        _apiIdParameter = _command.Parameters.Add("ApiId", SqliteType.Integer);
    }

    public Task WriteAsync(int referenceUnitId, int apiId)
    {
        _referenceUnitIdParameter.Value = referenceUnitId;
        _apiIdParameter.Value = apiId;
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