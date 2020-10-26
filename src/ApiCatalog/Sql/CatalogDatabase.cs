using System;
using System.Threading.Tasks;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ApiCatalog
{
    internal class CatalogDatabase : IDisposable
    {
        private readonly SqlConnection _connection;

        private CatalogDatabase(SqlConnection connection)
        {
            _connection = connection;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        public static async Task<CatalogDatabase> OpenAsync(string connectionString)
        {
            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            return new CatalogDatabase(connection);
        }

        public async Task<int> InsertPackageAsync(string name)
        {
            var packageId = await _connection.ExecuteScalarAsync<int>(@"
                MERGE Packages AS target
                USING (SELECT @Name) AS source (Name)
                ON    (target.Name = source.Name)
                WHEN NOT MATCHED THEN
                    INSERT (Name)
                    VALUES (@Name);
                SELECT  PackageId
                FROM    Packages
                WHERE   Name = @Name;
            ", new
            {
                Name = name
            });

            return packageId;
        }

        public async Task<(int Id, bool Inserted)> InsertPackageVersionAsync(int packageId, string version)
        {
            using var reader = await _connection.ExecuteReaderAsync(@"
                MERGE PackageVersions AS target
                USING (SELECT @PackageId, @Version) AS source (PackageId, Version)
                ON    (target.PackageId = source.PackageId AND
                       target.Version = source.Version)
                WHEN NOT MATCHED THEN
                    INSERT (PackageId, Version)
                    VALUES (@PackageId, @Version)
                OUTPUT
                    inserted.PackageVersionId;
                SELECT  PackageVersionId
                FROM    PackageVersions
                WHERE   PackageId = @PackageId
                AND     Version = @Version
            ", new
            {
                PackageId = packageId,
                Version = version
            });

            var isInserted = await reader.ReadAsync();
            await reader.NextResultAsync();
            await reader.ReadAsync();

            var id = Convert.ToInt32(reader[0]);
            return (id, isInserted);
        }

        public async Task<(int Id, bool Inserted)> InsertAssemblyAsync(Guid assemblyGuid,
                                                                       string assemblyName,
                                                                       string assemblyVersion,
                                                                       string assemblyPublicKeyToken)
        {
            using var reader = await _connection.ExecuteReaderAsync(@"
                MERGE Assemblies AS target
                USING (SELECT @AssemblyGuid) AS source (AssemblyGuid)
                ON    (target.AssemblyGuid = source.AssemblyGuid)
                WHEN NOT MATCHED THEN
                    INSERT  (AssemblyGuid, Name, Version, PublicKeyToken)
                    VALUES  (@AssemblyGuid, @Name, @Version, @PublicKeyToken)
                OUTPUT
                    inserted.AssemblyId;
                SELECT  AssemblyId
                FROM    Assemblies
                WHERE   AssemblyGuid = @AssemblyGuid;
            ", new
            {
                AssemblyGuid = assemblyGuid,
                Name = assemblyName,
                Version = assemblyVersion,
                PublicKeyToken = assemblyPublicKeyToken
            });

            var isInserted = await reader.ReadAsync();
            await reader.NextResultAsync();
            await reader.ReadAsync();

            var id = Convert.ToInt32(reader[0]);
            return (id, isInserted);
        }

        public async Task InsertPackageAssemblyAsync(int packageVersionId, int frameworkId, int assemblyId)
        {
            await _connection.ExecuteScalarAsync(@"
                INSERT INTO PackageAssemblies
                    (PackageVersionId, FrameworkId, AssemblyId)
                VALUES
                    (@PackageVersionId, @FrameworkId, @AssemblyId)
            ", new
            {
                PackageVersionId = packageVersionId,
                FrameworkId = frameworkId,
                AssemblyId = assemblyId
            });
        }

        public async Task<int> InsertApi(Guid apiGuid, ApiKind kind, int? parentApiId, string name)
        {
            var apiId = await _connection.ExecuteScalarAsync<int>(@"
                MERGE Apis AS target
                USING (SELECT @ApiGuid) AS source (ApiGuid)
                ON    (target.ApiGuid = source.ApiGuid)
                WHEN NOT MATCHED THEN
                    INSERT  (ApiGuid, Kind, ParentApiId, Name)
                    VALUES  (@ApiGuid, @Kind, @ParentApiId, @Name);
                SELECT  ApiId
                FROM    Apis
                WHERE   ApiGuid = @ApiGuid;
            ", new
            {
                ApiGuid = apiGuid,
                Kind = kind,
                ParentApiId = parentApiId,
                Name = name
            });

            return apiId;
        }

        public async Task<int> InsertDeclaration(int apiId, int assemblyId, string syntax)
        {
            var declartionId = await _connection.ExecuteScalarAsync<int>(@"
                INSERT INTO Declarations
                    (ApiId, AssemblyId, Syntax)
                VALUES
                    (@ApiId, @AssemblyId, @Syntax);
                SELECT CAST(SCOPE_IDENTITY() AS INT)
            ", new
            {
                ApiId = apiId,
                AssemblyId = assemblyId,
                Syntax = syntax
            });

            return declartionId;
        }
    }
}
