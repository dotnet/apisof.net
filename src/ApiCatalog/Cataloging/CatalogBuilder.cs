using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Dapper;

using Microsoft.Data.Sqlite;

namespace ApiCatalog
{
    public sealed class CatalogBuilder : IDisposable
    {
        public void Index(string indexPath)
        {
            var files = Directory.GetFiles(indexPath, "*.xml");

            static void DefineApis(CatalogBuilder builder, IEnumerable<XElement> apiElements)
            {
                foreach (var element in apiElements)
                {
                    var fingerprint = Guid.Parse(element.Attribute("fingerprint").Value);
                    var kind = (ApiKind)int.Parse(element.Attribute("kind").Value);
                    var parentFingerprint = element.Attribute("parent") == null
                        ? Guid.Empty
                        : Guid.Parse(element.Attribute("parent").Value);
                    var name = element.Attribute("name").Value;

                    builder.DefineApi(fingerprint, kind, parentFingerprint, name);
                }
            }

            foreach (var path in files)
            {
                Console.WriteLine($"Processing {path}...");
                var doc = XDocument.Load(path);
                if (doc.Root.IsEmpty)
                    continue;

                if (doc.Root.Name == "package")
                {
                    var packageFingerprint = Guid.Parse(doc.Root.Attribute("fingerprint").Value);
                    var packageId = doc.Root.Attribute("id").Value;
                    var packageName = doc.Root.Attribute("name").Value;
                    DefinePackage(packageFingerprint, packageId, packageName);
                    DefineApis(this, doc.Root.Elements("api"));

                    foreach (var assemblyElement in doc.Root.Elements("assembly"))
                    {
                        var framework = assemblyElement.Attribute("fx").Value;
                        var assemblyFingerprint = Guid.Parse(assemblyElement.Attribute("fingerprint").Value);
                        var name = assemblyElement.Attribute("name").Value;
                        var version = assemblyElement.Attribute("version").Value;
                        var publicKeyToken = assemblyElement.Attribute("publicKeyToken").Value;
                        DefineFramework(framework);
                        var assemblyCreated = DefineAssembly(assemblyFingerprint, name, version, publicKeyToken);
                        DefinePackageAssembly(packageFingerprint, framework, assemblyFingerprint);

                        if (assemblyCreated)
                        {
                            foreach (var syntaxElement in assemblyElement.Elements("syntax"))
                            {
                                var apiFingerprint = Guid.Parse(syntaxElement.Attribute("id").Value);
                                var syntax = syntaxElement.Value;
                                DefineDeclaration(assemblyFingerprint, apiFingerprint, syntax);
                            }
                        }
                    }
                }
                else if (doc.Root.Name == "framework")
                {
                    var framework = doc.Root.Attribute("name").Value;
                    DefineFramework(framework);
                    DefineApis(this, doc.Root.Elements("api"));

                    foreach (var assemblyElement in doc.Root.Elements("assembly"))
                    {
                        var assemblyFingerprint = Guid.Parse(assemblyElement.Attribute("fingerprint").Value);
                        var name = assemblyElement.Attribute("name").Value;
                        var version = assemblyElement.Attribute("version").Value;
                        var publicKeyToken = assemblyElement.Attribute("publicKeyToken").Value;
                        var assemblyExists = DefineAssembly(assemblyFingerprint, name, version, publicKeyToken);
                        DefineFrameworkAssembly(framework, assemblyFingerprint);

                        if (assemblyExists)
                        {
                            foreach (var syntaxElement in assemblyElement.Elements("syntax"))
                            {
                                var apiFingerprint = Guid.Parse(syntaxElement.Attribute("id").Value);
                                var syntax = syntaxElement.Value;
                                DefineDeclaration(assemblyFingerprint, apiFingerprint, syntax);
                            }
                        }
                    }
                }
            }
        }

        public void IndexUsages(string path, string name, DateOnly date)
        {
            using var streamReader = new StreamReader(path);
            Console.WriteLine($"Processing {path}...");

            DefineUsageSource(name, date);

            while (streamReader.ReadLine() is { } line)
            {
                var tabIndex = line.IndexOf('\t');
                var lastTabIndex = line.LastIndexOf('\t');
                if (tabIndex > 0 && tabIndex == lastTabIndex)
                {
                    var guidTextSpan = line.AsSpan(0, tabIndex);
                    var percentageSpan = line.AsSpan(tabIndex + 1);

                    if (Guid.TryParse(guidTextSpan, out var apiFingerprint) &&
                        float.TryParse(percentageSpan, out var percentage))
                    {
                        DefineApiUsage(name, apiFingerprint, percentage);
                    }
                }
            }
        }
        public static async Task<CatalogBuilder> CreateAsync(string path)
        {
            var exists = File.Exists(path);
            var cb = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadWriteCreate
            };

            var connection = new SqliteConnection(cb.ToString());
            var result = new CatalogBuilder(connection);
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

        public CatalogBuilder(SqliteConnection connection)
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
                ",
                @"
                    CREATE TABLE [UsageSources]
                    (
                        [UsageSourceId] INTEGER NOT NULL PRIMARY KEY,
                        [Name] TEXT NOT NULL,
                        [Date] TEXT NOT NULL
                    )
                ",
                @"
                    CREATE TABLE [ApiUsages]
                    (
                        [ApiId] INTEGER NOT NULL,
                        [UsageSourceId] INTEGER NOT NULL,
                        [Percentage] REAL NOT NULL
                    )
                ",
                @"
                    CREATE INDEX [IX_ApiUsages_ApiId] ON [ApiUsages] ([ApiId])
                ",
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
        private readonly Dictionary<string, int> _usageSourceIdByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

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

        private void DefineApi(Guid fingerprint, ApiKind kind, Guid parentFingerprint, string name)
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

        private bool DefineAssembly(Guid fingerprint, string name, string version, string publicKeyToken)
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

        private void DefineDeclaration(Guid assemblyFingerprint, Guid apiFingerprint, string syntax)
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

        private void DefineFramework(string frameworkName)
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

        private void DefineFrameworkAssembly(string framework, Guid assemblyFingerprint)
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

        private void DefinePackage(Guid fingerprint, string id, string version)
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

        private void DefinePackageAssembly(Guid packageFingerprint, string frameworkName, Guid assemblyFingerprint)
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

        private void DefineUsageSource(string name, DateOnly date)
        {
            if (_usageSourceIdByName.ContainsKey(name))
                return;

            var usageSourceId = _usageSourceIdByName.Count + 1;

            _connection.Execute(@"
                INSERT INTO [UsageSources]
                    (UsageSourceId, Name, Date)
                VALUES
                    (@UsageSourceId, @Name, @Date)
            ", new
            {
                UsageSourceId = usageSourceId,
                Name = name,
                Date = new DateTime(date.Year, date.Month, date.Day)
            });

            _usageSourceIdByName.Add(name, usageSourceId);
        }

        private void DefineApiUsage(string usageSourceName, Guid apiFingerprint, float percentage)
        {
            if (!_usageSourceIdByName.TryGetValue(usageSourceName, out var usageSourceId))
                return;

            if (!_apiIdByFingerprint.TryGetValue(apiFingerprint, out var apiId))
                return;

            _connection.Execute(@"
                INSERT INTO [ApiUsages]
                    (ApiId, UsageSourceId, Percentage)
                VALUES
                    (@ApiId, @UsageSourceId, @Percentage)
            ", new
            {
                ApiId = apiId,
                UsageSourceId = usageSourceId,
                Percentage = percentage
            });
        }
    }
}
