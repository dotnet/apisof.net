using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dapper;

using Microsoft.Data.Sqlite;

namespace ApiCatalog.CatalogModel
{
    public sealed partial class ApiCatalogModel
    {
        internal sealed class Converter
        {
            private readonly SqliteConnection _connection;

            private readonly Dictionary<string, int> _stringOffsetByString = new Dictionary<string, int>();
            private readonly Dictionary<int, int> _frameworkOffsetById = new Dictionary<int, int>();
            private readonly Dictionary<int, int> _packageOffsetById = new Dictionary<int, int>();
            private readonly Dictionary<int, int> _assemblyOffsetById = new Dictionary<int, int>();
            private readonly Dictionary<int, int> _apiOffsetById = new Dictionary<int, int>();
            private readonly Dictionary<Guid, int> _apiOffsetByGuid = new Dictionary<Guid, int>();

            private readonly TableWriter _stringTable = new TableWriter();
            private readonly TableWriter _frameworkTable = new TableWriter();
            private readonly TableWriter _packageTable = new TableWriter();
            private readonly TableWriter _assemblyTable = new TableWriter();
            private readonly TableWriter _apiTable = new TableWriter();

            private readonly List<int> _frameworkTableAssemblyPatchups = new List<int>();
            private readonly List<int> _packagesTableAssemblyPatchups = new List<int>();
            private readonly List<int> _assemblyTableApiPatchups = new List<int>();
            private readonly List<(int StringOffset, Guid ApiGuid)> _stringTableApiPatchups = new List<(int, Guid)>();

            private Converter(SqliteConnection connection)
            {
                _connection = connection;
            }

            public static async Task ConvertAsync(string sqliteDbPath, Stream stream)
            {
                var connectionString = new SqliteConnectionStringBuilder
                {
                    DataSource = sqliteDbPath,
                    Mode = SqliteOpenMode.ReadOnly
                }.ToString();

                using (var connection = new SqliteConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var converter = new Converter(connection);
                    await converter.ConvertAsync(stream);
                }
            }

            public async Task ConvertAsync(Stream stream)
            {
                var tables = new[]
                {
                    _stringTable,
                    _frameworkTable,
                    _packageTable,
                    _assemblyTable,
                    _apiTable,
                };

                await WriteFrameworksAsync();
                await WritePackagesAsync();
                await WriteAssembliesAsync();
                await WriteApisAsync();

                PatchFrameworks();
                PatchPackages();
                PatchAssemblies();
                PatchStrings();

                using (var binaryWriter = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                {
                    // Magic value

                    foreach (var b in MagicHeader)
                        binaryWriter.Write(b);

                    // Version

                    binaryWriter.Write(FormatVersion);

                    // Table sizes

                    binaryWriter.Write(tables.Length);
                    foreach (var table in tables)
                        binaryWriter.Write(table.Length);
                }

                // Compressed buffer

                using (var deflateStream = new DeflateStream(stream, CompressionLevel.Optimal, leaveOpen: true))
                {
                    foreach (var table in tables)
                        await table.CopyToAsync(deflateStream);
                }
            }

            private int WriteString(string value)
            {
                if (!_stringOffsetByString.TryGetValue(value, out var offset))
                {
                    offset = _stringTable.Offset;
                    _stringTable.WriteString(value);
                    _stringOffsetByString.Add(value, offset);
                }

                return offset;
            }

            private int WriteSyntax(string value)
            {
                if (!_stringOffsetByString.TryGetValue(value, out var offset))
                {
                    var parts = Markup.Parse(value).Parts.ToList();
                    foreach (var part in parts)
                        WriteString(part.Text);

                    offset = _stringTable.Offset;

                    _stringOffsetByString.Add(value, offset);
                    _stringTable.WriteInt32(parts.Count);

                    foreach (var part in parts)
                    {
                        _stringTable.WriteByte((byte)part.Kind);
                        _stringTable.WriteInt32(_stringOffsetByString[part.Text]);

                        if (part.Kind == MarkupPartKind.Reference)
                        {
                            if (part.Reference != null)
                                _stringTableApiPatchups.Add((_stringTable.Offset, part.Reference.Value));

                            _stringTable.WriteInt32(-1);
                        }
                    }
                }

                return offset;
            }

            private async Task WriteFrameworksAsync()
            {
                var rows = await _connection.QueryAsync<FrameworkRow>(@"
                    SELECT  f.FrameworkId,
                            f.FriendlyName,
                            (
                                SELECT  GROUP_CONCAT(fa.AssemblyId)
                                FROM    FrameworkAssemblies fa
                                WHERE   fa.FrameworkId = f.FrameworkId
                            ) AS AssemblyList
                    FROM    Frameworks f
                ");

                var rowList = rows.ToList();

                _frameworkTable.WriteInt32(rowList.Count);

                var tableStart = _frameworkTable.Offset;

                _frameworkTable.WriteInt32Placeholders(rowList.Count);

                for (var i = 0; i < rowList.Count; i++)
                {
                    var rowOffset = _frameworkTable.Offset;
                    _frameworkTable.Offset = tableStart + i * 4;
                    _frameworkTable.WriteInt32(rowOffset);
                    _frameworkTable.Offset = rowOffset;

                    var row = rowList[i];
                    var assemblyIds = (row.AssemblyList ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                                        .Select(int.Parse)
                                                                        .ToArray();

                    _frameworkOffsetById.Add(row.FrameworkId, rowOffset);
                    _frameworkTable.WriteInt32(WriteString(row.FriendlyName));

                    _frameworkTableAssemblyPatchups.Add(_frameworkTable.Offset);

                    _frameworkTable.WriteInt32(assemblyIds.Length);
                    foreach (var assemblyId in assemblyIds)
                        _frameworkTable.WriteInt32(assemblyId);
                }
            }

            private async Task WritePackagesAsync()
            {
                var rows = await _connection.QueryAsync<PackageRow>(@"
                    SELECT  pv.PackageVersionId AS PackageId,
                            p.Name,
                            pv.Version,
                            (
                                SELECT  GROUP_CONCAT(pa.FrameworkId || ';' || pa.AssemblyId)
                                FROM    PackageAssemblies pa
                                WHERE   pa.PackageVersionId = pv.PackageVersionId
                            ) AS AssemblyIds
                    FROM    Packages p
                                JOIN PackageVersions pv ON pv.PackageId = p.PackageId
                ");

                var rowList = rows.ToList();

                _packageTable.WriteInt32(rowList.Count);

                var tableStart = _packageTable.Offset;

                _packageTable.WriteInt32Placeholders(rowList.Count);

                for (var i = 0; i < rowList.Count; i++)
                {
                    var rowOffset = _packageTable.Offset;
                    _packageTable.Offset = tableStart + i * 4;
                    _packageTable.WriteInt32(rowOffset);
                    _packageTable.Offset = rowOffset;

                    var row = rowList[i];
                    var assemblyIds = (row.AssemblyIds ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                                       .Select(p =>
                                                                       {
                                                                           var pair = p.Split(';');
                                                                           return (FrameworkId: int.Parse(pair[0]), AssemblyId: int.Parse(pair[1]));
                                                                       })
                                                                       .Select(t => (FrameworkOffset: _frameworkOffsetById[t.FrameworkId], t.AssemblyId))
                                                                       .ToArray();

                    _packageOffsetById.Add(row.PackageId, rowOffset);
                    _packageTable.WriteInt32(WriteString(row.Name));
                    _packageTable.WriteInt32(WriteString(row.Version));

                    _packagesTableAssemblyPatchups.Add(_packageTable.Offset);

                    _packageTable.WriteInt32(assemblyIds.Length);
                    foreach (var (frameworkOffset, assemblyId) in assemblyIds)
                    {
                        _packageTable.WriteInt32(frameworkOffset);
                        _packageTable.WriteInt32(assemblyId);
                    }
                }
            }

            private async Task WriteAssembliesAsync()
            {
                var rows = await _connection.QueryAsync<AssemblyRow>(@"
                    SELECT  a.AssemblyId,
                            a.AssemblyGuid,
                            a.Name,
                            a.Version,
                            a.PublicKeyToken,
                            (
                                SELECT  GROUP_CONCAT(d.ApiId)
                                FROM    Declarations d
                                            JOIN Apis api ON api.ApiId = d.ApiId
                                WHERE   d.AssemblyId = a.AssemblyId
                                AND     api.ParentApiId IS NULL
                            ) RootApiIds,
                            (
                                SELECT  GROUP_CONCAT(fa.FrameworkId)
                                FROM    FrameworkAssemblies fa
                                WHERE   fa.AssemblyId = a.AssemblyId
                            ) AS FrameworkIds,
                            (
                                SELECT  GROUP_CONCAT(pa.PackageVersionId || ';' || pa.FrameworkId)
                                FROM    PackageAssemblies pa
                                WHERE   pa.AssemblyId = a.AssemblyId
                            ) AS PackageIds
                    FROM    Assemblies a
                ");

                var rowList = rows.ToList();

                _assemblyTable.WriteInt32(rowList.Count);

                var tableStart = _assemblyTable.Offset;

                _assemblyTable.WriteInt32Placeholders(rowList.Count);

                for (var i = 0; i < rowList.Count; i++)
                {
                    var rowOffset = _assemblyTable.Offset;
                    _assemblyTable.Offset = tableStart + i * 4;
                    _assemblyTable.WriteInt32(rowOffset);
                    _assemblyTable.Offset = rowOffset;

                    var row = rowList[i];
                    var rootApiIds = (row.RootApiIds ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                                     .Select(int.Parse)
                                                                     .ToArray();
                    var frameworkOffsets = (row.FrameworkIds ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                                             .Select(int.Parse)
                                                                             .Select(id => _frameworkOffsetById[id])
                                                                             .ToArray();

                    var packageOffsets = (row.PackageIds ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                                         .Select(p =>
                                                                         {
                                                                             var pair = p.Split(';');
                                                                             return (PackageId: int.Parse(pair[0]), FrameworkdId: int.Parse(pair[1]));
                                                                         })
                                                                         .Select(t => (PackageOffset: _packageOffsetById[t.PackageId], FrameworkOffset: _frameworkOffsetById[t.FrameworkdId]))
                                                                         .ToArray();

                    _assemblyOffsetById.Add(row.AssemblyId, rowOffset);
                    _assemblyTable.WriteGuid(row.AssemblyGuid);
                    _assemblyTable.WriteInt32(WriteString(row.Name));
                    _assemblyTable.WriteInt32(WriteString(row.PublicKeyToken));
                    _assemblyTable.WriteInt32(WriteString(row.Version));

                    _assemblyTableApiPatchups.Add(_assemblyTable.Offset);
                    _assemblyTable.WriteInt32(rootApiIds.Length);
                    foreach (var rootApiId in rootApiIds)
                        _assemblyTable.WriteInt32(rootApiId);

                    _assemblyTable.WriteInt32(frameworkOffsets.Length);
                    foreach (var frameworkOffset in frameworkOffsets)
                        _assemblyTable.WriteInt32(frameworkOffset);

                    _assemblyTable.WriteInt32(packageOffsets.Length);
                    foreach (var (packageOffset, frameworkOffset) in packageOffsets)
                    {
                        _assemblyTable.WriteInt32(packageOffset);
                        _assemblyTable.WriteInt32(frameworkOffset);
                    }
                }
            }

            private async Task WriteApisAsync()
            {
                var rows = await _connection.QueryAsync<ApiRow>(@"
                    SELECT  a.ApiId,
                            a.ApiGuid,
                            a.Kind,
                            a.ParentApiId,
                            a.Name
                    FROM    Apis a
                    WHERE   a.ParentApiId IS NULL
                    ORDER   BY a.ApiId
                ");

                var rowList = rows.ToList();

                _apiTable.WriteInt32(rowList.Count);

                var childArrayStart = _apiTable.Offset;

                for (var i = 0; i < rowList.Count; i++)
                    _apiTable.WriteInt32(-1);

                await WriteApisAsync(rowList, -1, childArrayStart);
            }

            private async Task WriteApisAsync(IReadOnlyList<ApiRow> rows, int parentOffset, int parentChildArrayStart)
            {
                for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                {
                    var row = rows[rowIndex];
                    var childRows = await _connection.QueryAsync<ApiRow>(@"
                        SELECT  a.ApiId,
                                a.ApiGuid,
                                a.Kind,
                                a.ParentApiId,
                                a.Name
                        FROM    Apis a
                        WHERE   a.ParentApiId = @ParentApiId
                        ORDER   BY a.ApiId
                    ", new { ParentApiId = row.ApiId });

                    var childRowsList = childRows.ToList();

                    var declarationRows = await _connection.QueryAsync<DeclarationRow>(@"
                        SELECT  d.AssemblyId,
                        s.Syntax
                        FROM    Declarations d
                                    JOIN Syntaxes s ON s.SyntaxId = d.SyntaxId
                        WHERE   d.ApiId = @ApiId
                    ", new { ApiId = row.ApiId });

                    var declarationRowList = declarationRows.ToList();

                    var rowOffset = _apiTable.Offset;
                    _apiOffsetById.Add(row.ApiId, rowOffset);
                    _apiOffsetByGuid.Add(Guid.Parse(row.ApiGuid), rowOffset);

                    _apiTable.Offset = parentChildArrayStart + rowIndex * 4;
                    _apiTable.WriteInt32(rowOffset);
                    _apiTable.Offset = rowOffset;

                    var nameOffset = WriteString(row.Name);

                    _apiTable.WriteGuid(row.ApiGuid);
                    _apiTable.WriteByte(row.Kind);
                    _apiTable.WriteInt32(parentOffset);
                    _apiTable.WriteInt32(nameOffset);
                    _apiTable.WriteInt32(childRowsList.Count);

                    var childArrayStart = _apiTable.Offset;

                    for (var i = 0; i < childRowsList.Count; i++)
                        _apiTable.WriteInt32(-1);

                    _apiTable.WriteInt32(declarationRowList.Count);

                    for (var i = 0; i < declarationRowList.Count; i++)
                    {
                        _apiTable.WriteInt32(_assemblyOffsetById[declarationRowList[i].AssemblyId]);
                        _apiTable.WriteInt32(WriteSyntax(declarationRowList[i].Syntax));
                    }

                    await WriteApisAsync(childRowsList, rowOffset, childArrayStart);
                }
            }

            private void PatchFrameworks()
            {
                var offset = _frameworkTable.Offset;

                foreach (var assemblyListOffset in _frameworkTableAssemblyPatchups)
                {
                    _frameworkTable.Offset = assemblyListOffset;
                    var count = _frameworkTable.ReadInt32();

                    for (var i = 0; i < count; i++)
                    {
                        var assemblyId = _frameworkTable.ReadInt32();
                        _frameworkTable.Offset -= 4;
                        _frameworkTable.WriteInt32(_assemblyOffsetById[assemblyId]);
                    }
                }

                _frameworkTable.Offset = offset;
            }

            private void PatchPackages()
            {
                var offset = _packageTable.Offset;

                foreach (var frameworkListOffset in _packagesTableAssemblyPatchups)
                {
                    _packageTable.Offset = frameworkListOffset;
                    var count = _packageTable.ReadInt32();

                    for (var i = 0; i < count; i++)
                    {
                        _packageTable.ReadInt32(); // Skip frameworkOffset
                        var assemblyId = _packageTable.ReadInt32();
                        _packageTable.Offset -= 4;
                        _packageTable.WriteInt32(_assemblyOffsetById[assemblyId]);
                    }
                }

                _packageTable.Offset = offset;
            }

            private void PatchAssemblies()
            {
                var offset = _assemblyTable.Offset;

                foreach (var apiListOffset in _assemblyTableApiPatchups)
                {
                    _assemblyTable.Offset = apiListOffset;
                    var count = _assemblyTable.ReadInt32();

                    for (var i = 0; i < count; i++)
                    {
                        var apiId = _assemblyTable.ReadInt32();
                        _assemblyTable.Offset -= 4;
                        _assemblyTable.WriteInt32(_apiOffsetById[apiId]);
                    }
                }

                _assemblyTable.Offset = offset;
            }

            private void PatchStrings()
            {
                var offset = _stringTable.Offset;

                foreach (var (stringOffset, apiGuid) in _stringTableApiPatchups)
                {
                    if (_apiOffsetByGuid.TryGetValue(apiGuid, out var apiOffset))
                    {
                        _stringTable.Offset = stringOffset;
                        _stringTable.WriteInt32(apiOffset);
                    }
                }

                _stringTable.Offset = offset;
            }

            private sealed class FrameworkRow
            {
                public int FrameworkId { get; set; }
                public string FriendlyName { get; set; }
                public string AssemblyList { get; set; }
            }

            private sealed class PackageRow
            {
                public int PackageId { get; set; }
                public string Name { get; set; }
                public string Version { get; set; }
                public string AssemblyIds { get; set; }
            }

            private sealed class AssemblyRow
            {
                public int AssemblyId { get; set; }
                public string AssemblyGuid { get; set; }
                public string Name { get; set; }
                public string Version { get; set; }
                public string PublicKeyToken { get; set; }
                public string RootApiIds { get; set; }
                public string FrameworkIds { get; set; }
                public string PackageIds { get; set; }
            }

            private sealed class ApiRow
            {
                public int ApiId { get; set; }
                public string ApiGuid { get; set; }
                public int Kind { get; set; }
                public int ParentApiId { get; set; }
                public string Name { get; set; }
            }

            private sealed class DeclarationRow
            {
                public int AssemblyId { get; set; }
                public string Syntax { get; set; }
            }

            private sealed class TableWriter
            {
                private readonly MemoryStream _stream;
                private readonly BinaryWriter _writer;

                public TableWriter()
                {
                    _stream = new MemoryStream();
                    _writer = new BinaryWriter(_stream);
                }

                public int Offset
                {
                    get
                    {
                        _writer.Flush();
                        return (int)_stream.Position;
                    }

                    set => _stream.Position = value;
                }

                public int Length
                {
                    get
                    {
                        _writer.Flush();
                        return (int)_stream.Length;
                    }
                }

                public void WriteInt32(int value)
                {
                    _writer.Write(value);
                }

                public void WriteByte(int value)
                {
                    _writer.Write((byte)value);
                }

                public void WriteGuid(string value)
                {
                    var guid = Guid.Parse(value);
                    var span = (Span<byte>)stackalloc byte[16];
                    Debug.Assert(guid.TryWriteBytes(span));
                    _writer.Write(span);
                }

                public void WriteString(string value)
                {
                    var length = value.Length;
                    var bytes = Encoding.UTF8.GetBytes(value);
                    WriteInt32(length);
                    _writer.Write(bytes);
                }

                public void WriteInt32Placeholders(int length)
                {
                    for (var i = 0; i < length; i++)
                        WriteInt32(-1);
                }

                public int ReadInt32()
                {
                    var byteSpan = (Span<byte>)stackalloc byte[4];
                    _stream.Read(byteSpan);
                    return BinaryPrimitives.ReadInt32LittleEndian(byteSpan);
                }

                public async Task CopyToAsync(Stream destination)
                {
                    _writer.Flush();
                    var position = _stream.Position;
                    _stream.Position = 0;
                    await _stream.CopyToAsync(destination);
                    _stream.Position = position;
                }
            }
        }
    }
}
