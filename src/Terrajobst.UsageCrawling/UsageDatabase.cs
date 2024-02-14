using Dapper;
using Microsoft.Data.Sqlite;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Terrajobst.UsageCrawling;

public sealed class UsageDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    private UsageDatabase(SqliteConnection connection)
    {
        _connection = connection;
    }

    public static async Task<UsageDatabase> OpenOrCreateAsync(string fileName)
    {
        var isNew = !File.Exists(fileName);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fileName,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync("PRAGMA JOURNAL_MODE = OFF");
        await connection.ExecuteAsync("PRAGMA SYNCHRONOUS = OFF");

        if (isNew)
            await CreateSchemaAsync(connection);

        return new UsageDatabase(connection);
    }

    private static async Task CreateSchemaAsync(SqliteConnection connection)
    {
        var commands = new[]
        {
            """
            CREATE TABLE [Packages]
            (
                [PackageId] INTEGER PRIMARY KEY,
                [Name]      Text    NOT NULL,
                [Version]   Text    NOT NULL
            ) WITHOUT ROWID
            """,
            """
            CREATE TABLE [Apis]
            (
                [ApiId] INTEGER PRIMARY KEY,
                [Guid]  Text    NOT NULL
            ) WITHOUT ROWID
            """,
            """
            CREATE TABLE [Usages]
            (
                [PackageId] INTEGER NOT NULL,
                [ApiId]     INTEGER NOT NULL,
                PRIMARY KEY ([PackageId], [ApiId])
            ) WITHOUT ROWID
            """,
        };

        await using var cmd = new SqliteCommand();
        cmd.Connection = connection;

        foreach (var sql in commands)
        {
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    public Task VacuumAsync()
    {
        return _connection.ExecuteAsync("VACUUM");
    }

    public async Task<IdMap<PackageIdentity>> ReadPackagesAsync()
    {
        var result = new IdMap<PackageIdentity>();
        var rows = await _connection.QueryAsync<(int Id, string Name, string Version)>("""
            SELECT  PackageId,
                    Name,
                    Version
            FROM    Packages
            """);

        foreach (var (id, name, versionText) in rows)
        {
            var version = NuGetVersion.Parse(versionText);
            var identity = new PackageIdentity(name, version);
            result.Add(id, identity);
        }

        return result;
    }

    public async Task<IdMap<Guid>> ReadApisAsync()
    {
        var result = new IdMap<Guid>();
        var rows = await _connection.QueryAsync<(int Id, string GuidText)>("""
            SELECT  ApiId,
                    Guid
            FROM    Apis
            """);

        foreach (var (id, guidText) in rows)
        {
            var guid = Guid.Parse(guidText);
            result.Add(id, guid);
        }

        return result;
    }

    public async Task InsertMissingApisAsync(IdMap<Guid> apis)
    {
        await using var transaction = _connection.BeginTransaction();
        await InsertMissingApisAsync(apis, transaction);
        await transaction.CommitAsync();
    }

    private async Task InsertMissingApisAsync(IdMap<Guid> apis, SqliteTransaction transaction)
    {
        var existingApis = await ReadApisAsync();

        await using var command = new SqliteCommand("""
            INSERT INTO Apis
                (ApiId, Guid)
            VALUES
                (@ApiId, @Guid)
            """, _connection, transaction);

        var apiIdParameter = command.Parameters.Add("ApiId", SqliteType.Integer);
        var guidParameter = command.Parameters.Add("Guid", SqliteType.Text);

        foreach (var (apiId, guid) in apis)
        {
            if (existingApis.Contains(guid))
                continue;

            apiIdParameter.Value = apiId;
            guidParameter.Value = guid;
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task DeletePackagesAsync(IEnumerable<int> packageIds)
    {
        await using var transaction = _connection.BeginTransaction();
        await using var command = new SqliteCommand("""
            DELETE FROM Packages WHERE PackageId = @PackageId;
            DELETE FROM Usages WHERE PackageId = @PackageId;
            """, _connection, transaction);

        var packageIdParameter = command.Parameters.Add("PackageId", SqliteType.Integer);

        foreach (var packageId in packageIds)
        {
            packageIdParameter.Value = packageId;
            await command.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    public PackageWriter CreatePackageWriter()
    {
        return new PackageWriter(_connection);
    }

    public UsageWriter CreateUsageWriter()
    {
        return new UsageWriter(_connection);
    }

    public async Task ExportUsagesAsync(IdMap<Guid> apiMap, IEnumerable<(Guid Api, Guid Ancestor)> ancestors, string outputPath)
    {
        await using var transaction = _connection.BeginTransaction();

        await _connection.ExecuteAsync("""
            CREATE TABLE [ApiAncestors]
            (
                [ApiId]      INTEGER NOT NULL,
                [AncestorId] INTEGER NOT NULL
            );
            CREATE INDEX IX_ApiAncestors_ApiId ON ApiAncestors (ApiId);
            """, _connection, transaction);

        // Bulk insert ancestors
        {
            await using var command = new SqliteCommand("""
                INSERT INTO ApiAncestors
                    (ApiId, AncestorId)
                VALUES
                    (@ApiId, @AncestorId)
                """, _connection, transaction);

            var apiIdParameter = command.Parameters.Add("ApiId", SqliteType.Integer);
            var ancestorIdParameter = command.Parameters.Add("AncestorId", SqliteType.Integer);

            foreach (var (api, ancestor) in ancestors)
            {
                apiIdParameter.Value = apiMap.GetOrAdd(api);
                ancestorIdParameter.Value = apiMap.GetOrAdd(ancestor);
                await command.ExecuteNonQueryAsync();
            }
        }

        await InsertMissingApisAsync(apiMap, transaction);

        var rows = await _connection.QueryAsync<(int ApiId, float Percentage)>("""
            SELECT   AA.AncestorId as Api,
                     CAST(COUNT(distinct u.PackageID) AS real) / (SELECT COUNT(distinct PackageID) FROM Usages) AS Percentage
            FROM     Usages u
                         JOIN ApiAncestors AA ON u.ApiId = AA.ApiId
            GROUP BY AA.AncestorId          
            """);

        await using var writer = new StreamWriter(outputPath);
        foreach (var (apiId, percentage) in rows)
        {
            var guid = apiMap.GetValue(apiId);
            await writer.WriteLineAsync($"{guid:N}\t{percentage}");
        }

        await transaction.RollbackAsync();
    }
}