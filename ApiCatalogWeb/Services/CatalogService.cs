﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dapper;

using Microsoft.Data.Sqlite;

using NuGet.Frameworks;

using PackageIndexing;

namespace ApiCatalogWeb.Services
{
    public class CatalogStats
    {
        public int NumberOfApis { get; set; }
        public int NumberOfDeclarations { get; set; }
        public int NumberOfAssemblies { get; set; }
        public int NumberOfFrameworks { get; set; }
        public int NumberOfFrameworkAssemblies { get; set; }
        public int NumberOfPackages { get; set; }
        public int NumberOfPackageVersions { get; set; }
        public int NumberOfPackageAssemblies { get; set; }
    }

    public class CatalogApi : IComparable<CatalogApi>
    {
        public int ApiId { get; set; }
        public string ApiGuid { get; set; }
        public int ParentApiId { get; set; }
        public ApiKind Kind { get; set; }
        public string Name { get; set; }
        public bool IsUnsupported { get; set; }

        public int CompareTo([AllowNull] CatalogApi other)
        {
            if (other == null)
                return 1;

            if (Kind.IsType() && other.Kind.IsMember())
                return -1;

            if (Kind.IsMember() && other.Kind.IsType())
                return 1;

            if (Kind.IsMember() && other.Kind.IsMember())
            {
                var result = Kind.CompareTo(other.Kind);
                if (result != 0)
                    return result;
            }

            if (Kind == ApiKind.Namespace && other.Kind == ApiKind.Namespace)
            {
                var orderReversed = new[]
                {
                    "Windows",
                    "Microsoft",
                    "System",
                };

                var topLevel = GetTopLevelNamespace(Name);
                var otherTopLevel = GetTopLevelNamespace(other.Name);

                var topLevelIndex = Array.IndexOf(orderReversed, topLevel);
                var otherTopLevelIndex = Array.IndexOf(orderReversed, otherTopLevel);

                var result = -topLevelIndex.CompareTo(otherTopLevelIndex);
                if (result != 0)
                    return result;
            }

            if (GetMemberName(Name) == GetMemberName(other.Name))
            {
                var typeParameterCount = GetTypeParameterCount(Name);
                var otherTypeParameterCount = GetTypeParameterCount(other.Name);

                var result = typeParameterCount.CompareTo(otherTypeParameterCount);
                if (result != 0)
                    return result;

                var parameterCount = GetParameterCount(Name);
                var otherParameterCount = GetParameterCount(other.Name);

                result = parameterCount.CompareTo(otherParameterCount);
                if (result != 0)
                    return result;
            }

            return Name.CompareTo(other.Name);
        }

        private int GetTypeParameterCount(string name)
        {
            return GetArity(name, '<', '>');
        }

        private int GetParameterCount(string name)
        {
            return GetArity(name, '(', ')');
        }

        private string GetMemberName(string name)
        {
            var angleIndex = name.IndexOf('<');
            var parenthesisIndex = name.IndexOf('(');
            if (angleIndex < 0 && parenthesisIndex < 0)
                return name;

            if (angleIndex >= 0 && parenthesisIndex >= 0)
                return name.Substring(0, Math.Min(angleIndex, parenthesisIndex));

            if (angleIndex >= 0)
                return name.Substring(0, angleIndex);

            return name.Substring(0, parenthesisIndex);
        }

        private int GetArity(string name, char openParenthesis, char closeParenthesis)
        {
            var openIndex = name.IndexOf(openParenthesis);
            if (openIndex < 0)
                return 0;

            var closeIndex = name.IndexOf(closeParenthesis);
            if (closeIndex < 0)
                return 0;

            var result = 1;

            for (var i = openIndex + 1; i < closeIndex; i++)
                if (name[i] == ',')
                    result++;

            return result;
        }

        private static string GetTopLevelNamespace(string name)
        {
            var dotIndex = name.IndexOf('.');
            if (dotIndex < 0)
                return name;

            return name.Substring(0, dotIndex);
        }
    }

    public class CatalogApiSpine
    {
        public CatalogApi Selected { get; set; }
        public CatalogApi Root => Parents.First();
        public List<CatalogApi> Parents { get; } = new List<CatalogApi>();
        public List<CatalogApi> Children { get; } = new List<CatalogApi>();
    }

    public class CatalogFrameworkAvailability : IFrameworkSpecific
    {
        public string FrameworkName { get; set; }
        public string AssemblyFingerprint { get; set; }
        public string AssemblyName { get; set; }
        public string AssemblyPublicKeyToken { get; set; }
        public string AssemblyVersion { get; set; }

        public NuGetFramework TargetFramework => NuGetFramework.ParseFolder(FrameworkName);
    }

    public class CatalogPackageAvailability : CatalogFrameworkAvailability
    {
        public string PackageName { get; set; }
        public string PackageVersion { get; set; }
    }

    public class CatalogAvailability
    {
        public CatalogAvailability(IEnumerable<string> frameworks,
                                   string selectedFramework,
                                   IEnumerable<CatalogFrameworkAvailability> frameworkAvailabilities,
                                   IEnumerable<CatalogPackageAvailability> packageAvailabilities)
        {
            var parsedFrameworks = frameworks.Select(NuGetFramework.ParseFolder).Where(fx => !fx.IsPCL);
            var availability = frameworkAvailabilities.Concat(packageAvailabilities).ToArray();

            foreach (var fx in parsedFrameworks)
            {
                var match = availability.GetNearest(fx);
                if (match != null)
                    Frameworks.Add((fx, match));
            }

            Current = Frameworks.FirstOrDefault(e => e.framework.GetShortFolderName() == selectedFramework).Item2
                ?? Frameworks.OrderBy(fx => fx.framework.Framework)
                             .ThenByDescending(fx => fx.framework.Version)
                             .FirstOrDefault().Item2;
        }

        public CatalogFrameworkAvailability Current { get; }
        public List<(NuGetFramework framework, CatalogFrameworkAvailability)> Frameworks { get; } = new List<(NuGetFramework framework, CatalogFrameworkAvailability)>();
    }

    public class CatalogService : IDisposable
    {
        private readonly SqliteConnection _sqliteConnection;

        public CatalogService()
        {
            var connectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = @"C:\Users\immo\Downloads\Indexing\apicatalog.db"
            }.ToString();

            _sqliteConnection = new SqliteConnection(connectionString);
            _sqliteConnection.Open();
        }

        public async Task<CatalogStats> GetCatalogStatsAsync()
        {
            var result = await _sqliteConnection.QuerySingleAsync<CatalogStats>(@"
                SELECT
                    (SELECT COUNT(*) FROM [Apis])                   AS [NumberOfApis],
                    (SELECT COUNT(*) FROM [Declarations])           AS [NumberOfDeclarations],
                    (SELECT COUNT(*) FROM [Assemblies])             AS [NumberOfAssemblies],
                    (SELECT COUNT(*) FROM [Packages])               AS [NumberOfPackages],
                    (SELECT COUNT(*) FROM [PackageVersions])        AS [NumberOfPackageVersions],
                    (SELECT COUNT(*) FROM [PackageAssemblies])      AS [NumberOfPackageAssemblies],
                    (SELECT COUNT(*) FROM [Frameworks])             AS [NumberOfFrameworks],
                    (SELECT COUNT(*) FROM [FrameworkAssemblies])    AS [NumberOfFrameworkAssemblies]
            ");

            return result;

        }

        public async Task<IReadOnlyList<string>> GetFrameworksAsync()
        {
            var result = await _sqliteConnection.QueryAsync<string>(@"
                SELECT  FriendlyName
                FROM    Frameworks
                ORDER   BY 1 
            ");

            return result.ToArray();
        }

        public async Task<IReadOnlyList<CatalogApi>> GetApisWithParentsAsync(IReadOnlyList<Guid> apiGuids)
        {
            var guidList = string.Join(", ", apiGuids.Select(g => $"'{g:N}'"));

            var rows = await _sqliteConnection.QueryAsync<CatalogApi>($@"
                WITH Parents AS
                (
	                SELECT	a.*
	                FROM	Apis a
	                WHERE 	a.ApiGuid IN ({guidList})
	
	                UNION ALL
	
	                SELECT	a.*
	                FROM	Parents p
				                JOIN Apis a ON a.ApiId = p.ParentApiId
                )
                SELECT	DISTINCT
		                *
                FROM	Parents
            ");

            var result = rows.ToArray();

            return result;
        }

        public async Task<IReadOnlyList<CatalogApi>> GetNamespacesAsync()
        {
            var rows = await _sqliteConnection.QueryAsync<CatalogApi>(@"
                SELECT  *
                FROM    Apis
                WHERE   ParentApiId IS NULL
            ");

            var result = rows.ToArray();
            Array.Sort(result);

            return result;
        }

        private async Task<IReadOnlyList<CatalogApi>> GetAncestorsAndSelf(string apiFingerprint)
        {
            var result = await _sqliteConnection.QueryAsync<CatalogApi>(@"
                WITH Parents AS
                (
                    SELECT  *
                    FROM    Apis a
                    WHERE   a.ApiGuid = @ApiGuid

                    UNION   ALL

	                SELECT  a.*
	                FROM    Apis a
				                JOIN Parents p ON p.ParentApiId = a.ApiId
                )
                SELECT  *
                FROM    Parents
            ", new
            {
                ApiGuid = apiFingerprint
            });

            return result.ToArray();
        }

        private async Task<IReadOnlyList<CatalogApi>> GetChildrenAsync(int apiId)
        {
            var result = await _sqliteConnection.QueryAsync<CatalogApi>(@"
                SELECT  *
                FROM    Apis a
                WHERE   a.ParentApiId = @ParentApiId
            ", new
            {
                ParentApiId = apiId
            });

            return result.ToArray();
        }

        private async Task<Dictionary<int, bool>> GetApiSupportAsync(int apiId, string frameworkName)
        {
            var rows = await _sqliteConnection.QueryAsync<(int ApiId, string PlatformFrameworks, string PackageFrameworks)>(@"
                SELECT	a.ApiId,
		                (
			                SELECT	GROUP_CONCAT(f.FriendlyName)
			                FROM	Declarations d
						                JOIN Assemblies asm ON asm.AssemblyId = d.AssemblyId
						                JOIN FrameworkAssemblies fa ON fa.AssemblyId = asm.AssemblyId
						                JOIN Frameworks f ON f.FrameworkId = fa.FrameworkId
			                WHERE	d.ApiId = a.ApiId
		                ) AS PlatformFrameworks,
		                (
			                SELECT	GROUP_CONCAT(f.FriendlyName)
			                FROM	Declarations d
						                JOIN Assemblies asm ON asm.AssemblyId = d.AssemblyId
						                JOIN PackageAssemblies pa ON pa.AssemblyId = asm.AssemblyId
						                JOIN Frameworks f ON f.FrameworkId = pa.FrameworkId
			                WHERE	d.ApiId = a.ApiId
		                ) AS PackageFrameworks
                FROM	Apis a
                WHERE	a.ApiId = @ApiId
                OR		a.ParentApiId = @ApiId
            ", new
            {
                ApiId = apiId
            });

            var framework = NuGetFramework.ParseFolder(frameworkName);
            var reducer = new FrameworkReducer();
            var result = new Dictionary<int, bool>();

            foreach (var entry in rows)
            {
                var platformFrameworks = (entry.PlatformFrameworks ?? string.Empty).Split(',').Select(NuGetFramework.ParseFolder);
                var reduced = reducer.GetNearest(framework, platformFrameworks);
                if (reduced == null)
                {
                    var packageFrameworks = (entry.PackageFrameworks ?? string.Empty).Split(',').Select(NuGetFramework.ParseFolder);
                    reduced = reducer.GetNearest(framework, packageFrameworks);
                }

                result.Add(entry.ApiId, reduced != null);
            }

            return result;
        }

        public async Task<CatalogApiSpine> GetSpineAsync(string apiFingerprint, string frameworkName)
        {
            var ancestorsAndSelf = await GetAncestorsAndSelf(apiFingerprint);
            var selected = ancestorsAndSelf.FirstOrDefault();
            if (selected == null)
                return null;

            var result = new CatalogApiSpine();
            result.Selected = selected;

            var canHaveChildren = !selected.Kind.IsMember();

            if (canHaveChildren)
            {
                result.Parents.AddRange(ancestorsAndSelf);
                result.Children.AddRange(await GetChildrenAsync(selected.ApiId));
            }
            else
            {
                var siblingsAndSelf = await GetChildrenAsync(selected.ParentApiId);
                result.Parents.AddRange(ancestorsAndSelf.Skip(1));
                result.Children.AddRange(siblingsAndSelf);

                var indexOfCurrent = result.Children.IndexOf(result.Children.Single(c => c.ApiId == selected.ApiId));
                result.Children[indexOfCurrent] = selected;
            }

            result.Children.Sort();

            if (!string.IsNullOrEmpty(frameworkName))
            {
                var support = await GetApiSupportAsync(result.Root.ApiId, frameworkName);
                result.Root.IsUnsupported = !support[result.Root.ApiId];
                foreach (var child in result.Children)
                    child.IsUnsupported = !support[child.ApiId];
            }

            return result;
        }

        private async Task<IReadOnlyList<CatalogFrameworkAvailability>> GetFrameworkAvailabilityAsync(string apiFingerprint)
        {
            var result = await _sqliteConnection.QueryAsync<CatalogFrameworkAvailability>(@"
                SELECT	f.FriendlyName AS FrameworkName,
                        a.AssemblyGuid AS AssemblyFingerprint,
		                a.Name AS AssemblyName,
		                a.PublicKeyToken AS AssemblyPublicKeyToken,
		                a.Version AS AssemblyVersion
                FROM	Apis api
                            JOIN Declarations d ON d.ApiId = api.ApiId
			                JOIN Assemblies a on a.AssemblyId = d.AssemblyId
			                JOIN FrameworkAssemblies fa on fa.AssemblyId = a.AssemblyId
			                JOIN Frameworks f on f.FrameworkId = fa.FrameworkId
                WHERE	api.ApiGuid = @ApiGuid
            ", new
            {
                ApiGuid = apiFingerprint
            });

            return result.ToArray();
        }

        private async Task<IReadOnlyList<CatalogPackageAvailability>> GetPackageAvailabilityAsync(string apiFingerprint)
        {
            var result = await _sqliteConnection.QueryAsync<CatalogPackageAvailability>(@"
                SELECT	p.Name AS PackageName,
		                pv.Version AS PackageVersion,
		                f.FriendlyName AS FrameworkName,
		                a.AssemblyGuid AS AssemblyFingerprint,
		                a.Name AS AssemblyName,
		                a.PublicKeyToken AS AssemblyPublicKeyToken,
		                a.Version AS AssemblyVersion
                FROM	Apis api
			                JOIN Declarations d ON d.ApiId = api.ApiId
			                JOIN Assemblies a ON a.AssemblyId = d.AssemblyId
			                JOIN PackageAssemblies pa ON pa.AssemblyId = a.AssemblyId
			                JOIN Frameworks f ON f.FrameworkId = pa.FrameworkId
			                JOIN PackageVersions pv ON pv.PackageVersionId = pa.PackageVersionId
			                JOIN Packages p ON p.PackageId = pv.PackageId
                WHERE	api.ApiGuid = @ApiGuid
            ", new
            {
                ApiGuid = apiFingerprint
            });

            return result.ToArray();
        }

        public async Task<CatalogAvailability> GetAvailabilityAsync(string apiFingerprint, string framework)
        {
            var frameworks = await GetFrameworksAsync();
            var frameworkAvailabilities = await GetFrameworkAvailabilityAsync(apiFingerprint);
            var packageAvailabilities = await GetPackageAvailabilityAsync(apiFingerprint);
            var availability = new CatalogAvailability(frameworks, framework, frameworkAvailabilities, packageAvailabilities);
            return availability;
        }

        public async Task<string> GetSyntaxAsync(string apiFingerprint, string assemblyFingerprint)
        {
            var result = await _sqliteConnection.QueryAsync<string>(@"
                WITH ApiParents AS
                (
                    SELECT  0 AS [Order],
                            a.ApiId,
                            a.ParentApiId,
                            a.Kind,
                            a.Name
                    FROM    Apis a
                    WHERE   a.ApiGuid = @ApiGuid

                    UNION   ALL

	                SELECT  p.[Order] + 1,
                            a.ApiId,
                            a.ParentApiId,
                            a.Kind,
                            a.Name
	                FROM    Apis a
				                JOIN ApiParents p ON p.ParentApiId = a.ApiId
                )
                SELECT   s.Syntax
                FROM     ApiParents api
                            JOIN Declarations d INDEXED BY IX_Declarations_ApiId ON d.ApiId = api.ApiId
			                JOIN Assemblies a on a.AssemblyId = d.AssemblyId
                            JOIN Syntaxes s ON s.SyntaxId = d.SyntaxId
                WHERE    a.AssemblyGuid = @AassemblyGuid
                AND      api.Name != '<global namespace>'
                ORDER BY api.[Order] DESC
            ", new
            {
                ApiGuid = apiFingerprint,
                AassemblyGuid = assemblyFingerprint,
            });

            var rows = result.ToArray();

            using var stringWriter = new StringWriter();
            using var writer = new IndentedTextWriter(stringWriter, new string(' ', 4));

            for (int i = 0; i < rows.Length; i++)
            {
                if (i > 0)
                {
                    writer.WriteLine("<p>{</p>");
                    writer.Indent++;
                }

                var lineReader = new StringReader(rows[i]);
                string line;
                while ((line = lineReader.ReadLine()) != null)
                {
                    writer.WriteLine(line);
                }
            }

            while (writer.Indent > 0)
            {
                writer.Indent--;
                writer.WriteLine("<p>}</p>");
            }

            writer.Flush();

            return stringWriter.ToString();
        }

        public void Dispose()
        {
            _sqliteConnection.Dispose();
        }
    }
}