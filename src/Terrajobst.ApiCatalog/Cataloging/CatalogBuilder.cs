using System.Diagnostics;
using System.Security.Cryptography;
using System.Xml.Linq;

using Dapper;

using Microsoft.Data.Sqlite;

namespace Terrajobst.ApiCatalog;

public sealed class CatalogBuilder : IDisposable
{
    public void Index(string indexPath)
    {
        var files = Directory.GetFiles(indexPath, "*.xml");

        foreach (var path in files)
        {
            Console.WriteLine($"Processing {path}...");
            var doc = XDocument.Load(path);
            IndexDocument(doc);
        }
    }

    public void IndexDocument(XDocument doc)
    {
        if (doc.Root is null or { IsEmpty: true })
            return;

        if (doc.Root.Name == "package")
            IndexPackage(doc);
        else if (doc.Root.Name == "framework")
            IndexFramework(doc);
        else
            throw new Exception($"Unexpected element: {doc.Root.Name}");
    }

    private void IndexFramework(XDocument doc)
    {
        var framework = doc.Root.Attribute("name").Value;
        DefineFramework(framework);
        DefineApis(doc.Root.Elements("api"));
        DefineExtensions(doc.Root.Elements("extension"));

        foreach (var assemblyElement in doc.Root.Elements("assembly"))
        {
            var assemblyFingerprint = Guid.Parse(assemblyElement.Attribute("fingerprint").Value);
            var name = assemblyElement.Attribute("name").Value;
            var version = assemblyElement.Attribute("version").Value;
            var publicKeyToken = assemblyElement.Attribute("publicKeyToken").Value;
            var assemblyCreated = DefineAssembly(assemblyFingerprint, name, version, publicKeyToken);
            DefineFrameworkAssembly(framework, assemblyFingerprint);

            if (assemblyCreated)
            {
                DefinePlatformSupport(assemblyFingerprint, assemblyElement);
                DefineObsoletions(assemblyFingerprint, assemblyElement);
                DefinePreviewRequirements(assemblyFingerprint, assemblyElement);
                DefineExperimentals(assemblyFingerprint, assemblyElement);

                foreach (var syntaxElement in assemblyElement.Elements("syntax"))
                {
                    var apiFingerprint = Guid.Parse(syntaxElement.Attribute("id").Value);
                    var syntax = syntaxElement.Value;
                    DefineDeclaration(assemblyFingerprint, apiFingerprint, syntax);
                }
            }
        }
    }

    private void IndexPackage(XDocument doc)
    {
        var packageFingerprint = Guid.Parse(doc.Root.Attribute("fingerprint").Value);
        var packageId = doc.Root.Attribute("id").Value;
        var packageName = doc.Root.Attribute("name").Value;
        DefinePackage(packageFingerprint, packageId, packageName);
        DefineApis(doc.Root.Elements("api"));
        DefineExtensions(doc.Root.Elements("extension"));

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
                DefinePlatformSupport(assemblyFingerprint, assemblyElement);
                DefineObsoletions(assemblyFingerprint, assemblyElement);
                DefinePreviewRequirements(assemblyFingerprint, assemblyElement);
                DefineExperimentals(assemblyFingerprint, assemblyElement);

                foreach (var syntaxElement in assemblyElement.Elements("syntax"))
                {
                    var apiFingerprint = Guid.Parse(syntaxElement.Attribute("id").Value);
                    var syntax = syntaxElement.Value;
                    DefineDeclaration(assemblyFingerprint, apiFingerprint, syntax);
                }
            }
        }
    }

    private void DefineApis(IEnumerable<XElement> apiElements)
    {
        foreach (var element in apiElements)
        {
            var fingerprint = Guid.Parse(element.Attribute("fingerprint").Value);
            var kind = (ApiKind)int.Parse(element.Attribute("kind").Value);
            var parentFingerprint = element.Attribute("parent") == null
                ? Guid.Empty
                : Guid.Parse(element.Attribute("parent").Value);
            var name = element.Attribute("name").Value;

            DefineApi(fingerprint, kind, parentFingerprint, name);
        }
    }

    public void IndexUsages(string path, string name, DateOnly date)
    {
        Console.WriteLine($"Processing {path}...");
        var usages = ParseFile(path);
        IndexUsages(name, date, usages);

        static IEnumerable<(Guid ApiFingerpritn, float Percentage)> ParseFile(string path)
        {
            using var streamReader = new StreamReader(path);

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
                        yield return (apiFingerprint, percentage);
                    }
                }
            }

        }
    }

    public void IndexUsages(string name, DateOnly date, IEnumerable<(Guid ApiFingerpritn, float Percentage)> usages)
    {
        DefineUsageSource(name, date);

        foreach (var (apiFingerprint, percentage) in usages)
            DefineApiUsage(name, apiFingerprint, percentage);
    }

    public static CatalogBuilder Create(string path)
    {
        // NOTE: We disable pooling to avoid the database from being locked when we're disposed.
        //       This is critical to allow later parts of the catalog generation to read the file.
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Pooling = false,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        var connection = new SqliteConnection(connectionString);
        connection.Open();
        connection.Execute("PRAGMA JOURNAL_MODE = OFF");
        connection.Execute("PRAGMA SYNCHRONOUS = OFF");
        connection.Execute("PRAGMA FOREIGN_KEYS = OFF");
        CreateSchema(connection);

        var transaction = connection.BeginTransaction();
        return new CatalogBuilder(connection, transaction);
    }

    private readonly SqliteConnection _connection;
    private readonly SqliteTransaction _transaction;

    private CatalogBuilder(SqliteConnection connection, SqliteTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public void Dispose()
    {
        CommitExtensions();

        _transaction.Commit();
        _transaction.Dispose();
        _connection.Dispose();
    }

    private static void CreateSchema(SqliteConnection connection)
    {
        connection.Execute(@"
            CREATE TABLE Apis
            (
                ApiId       INTEGER PRIMARY KEY,
                Kind        INTEGER NOT NULL,
                ApiGuid     TEXT    NOT NULL,
                ParentApiId INTEGER REFERENCES Apis,
                Name        TEXT    NOT NULL
            );
            CREATE UNIQUE INDEX IX_Apis_ApiGuid ON Apis (ApiGuid);
            CREATE INDEX IX_Apis_ParentApiId ON Apis (ParentApiId);

            CREATE TABLE Assemblies
            (
                AssemblyId     INTEGER NOT NULL PRIMARY KEY,
                AssemblyGuid   TEXT    NOT NULL,
                Name           TEXT    NOT NULL,
                Version        TEXT    NOT NULL,
                PublicKeyToken TEXT    NOT NULL
            );
            CREATE UNIQUE INDEX IX_Assemblies_AssemblyGuid ON Assemblies (AssemblyGuid);

            CREATE TABLE Frameworks
            (
                FrameworkId  INTEGER NOT NULL PRIMARY KEY,
                FriendlyName TEXT    NOT NULL
            );

            CREATE TABLE FrameworkAssemblies
            (
                FrameworkId INTEGER NOT NULL REFERENCES Frameworks,
                AssemblyId  INTEGER NOT NULL REFERENCES Assemblies
            );
            CREATE INDEX IX_FrameworkAssemblies_AssemblyId ON FrameworkAssemblies (AssemblyId);
            CREATE INDEX IX_FrameworkAssemblies_FrameworkId ON FrameworkAssemblies (FrameworkId);

            CREATE TABLE OSPlatforms
            (
                OSPlatformId INTEGER PRIMARY KEY,
                Name         TEXT NOT NULL
            );

            CREATE TABLE OSPlatformsSupport
            (
                ApiId        INTEGER REFERENCES Apis,
                AssemblyId   INTEGER NOT NULL REFERENCES Assemblies,
                OSPlatformId INTEGER NOT NULL REFERENCES OSPlatforms,
                IsSupported  INTEGER NOT NULL
            );
            CREATE INDEX IX_OSPlatformsSupport_ApiId ON OSPlatformsSupport (ApiId);
            CREATE INDEX IX_OSPlatformsSupport_AssemblyId ON OSPlatformsSupport (AssemblyId);

            CREATE TABLE Obsoletions
            (
                ApiId        INTEGER REFERENCES Apis,
                AssemblyId   INTEGER NOT NULL REFERENCES Assemblies,
                Message      TEXT,
                IsError      INTEGER,
                DiagnosticId TEXT,
                UrlFormat    TEXT
            );
            CREATE INDEX IX_Obsoletions_ApiId ON Obsoletions (ApiId);
            CREATE INDEX IX_Obsoletions_AssemblyId ON Obsoletions (AssemblyId);

            CREATE TABLE PreviewRequirements
            (
                ApiId        INTEGER REFERENCES Apis,
                AssemblyId   INTEGER NOT NULL REFERENCES Assemblies,
                Message      TEXT,
                Url          TEXT
            );
            CREATE INDEX IX_PreviewRequirements_ApiId ON PreviewRequirements (ApiId);
            CREATE INDEX IX_PreviewRequirements_AssemblyId ON PreviewRequirements (AssemblyId);

            CREATE TABLE Experimentals
            (
                ApiId        INTEGER REFERENCES Apis,
                AssemblyId   INTEGER NOT NULL REFERENCES Assemblies,
                DiagnosticId TEXT,
                UrlFormat    TEXT
            );
            CREATE INDEX IX_Experimentals_ApiId ON Experimentals (ApiId);
            CREATE INDEX IX_Experimentals_AssemblyId ON Experimentals (AssemblyId);

            CREATE TABLE Packages
            (
                PackageId INTEGER NOT NULL PRIMARY KEY,
                Name      TEXT    NOT NULL
            );

            CREATE TABLE PackageVersions
            (
                PackageVersionId INTEGER NOT NULL PRIMARY KEY,
                PackageId        INTEGER NOT NULL REFERENCES Packages,
                Version          TEXT    NOT NULL
            );

            CREATE TABLE PackageAssemblies
            (
                PackageVersionId INTEGER NOT NULL REFERENCES PackageVersions,
                FrameworkId      INTEGER NOT NULL REFERENCES Frameworks,
                AssemblyId       INTEGER NOT NULL REFERENCES Assemblies
            );
            CREATE INDEX IX_PackageAssemblies_AssemblyId ON PackageAssemblies (AssemblyId);

            CREATE TABLE Syntaxes
            (
                SyntaxId INTEGER PRIMARY KEY,
                Syntax   TEXT NOT NULL
            );

            CREATE TABLE ExtensionMethods
            (
                ExtensionMethodGuid TEXT  NOT NULL,
                ExtendedTypeId      INTEGER NOT NULL REFERENCES Apis,
                ExtensionMethodId   INTEGER NOT NULL REFERENCES Apis
            );

            CREATE TABLE Declarations
            (
                ApiId      INTEGER NOT NULL REFERENCES Apis,
                AssemblyId INTEGER NOT NULL REFERENCES Assemblies,
                SyntaxId   INTEGER NOT NULL REFERENCES Syntaxes
            );
            CREATE INDEX IX_Declarations_ApiId ON Declarations (ApiId);

            CREATE TABLE UsageSources
            (
                UsageSourceId INTEGER NOT NULL PRIMARY KEY,
                Name          TEXT    NOT NULL,
                Date          TEXT    NOT NULL
            );

            CREATE TABLE ApiUsages
            (
                ApiId         INTEGER NOT NULL REFERENCES Apis,
                UsageSourceId INTEGER NOT NULL REFERENCES UsageSources,
                Percentage    REAL    NOT NULL
            );
            CREATE INDEX IX_ApiUsages_ApiId ON ApiUsages (ApiId);
        ");
    }

    private readonly Dictionary<string, int> _syntaxIdBySyntax = new();
    private readonly Dictionary<Guid, int> _apiIdByFingerprint = new();
    private readonly Dictionary<Guid, int> _assemblyIdByFingerprint = new();
    private readonly Dictionary<string, int> _packageIdByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, int> _packageVersionIdByFingerprint = new();
    private readonly Dictionary<string, int> _frameworkIdByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _osPlatformIdByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _usageSourceIdByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, (Guid, Guid)> _extensions = new();

    private int DefineSyntax(string syntax)
    {
        if (_syntaxIdBySyntax.TryGetValue(syntax, out var syntaxId))
            return syntaxId;

        syntaxId = _syntaxIdBySyntax.Count + 1;

        _connection.Execute(
            @"
                INSERT INTO [Syntaxes]
                    (SyntaxId, Syntax)
                VALUES
                    (@SyntaxId, @Syntax)
            ",
            new {
                SyntaxId = syntaxId,
                Syntax = syntax
            },
            _transaction
        );

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

        _connection.Execute(
            @"
                INSERT INTO [Apis]
                    (ApiId, Kind, ApiGuid, ParentApiId, Name)
                VALUES
                    (@ApiId, @Kind, @ApiGuid, @ParentApiId, @Name)
            ",
            new {
                ApiId = apiId,
                Kind = kind,
                ApiGuid = fingerprint.ToString("N"),
                ParentApiId = parentApiId,
                Name = name
            },
            _transaction
        );

        _apiIdByFingerprint.Add(fingerprint, apiId);
    }

    private bool DefineAssembly(Guid fingerprint, string name, string version, string publicKeyToken)
    {
        if (_assemblyIdByFingerprint.ContainsKey(fingerprint))
            return false;

        var assemblyId = _assemblyIdByFingerprint.Count + 1;

        _connection.Execute(
            @"
                INSERT INTO [Assemblies]
                    (AssemblyId, AssemblyGuid, Name, Version, PublicKeyToken)
                VALUES
                    (@AssemblyId, @AssemblyGuid, @Name, @Version, @PublicKeyToken)
            ",
            new {
                AssemblyId = assemblyId,
                AssemblyGuid = fingerprint.ToString("N"),
                Name = name,
                Version = version,
                PublicKeyToken = publicKeyToken,
            },
            _transaction
        );

        _assemblyIdByFingerprint.Add(fingerprint, assemblyId);
        return true;
    }

    private void DefinePlatformSupport(Guid assemblyFingerprint, XElement element)
    {
        DefinePlatformSupport(assemblyFingerprint, element, isSupported: true);
        DefinePlatformSupport(assemblyFingerprint, element, isSupported: false);
    }

    private void DefinePlatformSupport(Guid assemblyFingerprint, XElement element, bool isSupported)
    {
        var elementName = isSupported ? "supportedPlatform" : "unsupportedPlatform";

        foreach (var supportElement in element.Elements(elementName))
        {
            var id = supportElement.Attribute("id")?.Value;
            var apiFingerprint = id is null ? (Guid?)null : Guid.Parse(id);
            var platformName = supportElement.Attribute("name").Value;

            DefinePlatformSupport(apiFingerprint, assemblyFingerprint, platformName, isSupported);
        }
    }

    private void DefinePlatform(string platformName)
    {
        if (_osPlatformIdByName.ContainsKey(platformName))
            return;

        var osPlatformId = _osPlatformIdByName.Count + 1;

        _connection.Execute(
            @"
                INSERT INTO OSPlatforms
                    (OSPlatformId, Name)
                VALUES
                    (@OSPlatformId, @Name)
            ",
            new {
                OSPlatformId = osPlatformId,
                Name = platformName
            },
            _transaction
        );

        _osPlatformIdByName.Add(platformName, osPlatformId);
    }

    private void DefinePlatformSupport(Guid? apiFingerprint, Guid assemblyFingerprint, string platformName, bool isSupported)
    {
        DefinePlatform(platformName);

        var apiId = apiFingerprint is null ? (int?)null : _apiIdByFingerprint[apiFingerprint.Value];
        var assemblyId = _assemblyIdByFingerprint[assemblyFingerprint];
        var osPlatformId = _osPlatformIdByName[platformName];

        _connection.Execute(
            @"
                INSERT INTO OSPlatformsSupport
                    (ApiId, AssemblyId, OSPlatformId, IsSupported)
                VALUES
                    (@ApiId, @AssemblyId, @OSPlatformId, @IsSupported)
            ",
            new {
                ApiId = apiId,
                AssemblyId = assemblyId,
                OSPlatformId = osPlatformId,
                IsSupported = isSupported
            },
            _transaction
        );
    }

    private void DefineObsoletions(Guid assemblyFingerprint, XElement element)
    {
        foreach (var obsoleteElement in element.Elements("obsolete"))
        {
            var apiFingerprint = Guid.Parse(obsoleteElement.Attribute("id").Value);
            var isError = bool.Parse(obsoleteElement.Attribute("isError")?.Value ?? bool.FalseString);
            var message = obsoleteElement.Attribute("message")?.Value;
            var diagnosticId = obsoleteElement.Attribute("diagnosticId")?.Value;
            var urlFormat = obsoleteElement.Attribute("urlFormat")?.Value;
            DefineObsoletion(apiFingerprint, assemblyFingerprint, isError, message, diagnosticId, urlFormat);
        }
    }

    private void DefineObsoletion(Guid apiFingerprint, Guid assemblyFingerprint, bool isError, string message, string diagnosticId, string urlFormat)
    {
        var apiId = _apiIdByFingerprint[apiFingerprint];
        var assemblyId = _assemblyIdByFingerprint[assemblyFingerprint];

        _connection.Execute(
            @"
                INSERT INTO Obsoletions
                    (ApiId, AssemblyId, Message, IsError, DiagnosticId, UrlFormat)
                VALUES
                    (@ApiId, @AssemblyId, @Message, @IsError, @DiagnosticId, @UrlFormat)
            ",
            new {
                ApiId = apiId,
                AssemblyId = assemblyId,
                Message = message,
                IsError = isError,
                DiagnosticId = diagnosticId,
                UrlFormat = urlFormat
            },
            _transaction
        );
    }

    private void DefinePreviewRequirements(Guid assemblyFingerprint, XElement element)
    {
        foreach (var previewRequirementElement in element.Elements("previewRequirement"))
        {
            var id = previewRequirementElement.Attribute("id")?.Value;
            var apiFingerprint = id is null ? (Guid?)null : Guid.Parse(id);
            var message = previewRequirementElement.Attribute("message")?.Value;
            var url = previewRequirementElement.Attribute("url")?.Value;

            DefinePreviewRequirements(apiFingerprint, assemblyFingerprint, message, url);
        }
    }

    private void DefinePreviewRequirements(Guid? apiFingerprint, Guid assemblyFingerprint, string message, string url)
    {
        var apiId = apiFingerprint is null ? (int?)null : _apiIdByFingerprint[apiFingerprint.Value];
        var assemblyId = _assemblyIdByFingerprint[assemblyFingerprint];

        _connection.Execute(
            @"
                INSERT INTO PreviewRequirements
                    (ApiId, AssemblyId, Message, Url)
                VALUES
                    (@ApiId, @AssemblyId, @Message, @Url)
            ",
            new {
                ApiId = apiId,
                AssemblyId = assemblyId,
                Message = message,
                Url = url
            },
            _transaction
        );
    }

    private void DefineExperimentals(Guid assemblyFingerprint, XElement element)
    {
        foreach (var experimentalElement in element.Elements("experimental"))
        {
            var id = experimentalElement.Attribute("id")?.Value;
            var apiFingerprint = id is null ? (Guid?)null : Guid.Parse(id);
            var diagnosticId = experimentalElement.Attribute("diagnosticId")?.Value;
            var urlFormat = experimentalElement.Attribute("urlFormat")?.Value;

            DefineExperimentals(apiFingerprint, assemblyFingerprint, diagnosticId, urlFormat);
        }
    }

    private void DefineExperimentals(Guid? apiFingerprint, Guid assemblyFingerprint, string diagnosticId, string urlFormat)
    {
        var apiId = apiFingerprint is null ? (int?)null : _apiIdByFingerprint[apiFingerprint.Value];
        var assemblyId = _assemblyIdByFingerprint[assemblyFingerprint];

        _connection.Execute(
            @"
                INSERT INTO Experimentals
                    (ApiId, AssemblyId, DiagnosticId, UrlFormat)
                VALUES
                    (@ApiId, @AssemblyId, @DiagnosticId, @UrlFormat)
            ",
            new {
                ApiId = apiId,
                AssemblyId = assemblyId,
                DiagnosticId = diagnosticId,
                UrlFormat = urlFormat
            },
            _transaction
        );
    }

    private void DefineExtensions(IEnumerable<XElement> extensionElements)
    {
        foreach (var experimentalElement in extensionElements)
        {
            var guid = Guid.Parse(experimentalElement.Attribute("fingerprint").Value);
            var typeGuid = Guid.Parse(experimentalElement.Attribute("type").Value);
            var methodGuid = Guid.Parse(experimentalElement.Attribute("method").Value);
            DefineExtension(guid, typeGuid, methodGuid);
        }
    }

    private void DefineExtension(Guid extensionGuid, Guid typeGuid, Guid methodGuid)
    {
        if (extensionGuid == Guid.Empty || typeGuid == Guid.Empty || methodGuid == Guid.Empty)
            return;

        _extensions.TryAdd(extensionGuid, (typeGuid, methodGuid));
    }

    private void CommitExtensions()
    {
        foreach (var (extensionMethodGuid, (typeGuid, methodGuid)) in _extensions)
        {
            if (!_apiIdByFingerprint.TryGetValue(typeGuid, out var typeId))
            {
                Console.WriteLine($"error: can't resolve extension type {typeGuid}");
                continue;
            }

            if (!_apiIdByFingerprint.TryGetValue(methodGuid, out var methodId))
            {
                Console.WriteLine($"error: can't resolve extension method {methodId}");
                continue;
            }

            _connection.Execute(
                @"
                    INSERT INTO ExtensionMethods
                        (ExtensionMethodGuid, ExtendedTypeId, ExtensionMethodId)
                    VALUES
                        (@ExtensionMethodGuid, @TypeId, @MethodId)
                ",
                new {
                    ExtensionMethodGuid = extensionMethodGuid.ToString("N"),
                    TypeId = typeId,
                    MethodId = methodId
                },
                _transaction
            );
        }
    }

    private void DefineDeclaration(Guid assemblyFingerprint, Guid apiFingerprint, string syntax)
    {
        var assemblyId = _assemblyIdByFingerprint[assemblyFingerprint];
        var apiId = _apiIdByFingerprint[apiFingerprint];
        var syntaxId = DefineSyntax(syntax);

        _connection.Execute(
            @"
                INSERT INTO [Declarations]
                    (AssemblyId, ApiId, SyntaxId)
                VALUES
                    (@AssemblyId, @ApiId, @SyntaxId)
            ",
            new {
                AssemblyId = assemblyId,
                ApiId = apiId,
                SyntaxId = syntaxId
            },
            _transaction
        );
    }

    private void DefineFramework(string frameworkName)
    {
        if (_frameworkIdByName.ContainsKey(frameworkName))
            return;

        var frameworkId = _frameworkIdByName.Count + 1;

        _connection.Execute(
            @"
                INSERT INTO [Frameworks]
                    (FrameworkId, FriendlyName)
                VALUES
                    (@FrameworkId, @FriendlyName)
            ",
            new {
                FrameworkId = frameworkId,
                FriendlyName = frameworkName
            },
            _transaction
        );

        _frameworkIdByName.Add(frameworkName, frameworkId);
    }

    private void DefineFrameworkAssembly(string framework, Guid assemblyFingerprint)
    {
        var frameworkId = _frameworkIdByName[framework];
        var assemblyId = _assemblyIdByFingerprint[assemblyFingerprint];

        _connection.Execute(
            @"
                INSERT INTO [FrameworkAssemblies]
                    (FrameworkId, AssemblyId)
                VALUES
                    (@FrameworkId, @AssemblyId)
            ",
            new {
                FrameworkId = frameworkId,
                AssemblyId = assemblyId
            },
            _transaction
        );
    }

    private void DefinePackage(Guid fingerprint, string id, string version)
    {
        if (_packageVersionIdByFingerprint.ContainsKey(fingerprint))
            return;

        var packageVersionId = _packageVersionIdByFingerprint.Count + 1;
        var packageId = DefinePackageName(id);

        _connection.Execute(
            @"
                INSERT INTO [PackageVersions]
                    (PackageVersionId, PackageId, Version)
                VALUES
                    (@PackageVersionId, @PackageId, @Version)
            ",
            new {
                PackageVersionId = packageVersionId,
                PackageId = packageId,
                Version = version
            },
            _transaction
        );

        _packageVersionIdByFingerprint.Add(fingerprint, packageVersionId);
    }

    private int DefinePackageName(string name)
    {
        if (!_packageIdByName.TryGetValue(name, out var packageId))
        {
            packageId = _packageIdByName.Count + 1;

            _connection.Execute(
                @"
                    INSERT INTO [Packages]
                        (PackageId, Name)
                    VALUES
                        (@PackageId, @Name)
                ",
                new {
                    PackageId = packageId,
                    Name = name,
                },
                _transaction
            );

            _packageIdByName.Add(name, packageId);
        }

        return packageId;
    }

    private void DefinePackageAssembly(Guid packageFingerprint, string frameworkName, Guid assemblyFingerprint)
    {
        var packageVersionId = _packageVersionIdByFingerprint[packageFingerprint];
        var frameworkId = _frameworkIdByName[frameworkName];
        var assemblyId = _assemblyIdByFingerprint[assemblyFingerprint];

        _connection.Execute(
            @"
                INSERT INTO [PackageAssemblies]
                    (PackageVersionId, FrameworkId, AssemblyId)
                VALUES
                    (@PackageVersionId, @FrameworkId, @AssemblyId)
            ",
            new {
                PackageVersionId = packageVersionId,
                FrameworkId = frameworkId,
                AssemblyId = assemblyId
            },
            _transaction
        );
    }

    private void DefineUsageSource(string name, DateOnly date)
    {
        if (_usageSourceIdByName.ContainsKey(name))
            return;

        var usageSourceId = _usageSourceIdByName.Count + 1;

        _connection.Execute(
            @"
                INSERT INTO [UsageSources]
                    (UsageSourceId, Name, Date)
                VALUES
                    (@UsageSourceId, @Name, @Date)
            ",
            new {
                UsageSourceId = usageSourceId,
                Name = name,
                Date = new DateTime(date.Year, date.Month, date.Day)
            },
            _transaction
        );

        _usageSourceIdByName.Add(name, usageSourceId);
    }

    private void DefineApiUsage(string usageSourceName, Guid apiFingerprint, float percentage)
    {
        if (!_usageSourceIdByName.TryGetValue(usageSourceName, out var usageSourceId))
            return;

        if (!_apiIdByFingerprint.TryGetValue(apiFingerprint, out var apiId))
            return;

        _connection.Execute(
            @"
                INSERT INTO [ApiUsages]
                    (ApiId, UsageSourceId, Percentage)
                VALUES
                    (@ApiId, @UsageSourceId, @Percentage)
            ",
            new {
                ApiId = apiId,
                UsageSourceId = usageSourceId,
                Percentage = percentage
            },
            _transaction
        );
    }
}