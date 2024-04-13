using Dapper;
using Microsoft.Data.Sqlite;

namespace Terrajobst.UsageCrawling.Storage;

public sealed class UsageDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private SqliteTransaction _transaction;
    private readonly IdMap<string> _referenceUnitIdMap;
    private readonly IdMap<Guid> _featureIdMap;

    private readonly InsertReferenceUnitCommand _insertReferenceUnitCommand;
    private readonly DeleteReferenceUnitCommand _deleteReferenceUnitCommand;
    private readonly InsertFeatureCommand _insertFeatureCommand;
    private readonly DeleteFeatureCommand _deleteFeatureCommand;
    private readonly InsertUsageCommand _insertUsageCommand;
    private readonly InsertParentFeatureCommand _insertParentFeature;

    private UsageDatabase(SqliteConnection connection, SqliteTransaction transaction, IdMap<string> referenceUnitIdMap, IdMap<Guid> featureIdMap)
    {
        _connection = connection;
        _transaction = transaction;
        _referenceUnitIdMap = referenceUnitIdMap;
        _featureIdMap = featureIdMap;
        _insertReferenceUnitCommand = new InsertReferenceUnitCommand(connection, _transaction);
        _deleteReferenceUnitCommand = new DeleteReferenceUnitCommand(connection, _transaction);
        _insertFeatureCommand = new InsertFeatureCommand(connection, _transaction);
        _deleteFeatureCommand = new DeleteFeatureCommand(connection, _transaction);
        _insertParentFeature = new InsertParentFeatureCommand(connection, _transaction);
        _insertUsageCommand = new InsertUsageCommand(connection, _transaction);
    }

    public void Dispose()
    {
        _transaction.Commit();
        _insertReferenceUnitCommand.Dispose();
        _insertFeatureCommand.Dispose();
        _insertParentFeature.Dispose();
        _insertUsageCommand.Dispose();
        _transaction.Dispose();
        _connection.Dispose();
    }

    public async Task OpenAsync()
    {
        await _connection.OpenAsync();
        await CreateSchemaIfNotExistsAsync(_connection);
        BeginTransaction();
    }

    public async Task CloseAsync()
    {
        await _transaction.CommitAsync();
        await _transaction.DisposeAsync();
        await _connection.CloseAsync();
    }

    private void BeginTransaction()
    {
        _transaction = _connection.BeginTransaction();
        _insertReferenceUnitCommand.Transaction = _transaction;
        _deleteReferenceUnitCommand.Transaction = _transaction;
        _insertFeatureCommand.Transaction = _transaction;
        _deleteFeatureCommand.Transaction = _transaction;
        _insertParentFeature.Transaction = _transaction;
        _insertUsageCommand.Transaction = _transaction;
    }

    public static async Task<UsageDatabase> OpenOrCreateAsync(string fileName)
    {
        ThrowIfNullOrEmpty(fileName);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fileName,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        }.ToString();

        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync("PRAGMA JOURNAL_MODE = OFF");
        await connection.ExecuteAsync("PRAGMA SYNCHRONOUS = OFF");

        var referenceUnitIdMap = new IdMap<string>();
        var featureIdMap = new IdMap<Guid>();

        await CreateSchemaIfNotExistsAsync(connection);
        await GetReferenceUnitsAsync(connection, referenceUnitIdMap);
        await GetFeaturesAsync(connection, featureIdMap);

        var transaction = connection.BeginTransaction();

        return new UsageDatabase(connection, transaction, referenceUnitIdMap, featureIdMap);
    }

    private static async Task GetReferenceUnitsAsync(SqliteConnection connection, IdMap<string> referenceUnitIdMap)
    {
        var results = connection.QueryUnbufferedAsync<(int Id, string Identifier)>(
            """
            SELECT  ReferenceUnitId, Identifier
            FROM    ReferenceUnits
            """);

        await foreach (var (id, identifier) in results)
            referenceUnitIdMap.Add(id, identifier);
    }

    private static async Task GetFeaturesAsync(SqliteConnection connection, IdMap<Guid> featureIdMap)
    {
        var results = connection.QueryUnbufferedAsync<(int Id, string GuidText)>(
            """
            SELECT  FeatureId, Guid
            FROM    Features
            """);

        await foreach (var (id, guidText) in results)
        {
            var guid = Guid.Parse(guidText);
            featureIdMap.Add(id, guid);
        }
    }

    private static async Task CreateSchemaIfNotExistsAsync(SqliteConnection connection)
    {
        var schema =
            """
            CREATE TABLE IF NOT EXISTS [ReferenceUnits]
            (
                [ReferenceUnitId]  INTEGER PRIMARY KEY,
                [Identifier]       TEXT    NOT NULL,
                [CollectorVersion] INTEGER NOT NULL
            ) WITHOUT ROWID;

            CREATE INDEX IF NOT EXISTS [IX_ReferenceUnits_CollectorVersion]
            ON [ReferenceUnits] ([CollectorVersion]);

            CREATE TABLE IF NOT EXISTS [Features]
            (
                [FeatureId]        INTEGER PRIMARY KEY,
                [Guid]             TEXT    NOT NULL,
                [CollectorVersion] INTEGER NOT NULL
            ) WITHOUT ROWID;

            CREATE TABLE IF NOT EXISTS [ParentFeatures]
            (
                [ChildFeatureId]   INTEGER NOT NULL,
                [ParentFeatureId]  INTEGER NOT NULL,
                PRIMARY KEY ([ChildFeatureId], [ParentFeatureId])
            ) WITHOUT ROWID;

            CREATE TABLE IF NOT EXISTS [Usages]
            (
                [ReferenceUnitId]  INTEGER NOT NULL,
                [FeatureId]        INTEGER NOT NULL,
                PRIMARY KEY ([ReferenceUnitId], [FeatureId])
            ) WITHOUT ROWID;
            """;

        await connection.ExecuteAsync(schema);
    }

    public async Task VacuumAsync()
    {
        await _transaction.CommitAsync();
        await _connection.ExecuteAsync("VACUUM");
        BeginTransaction();
    }

    public Task<IEnumerable<(string Identifier, int CollectorVersion)>> GetReferenceUnitsAsync()
    {
        return _connection.QueryAsync<(string Identifier, int CollectorVersion)>(
            """
            SELECT  Identifier,
                    CollectorVersion
            FROM    ReferenceUnits
            """, transaction: _transaction);
    }

    public async Task<IEnumerable<(Guid Feature, int CollectorVersion)>> GetFeaturesAsync()
    {
        var results = _connection.QueryUnbufferedAsync<(string GuidText, int CollectorVersion)>(
            """
            SELECT  Guid, CollectorVersion
            FROM    Features
            """, transaction: _transaction);

        var result = new List<(Guid, int)>();

        await foreach (var (guidText, collectorVersion) in results)
        {
            var guid = Guid.Parse(guidText);
            result.Add((guid, collectorVersion));
        }

        return result;
    }

    public async Task DeleteReferenceUnitsAsync(IEnumerable<string> referenceUnitIdentifiers)
    {
        ThrowIfNull(referenceUnitIdentifiers);

        foreach (var referenceUnit in referenceUnitIdentifiers)
        {
            if (!_referenceUnitIdMap.TryGetId(referenceUnit, out var referenceUnitId))
                continue;

            await _deleteReferenceUnitCommand.ExecuteAsync(referenceUnitId);
        }
    }

    public async Task DeleteFeaturesAsync(IEnumerable<Guid> features)
    {
        ThrowIfNull(features);

        foreach (var feature in features)
        {
            if (!_featureIdMap.TryGetId(feature, out var featureId))
                continue;

            await _deleteFeatureCommand.ExecuteAsync(featureId);
        }
    }

    public async ValueTask AddReferenceUnitAsync(string identifier, int collectorVersion = 0)
    {
        ThrowIfNullOrEmpty(identifier);

        if (_referenceUnitIdMap.Contains(identifier))
            throw new ArgumentException($"The reference unit '{identifier}' already exists", nameof(identifier));

        var referenceUnitId = _referenceUnitIdMap.Add(identifier);
        await _insertReferenceUnitCommand.ExecuteAsync(referenceUnitId, identifier, collectorVersion);
    }

    public async ValueTask<bool> TryAddFeatureAsync(Guid feature, int collectorVersion = 0)
    {
        if (_featureIdMap.Contains(feature))
            return false;

        var featureId = _featureIdMap.Add(feature);
        await _insertFeatureCommand.ExecuteAsync(featureId, feature, collectorVersion);
        return true;
    }

    public async ValueTask AddParentFeatureAsync(Guid childFeature, Guid parentFeature)
    {
        if (!_featureIdMap.TryGetId(childFeature, out var childFeatureId))
            throw new ArgumentException($"Child feature '{childFeature}' wasn't found'", nameof(childFeature));

        if (!_featureIdMap.TryGetId(parentFeature, out var parentFeatureId))
            throw new ArgumentException($"Parent feature '{parentFeature}' wasn't found'", nameof(parentFeature));

        await _insertParentFeature.ExecuteAsync(childFeatureId, parentFeatureId);
    }

    public async ValueTask AddUsageAsync(string referenceUnitIdentifier, Guid feature)
    {
        ThrowIfNullOrEmpty(referenceUnitIdentifier);

        if (!_referenceUnitIdMap.TryGetId(referenceUnitIdentifier, out var referenceUnit))
            throw new ArgumentException($"Reference unit '{referenceUnitIdentifier}' wasn't found'", nameof(referenceUnitIdentifier));

        if (!_featureIdMap.TryGetId(feature, out var featureId))
            throw new ArgumentException($"Feature '{feature}' wasn't found'", nameof(feature));

        await _insertUsageCommand.ExecuteAsync(referenceUnit, featureId);
    }

    public async Task<IReadOnlyCollection<(Guid Feature, float percentage)>> GetUsagesAsync()
    {
        var maxVersion = await _connection.ExecuteScalarAsync<int>(
            """
            SELECT MAX(CollectorVersion) FROM ReferenceUnits
            """,
            transaction: _transaction);
        await _connection.ExecuteAsync(
            """
            DROP TABLE IF EXISTS ReferenceUnitCounts;
            CREATE TABLE ReferenceUnitCounts
            (
                [CollectorVersion] INTEGER PRIMARY KEY,
                [Count]            INTEGER NOT NULL
            ) WITHOUT ROWID;
            """, transaction: _transaction);

        for (var version = 0; version <= maxVersion; version++)
        {
            await _connection.ExecuteAsync(
                """
                INSERT INTO ReferenceUnitCounts(CollectorVersion, Count)
                VALUES (@CollectorVersion, (SELECT COUNT(*) FROM ReferenceUnits WHERE ReferenceUnits.CollectorVersion >= @CollectorVersion))
                """, new { CollectorVersion= version }, transaction: _transaction);
        }

        var results = _connection.QueryUnbufferedAsync<(int FeatureId, float Percentage)>(
            """
            SELECT   COALESCE(a.ParentFeatureId, u.FeatureId) AS FeatureId,
                     CAST(COUNT(DISTINCT u.ReferenceUnitId) AS REAL) / (SELECT r.Count FROM ReferenceUnitCounts r WHERE r.CollectorVersion = f.CollectorVersion) AS Percentage
            FROM     Usages u
                         JOIN Features f ON f.FeatureId = u.FeatureId
                         LEFT JOIN ParentFeatures a ON a.ChildFeatureId = u.FeatureId
            GROUP BY COALESCE(a.ParentFeatureId, u.FeatureId)
            """, transaction: _transaction);

        var result = new List<(Guid, float)>();

        await foreach (var item in results)
        {
            var feature = _featureIdMap.GetValue(item.FeatureId);
            result.Add((feature, item.Percentage));
        }

        await _connection.ExecuteAsync(
            """
            DROP TABLE IF EXISTS ReferenceUnitCounts;
            """, transaction: _transaction);

        return result;
    }

    public async Task ExportUsagesAsync(string fileName)
    {
        ThrowIfNullOrEmpty(fileName);

        var results = await GetUsagesAsync();

        await using var writer = new StreamWriter(fileName);
        foreach (var (feature, percentage) in results)
            await writer.WriteLineAsync($"{feature:N}\t{percentage}");
    }

    private sealed class InsertReferenceUnitCommand : IDisposable
    {
        private readonly SqliteCommand _command;
        private readonly SqliteParameter _referenceUnitIdParameter;
        private readonly SqliteParameter _identifierParameter;
        private readonly SqliteParameter _collectorVersionParameter;

        public InsertReferenceUnitCommand(SqliteConnection connection, SqliteTransaction transaction)
        {
            _command = new SqliteCommand(
                """
                INSERT INTO ReferenceUnits
                    (ReferenceUnitId, Identifier, CollectorVersion)
                VALUES
                    (@ReferenceUnitId, @Identifier, @CollectorVersion)
                """, connection, transaction);

            _referenceUnitIdParameter = _command.Parameters.Add("@ReferenceUnitId", SqliteType.Integer);
            _identifierParameter = _command.Parameters.Add("@Identifier", SqliteType.Text);
            _collectorVersionParameter = _command.Parameters.Add("@CollectorVersion", SqliteType.Integer);
        }

        public void Dispose()
        {
            _command.Dispose();
        }

        public async ValueTask ExecuteAsync(int referenceUnitId, string identifier, int collectorVersion)
        {
            _referenceUnitIdParameter.Value = referenceUnitId;
            _identifierParameter.Value = identifier;
            _collectorVersionParameter.Value = collectorVersion;
            await _command.ExecuteNonQueryAsync();
        }

        public SqliteTransaction? Transaction
        {
            get => _command.Transaction;
            set => _command.Transaction = value;
        }
    }

    private sealed class DeleteReferenceUnitCommand : IDisposable
    {
        private readonly SqliteCommand _command;
        private readonly SqliteParameter _referenceUnitIdParameter;

        public DeleteReferenceUnitCommand(SqliteConnection connection, SqliteTransaction transaction)
        {
            _command = new SqliteCommand(
                """
                DELETE FROM ReferenceUnits WHERE ReferenceUnitId = @ReferenceUnitId;
                DELETE FROM Usages WHERE ReferenceUnitId = @ReferenceUnitId;
                """, connection, transaction);

            _referenceUnitIdParameter = _command.Parameters.Add("@ReferenceUnitId", SqliteType.Integer);
        }

        public void Dispose()
        {
            _command.Dispose();
        }

        public async ValueTask ExecuteAsync(int referenceUnitId)
        {
            _referenceUnitIdParameter.Value = referenceUnitId;
            await _command.ExecuteNonQueryAsync();
        }

        public SqliteTransaction? Transaction
        {
            get => _command.Transaction;
            set => _command.Transaction = value;
        }
    }

    private sealed class InsertFeatureCommand : IDisposable
    {
        private readonly SqliteCommand _command;
        private readonly SqliteParameter _featureIdParameter;
        private readonly SqliteParameter _guidParameter;
        private readonly SqliteParameter _collectorVersionParameter;

        public InsertFeatureCommand(SqliteConnection connection, SqliteTransaction transaction)
        {
            _command = new SqliteCommand(
                """
                INSERT INTO Features
                    (FeatureId, Guid, CollectorVersion)
                VALUES
                    (@FeatureId, @Guid, @CollectorVersion)
                """, connection, transaction);

            _featureIdParameter = _command.Parameters.Add("@FeatureId", SqliteType.Integer);
            _guidParameter = _command.Parameters.Add("@Guid", SqliteType.Text);
            _collectorVersionParameter = _command.Parameters.Add("@CollectorVersion", SqliteType.Integer);
        }

        public void Dispose()
        {
            _command.Dispose();
        }

        public async ValueTask ExecuteAsync(int featureId, Guid feature, int collectorVersion)
        {
            _featureIdParameter.Value = featureId;
            _guidParameter.Value = feature;
            _collectorVersionParameter.Value = collectorVersion;
            await _command.ExecuteNonQueryAsync();
        }

        public SqliteTransaction? Transaction
        {
            get => _command.Transaction;
            set => _command.Transaction = value;
        }
    }

    private sealed class DeleteFeatureCommand : IDisposable
    {
        private readonly SqliteCommand _command;
        private readonly SqliteParameter _featureIdParameter;

        public DeleteFeatureCommand(SqliteConnection connection, SqliteTransaction transaction)
        {
            // NOTE: We don't delete usages here because the table has no row id and its primary
            //       key is defined as (reference unit id, feature id) so deleting by feature id
            //       requires a full table scan.
            //       However, since we're only using this to trim the export functionality we can
            //       ignore this because we do an INNER JOIN with the Feature table which reject
            //       those rows.

            _command = new SqliteCommand(
                """
                DELETE FROM Features WHERE FeatureId = @FeatureId;
                """, connection, transaction);

            _featureIdParameter = _command.Parameters.Add("@FeatureId", SqliteType.Integer);
        }

        public void Dispose()
        {
            _command.Dispose();
        }

        public async ValueTask ExecuteAsync(int featureId)
        {
            _featureIdParameter.Value = featureId;
            await _command.ExecuteNonQueryAsync();
        }

        public SqliteTransaction? Transaction
        {
            get => _command.Transaction;
            set => _command.Transaction = value;
        }
    }

    private sealed class InsertParentFeatureCommand : IDisposable
    {
        private readonly SqliteCommand _command;
        private readonly SqliteParameter _childFeatureIdParameter;
        private readonly SqliteParameter _parentFeatureIdParameter;

        public InsertParentFeatureCommand(SqliteConnection connection, SqliteTransaction transaction)
        {
            _command = new SqliteCommand(
                """
                INSERT INTO [ParentFeatures]
                    ([ChildFeatureId], [ParentFeatureId])
                VALUES
                    (@ChildFeatureId, @ParentFeatureId)
                """, connection, transaction);

            _childFeatureIdParameter = _command.Parameters.Add("@ChildFeatureId", SqliteType.Integer);
            _parentFeatureIdParameter = _command.Parameters.Add("@ParentFeatureId", SqliteType.Integer);
        }

        public void Dispose()
        {
            _command.Dispose();
        }

        public async ValueTask ExecuteAsync(int childFeatureId, int parentFeatureId)
        {
            _childFeatureIdParameter.Value = childFeatureId;
            _parentFeatureIdParameter.Value = parentFeatureId;

            await _command.ExecuteNonQueryAsync();
        }

        public SqliteTransaction? Transaction
        {
            get => _command.Transaction;
            set => _command.Transaction = value;
        }
    }

    private sealed class InsertUsageCommand : IDisposable
    {
        private readonly SqliteCommand _command;
        private readonly SqliteParameter _referenceUnitIdParameter;
        private readonly SqliteParameter _featureIdParameter;

        public InsertUsageCommand(SqliteConnection connection, SqliteTransaction transaction)
        {
            _command = new SqliteCommand(
                """
                INSERT INTO Usages
                    (ReferenceUnitId, FeatureId)
                VALUES
                    (@ReferenceUnitId, @FeatureId)
                """, connection, transaction);

            _referenceUnitIdParameter = _command.Parameters.Add("@ReferenceUnitId", SqliteType.Integer);
            _featureIdParameter = _command.Parameters.Add("@FeatureId", SqliteType.Integer);
        }

        public void Dispose()
        {
            _command.Dispose();
        }

        public async ValueTask ExecuteAsync(int referenceUnitId, int featureId)
        {
            _referenceUnitIdParameter.Value = referenceUnitId;
            _featureIdParameter.Value = featureId;
            await _command.ExecuteNonQueryAsync();
        }

        public SqliteTransaction? Transaction
        {
            get => _command.Transaction;
            set => _command.Transaction = value;
        }
    }
}