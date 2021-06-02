using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Dapper;

using Microsoft.Data.Sqlite;

namespace ApiCatalog
{
    public sealed class CatalogBuilderSQLite : CatalogBuilder, IDisposable
    {
        public static async Task<CatalogBuilderSQLite> CreateAsync(string path)
        {
            var exists = File.Exists(path);
            var cb = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadWriteCreate
            };

            var connection = new SqliteConnection(cb.ToString());
            var result = new CatalogBuilderSQLite(connection);
            try
            {
                await connection.OpenAsync();
                connection.Execute("PRAGMA JOURNAL_MODE = OFF");
                connection.Execute("PRAGMA SYNCHRONOUS = OFF");
                if (!exists)
                    await result.CreateSchemaAsync();
            }
            catch
            {
                result.Dispose();
                throw;
            }

            return result;
        }

        private readonly SqliteConnection _connection;

        public CatalogBuilderSQLite(SqliteConnection connection)
        {
            _connection = connection;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        private async Task CreateSchemaAsync()
        {
            var commands = new[]
            {
                @"
                    CREATE TABLE [Syntaxes]
                    (
                        [SyntaxId] INTEGER PRIMARY KEY,
                        [Syntax] TEXT NOT NULL
                    )
                ",
                @"
                    CREATE TABLE [Apis]
                    (
                        [ApiId] INTEGER PRIMARY KEY,
                        [Kind] INTEGER NOT NULL,
                        [ApiGuid] TEXT NOT NULL,
                        [ParentApiId] INTEGER,
                        [Name] TEXT NOT NULL
                    )
                ",
                @"
                    CREATE UNIQUE INDEX [IX_Apis_ApiGuid] ON [Apis] ([ApiGuid])
                ",
                @"
                    CREATE INDEX [IX_Apis_ParentApiId] ON [Apis] ([ParentApiId])
                ",
                @"
                    CREATE TABLE [Assemblies]
                    (
                        [AssemblyId] INTEGER NOT NULL PRIMARY KEY,
                        [AssemblyGuid] TEXT NOT NULL,
                        [Name] TEXT NOT NULL,
                        [Version] TEXT NOT NULL,
                        [PublicKeyToken] TEXT NOT NULL
                    )
                ",
                @"
                    CREATE UNIQUE INDEX [IX_Assemblies_AssemblyGuid] ON [Assemblies] ([AssemblyGuid])
                ",
                @"
                    CREATE TABLE [Declarations]
                    (
                        [ApiId] INTEGER NOT NULL,
                        [AssemblyId] INTEGER NOT NULL,
                        [SyntaxId] INTEGER NOT NULL
                    )
                ",
                @"
                    CREATE INDEX [IX_Declarations_ApiId] ON [Declarations] ([ApiId])
                ",
                @"
                    CREATE TABLE [Frameworks]
                    (
                        [FrameworkId] INTEGER NOT NULL PRIMARY KEY,
                        [FriendlyName] TEXT NOT NULL
                    )
                ",
                @"
                    CREATE TABLE [PackageAssemblies]
                    (
                        [PackageVersionId] INTEGER NOT NULL,
                        [FrameworkId] INTEGER NOT NULL,
                        [AssemblyId] INTEGER NOT NULL
                    )
                ",
                @"
                    CREATE INDEX [IX_PackageAssemblies_AssemblyId] ON [PackageAssemblies] ([AssemblyId])
                ",
                @"
                    CREATE TABLE [Packages]
                    (
                        [PackageId] INTEGER NOT NULL PRIMARY KEY,
                        [Name] TEXT NOT NULL
                    )
                ",
                @"
                    CREATE TABLE [PackageVersions]
                    (
                        [PackageVersionId] INTEGER NOT NULL PRIMARY KEY,
                        [PackageId] INTEGER NOT NULL,
                        [Version] TEXT NOT NULL
                    )
                ",
                @"
                    CREATE TABLE [FrameworkAssemblies]
                    (
                        [FrameworkId] INTEGER NOT NULL,
                        [AssemblyId] INTEGER NOT NULL
                    )
                ",
                @"
                    CREATE INDEX [IX_FrameworkAssemblies_FrameworkId] ON [FrameworkAssemblies] ([FrameworkId])
                ",
                @"
                    CREATE INDEX [IX_FrameworkAssemblies_AssemblyId] ON [FrameworkAssemblies] ([AssemblyId])
                "
            };

            using var cmd = new SqliteCommand();
            cmd.Connection = _connection;

            foreach (var sql in commands)
            {
                cmd.CommandText = sql;
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private readonly Dictionary<string, int> _syntaxIdBySyntax = new Dictionary<string, int>();
        private readonly Dictionary<Guid, int> _apiIdByFingerprint = new Dictionary<Guid, int>();
        private readonly Dictionary<Guid, int> _assemblydIdByFingerprint = new Dictionary<Guid, int>();
        private readonly Dictionary<string, int> _packageIdByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Guid, int> _packageVersionIdByFingerprint = new Dictionary<Guid, int>();
        private readonly Dictionary<string, int> _frameworkIdByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private int DefineSyntax(string syntax)
        {
            if (_syntaxIdBySyntax.TryGetValue(syntax, out var syntaxId))
                return syntaxId;

            syntaxId = _syntaxIdBySyntax.Count + 1;

            _connection.Execute(@"
                    INSERT INTO [Syntaxes]
                        (SyntaxId, Syntax)
                    VALUES
                        (@SyntaxId, @Syntax)
                ", new
            {
                SyntaxId = syntaxId,
                Syntax = syntax
            });

            _syntaxIdBySyntax.Add(syntax, syntaxId);

            return syntaxId;
        }

        protected override void DefineApi(Guid fingerprint, ApiKind kind, Guid parentFingerprint, string name)
        {
            if (_apiIdByFingerprint.ContainsKey(fingerprint))
                return;

            var apiId = _apiIdByFingerprint.Count + 1;
            var parentApiId = parentFingerprint == Guid.Empty
                                ? (int?)null
                                : _apiIdByFingerprint[parentFingerprint];

            _connection.Execute(@"
                INSERT INTO [Apis]
                    (ApiId, Kind, ApiGuid, ParentApiId, Name)
                VALUES
                    (@ApiId, @Kind, @ApiGuid, @ParentApiId, @Name)
            ", new
            {
                ApiId = apiId,
                Kind = kind,
                ApiGuid = fingerprint.ToString("N"),
                ParentApiId = parentApiId,
                Name = name
            });

            _apiIdByFingerprint.Add(fingerprint, apiId);
        }

        protected override bool DefineAssembly(Guid fingerprint, string name, string version, string publicKeyToken)
        {
            if (_assemblydIdByFingerprint.ContainsKey(fingerprint))
                return false;

            var assemblyId = _assemblydIdByFingerprint.Count + 1;

            _connection.Execute(@"
                INSERT INTO [Assemblies]
                    (AssemblyId, AssemblyGuid, Name, Version, PublicKeyToken)
                VALUES
                    (@AssemblyId, @AssemblyGuid, @Name, @Version, @PublicKeyToken)
            ", new
            {
                AssemblyId = assemblyId,
                AssemblyGuid = fingerprint.ToString("N"),
                Name = name,
                Version = version,
                PublicKeyToken = publicKeyToken,
            });

            _assemblydIdByFingerprint.Add(fingerprint, assemblyId);
            return true;
        }

        protected override void DefineDeclaration(Guid assemblyFingerprint, Guid apiFingerprint, string syntax)
        {
            var assemblyId = _assemblydIdByFingerprint[assemblyFingerprint];
            var apiId = _apiIdByFingerprint[apiFingerprint];
            var syntaxId = DefineSyntax(syntax);

            _connection.Execute(@"
                INSERT INTO [Declarations]
                    (AssemblyId, ApiId, SyntaxId)
                VALUES
                    (@AssemblyId, @ApiId, @SyntaxId)
            ", new
            {
                AssemblyId = assemblyId,
                ApiId = apiId,
                SyntaxId = syntaxId
            });
        }

        protected override void DefineFramework(string frameworkName)
        {
            if (_frameworkIdByName.ContainsKey(frameworkName))
                return;

            var frameworkId = _frameworkIdByName.Count + 1;

            _connection.Execute(@"
                INSERT INTO [Frameworks]
                    (FrameworkId, FriendlyName)
                VALUES
                    (@FrameworkId, @FriendlyName)
            ", new
            {
                FrameworkId = frameworkId,
                FriendlyName = frameworkName
            });

            _frameworkIdByName.Add(frameworkName, frameworkId);
        }

        protected override void DefineFrameworkAssembly(string framework, Guid assemblyFingerprint)
        {
            var frameworkId = _frameworkIdByName[framework];
            var assemblyId = _assemblydIdByFingerprint[assemblyFingerprint];

            _connection.Execute(@"
                INSERT INTO [FrameworkAssemblies]
                    (FrameworkId, AssemblyId)
                VALUES
                    (@FrameworkId, @AssemblyId)
            ", new
            {
                FrameworkId = frameworkId,
                AssemblyId = assemblyId
            });
        }

        protected override void DefinePackage(Guid fingerprint, string id, string version)
        {
            if (_packageVersionIdByFingerprint.ContainsKey(fingerprint))
                return;

            var packageVersionId = _packageVersionIdByFingerprint.Count + 1;
            var packageId = DefinePackageName(id);

            _connection.Execute(@"
                INSERT INTO [PackageVersions]
                    (PackageVersionId, PackageId, Version)
                VALUES
                    (@PackageVersionId, @PackageId, @Version)
            ", new
            {
                PackageVersionId = packageVersionId,
                PackageId = packageId,
                Version = version
            });

            _packageVersionIdByFingerprint.Add(fingerprint, packageVersionId);
        }

        private int DefinePackageName(string name)
        {
            if (!_packageIdByName.TryGetValue(name, out var packageId))
            {
                packageId = _packageIdByName.Count + 1;

                _connection.Execute(@"
                    INSERT INTO [Packages]
                        (PackageId, Name)
                    VALUES
                        (@PackageId, @Name)
                ", new
                {
                    PackageId = packageId,
                    Name = name,
                });

                _packageIdByName.Add(name, packageId);
            }

            return packageId;
        }

        protected override void DefinePackageAssembly(Guid packageFingerprint, string frameworkName, Guid assemblyFingerprint)
        {
            var packageVersionId = _packageVersionIdByFingerprint[packageFingerprint];
            var frameworkId = _frameworkIdByName[frameworkName];
            var assemblyId = _assemblydIdByFingerprint[assemblyFingerprint];

            _connection.Execute(@"
                INSERT INTO [PackageAssemblies]
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
    }
}
