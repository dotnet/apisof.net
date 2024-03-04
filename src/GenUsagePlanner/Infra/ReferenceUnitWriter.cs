using Microsoft.Data.Sqlite;

namespace GenUsagePlanner.Infra;

public sealed class ReferenceUnitWriter : IDisposable
{
    private readonly SqliteCommand _command;
    private readonly SqliteTransaction _transaction;
    private readonly SqliteParameter _referenceUnitIdParameter;
    private readonly SqliteParameter _identifierParameter;

    public ReferenceUnitWriter(SqliteConnection connection)
    {
        _transaction = connection.BeginTransaction();
        _command = new SqliteCommand("""
            INSERT INTO ReferenceUnits
                (ReferenceUnitId, Identifier)
            VALUES
                (@ReferenceUnitId, @Identifier)
            """, connection, _transaction);

        _referenceUnitIdParameter = _command.Parameters.Add("ReferenceUnitId", SqliteType.Integer);
        _identifierParameter = _command.Parameters.Add("Identifier", SqliteType.Text);
    }

    public Task WriteAsync(int referenceUnitId, string identifier)
    {
        _referenceUnitIdParameter.Value = referenceUnitId;
        _identifierParameter.Value = identifier;
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