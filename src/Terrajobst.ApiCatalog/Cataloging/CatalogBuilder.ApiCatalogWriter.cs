#nullable enable

using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace Terrajobst.ApiCatalog;

public sealed partial class CatalogBuilder
{
    internal sealed class ApiCatalogWriter
    {
        private readonly CatalogBuilder _builder;
        private readonly Dictionary<string, int> _stringOffsetByString = new();
        private readonly Dictionary<IntermediateFramework, int> _frameworkOffsets = new();
        private readonly Dictionary<IntermediatePackage, int> _packageOffsets = new();
        private readonly Dictionary<int, int> _assemblyOffsetByIndex = new();
        private readonly Dictionary<IntermediateUsageSource, int> _usageSourceOffsets = new();
        private readonly Dictionary<int, int> _apiOffsetByIndex = new();
        private readonly Dictionary<Guid, int> _apiOffsetByGuid = new();

        private readonly TableWriter _stringTable = new();
        private readonly TableWriter _platformTable = new();
        private readonly TableWriter _frameworkTable = new();
        private readonly TableWriter _packageTable = new();
        private readonly TableWriter _assemblyTable = new();
        private readonly TableWriter _usageSourcesTable = new();
        private readonly TableWriter _apiTable = new();
        private readonly TableWriter _obsoletionTable = new();
        private readonly TableWriter _platformSupportTable = new();
        private readonly TableWriter _previewRequirementTable = new();
        private readonly TableWriter _experimentalTable = new();
        private readonly TableWriter _extensionMethodsTable = new();

        private readonly List<int> _frameworkTableAssemblyPatchups = new();
        private readonly List<int> _packagesTableAssemblyPatchups = new();
        private readonly List<int> _assemblyTableApiPatchups = new();
        private readonly List<(int StringOffset, Guid ApiGuid)> _stringTableApiPatchups = new();

        public ApiCatalogWriter(CatalogBuilder builder)
        {
            _builder = builder;
        }
        
        public void Write(Stream stream)
        {
            var tables = new[]
            {
                _stringTable,
                _platformTable,
                _frameworkTable,
                _packageTable,
                _assemblyTable,
                _usageSourcesTable,
                _apiTable,
                _obsoletionTable,
                _platformSupportTable,
                _previewRequirementTable,
                _experimentalTable,
                _extensionMethodsTable,
            };

            WritePlatforms();
            WriteFrameworks();
            WritePackages();
            WriteAssemblies();
            WriteUsageSources();
            WriteApis();
            WriteObsoletions();
            WritePlatformSupport();
            WritePreviewRequirements();
            WriteExperimentals();
            WriteExtensionMethods();

            PatchFrameworks();
            PatchPackages();
            PatchAssemblies();
            PatchStrings();

            using (var binaryWriter = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                // Magic value

                foreach (var b in ApiCatalogModel.MagicHeader)
                    binaryWriter.Write(b);

                // Version

                binaryWriter.Write(ApiCatalogModel.CurrentFormatVersion);

                // Table sizes

                binaryWriter.Write(tables.Length);
                foreach (var table in tables)
                    binaryWriter.Write(table.Length);
            }

            // Compressed buffer

            using (var deflateStream = new DeflateStream(stream, CompressionLevel.Optimal, leaveOpen: true))
            {
                foreach (var table in tables)
                    table.CopyTo(deflateStream);
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
                        if (part.Reference is not null)
                            _stringTableApiPatchups.Add((_stringTable.Offset, part.Reference.Value));

                        _stringTable.WriteInt32(-1);
                    }
                }
            }

            return offset;
        }

        private void WritePlatforms()
        {
            var platforms = _builder._platformNames;

            _platformTable.WriteInt32(platforms.Count);

            foreach (var platform in platforms)
            {
                var stringOffset = WriteString(platform);

                _platformTable.WriteInt32(stringOffset);
            }
        }

        private void WriteFrameworks()
        {
            var frameworks = _builder._frameworkByName.Values.ToArray();

            _frameworkTable.WriteInt32(frameworks.Length);

            var tableStart = _frameworkTable.Offset;

            _frameworkTable.WriteInt32Placeholders(frameworks.Length);

            for (var i = 0; i < frameworks.Length; i++)
            {
                var rowOffset = _frameworkTable.Offset;
                _frameworkTable.Offset = tableStart + i * 4;
                _frameworkTable.WriteInt32(rowOffset);
                _frameworkTable.Offset = rowOffset;

                var framework = frameworks[i];
                var assemblyIds = framework.Assemblies;

                _frameworkOffsets.Add(framework, rowOffset);
                _frameworkTable.WriteInt32(WriteString(framework.Name));

                _frameworkTableAssemblyPatchups.Add(_frameworkTable.Offset);

                _frameworkTable.WriteInt32(assemblyIds.Count);
                foreach (var assembly in assemblyIds)
                    _frameworkTable.WriteInt32(assembly.Index);
            }
        }

        private void WritePackages()
        {
            var packages = _builder._packageByFingerprint.Values.ToArray();

            _packageTable.WriteInt32(packages.Length);

            var tableStart = _packageTable.Offset;

            _packageTable.WriteInt32Placeholders(packages.Length);

            for (var i = 0; i < packages.Length; i++)
            {
                var rowOffset = _packageTable.Offset;
                _packageTable.Offset = tableStart + i * 4;
                _packageTable.WriteInt32(rowOffset);
                _packageTable.Offset = rowOffset;

                var package = packages[i];
                var assemblies = package.Assemblies.ToArray();

                _packageOffsets.Add(package, rowOffset);
                _packageTable.WriteInt32(WriteString(package.Name));
                _packageTable.WriteInt32(WriteString(package.Version));

                _packagesTableAssemblyPatchups.Add(_packageTable.Offset);

                _packageTable.WriteInt32(assemblies.Length);
                foreach (var (framework, assembly) in assemblies)
                {
                    var frameworkOffset = _frameworkOffsets[framework];
                    _packageTable.WriteInt32(frameworkOffset);
                    _packageTable.WriteInt32(assembly.Index);
                }
            }
        }

        private void WriteAssemblies()
        {
            var assemblies = _builder._assemblies;

            _assemblyTable.WriteInt32(assemblies.Count);

            var tableStart = _assemblyTable.Offset;

            _assemblyTable.WriteInt32Placeholders(assemblies.Count);

            for (var i = 0; i < assemblies.Count; i++)
            {
                var rowOffset = _assemblyTable.Offset;
                _assemblyTable.Offset = tableStart + i * 4;
                _assemblyTable.WriteInt32(rowOffset);
                _assemblyTable.Offset = rowOffset;

                var assembly = assemblies[i];
                var rootApis = assembly.RootApis.ToArray();
                var frameworks = assembly.Frameworks;
                var packages = assembly.Packages;

                _assemblyOffsetByIndex.Add(assembly.Index, rowOffset);
                _assemblyTable.WriteGuid(assembly.Fingerprint);
                _assemblyTable.WriteInt32(WriteString(assembly.Name));
                _assemblyTable.WriteInt32(WriteString(assembly.PublicKeyToken));
                _assemblyTable.WriteInt32(WriteString(assembly.Version));

                _assemblyTableApiPatchups.Add(_assemblyTable.Offset);
                _assemblyTable.WriteInt32(rootApis.Length);
                foreach (var rootApi in rootApis)
                    _assemblyTable.WriteInt32(rootApi.Index);

                _assemblyTable.WriteInt32(frameworks.Count);
                foreach (var framework in frameworks)
                {
                    var frameworkOffset = _frameworkOffsets[framework];
                    _assemblyTable.WriteInt32(frameworkOffset);
                }

                _assemblyTable.WriteInt32(packages.Count);
                foreach (var (package, framework) in packages)
                {
                    var packageOffset = _packageOffsets[package];
                    var frameworkOffset = _frameworkOffsets[framework];
                    _assemblyTable.WriteInt32(packageOffset);
                    _assemblyTable.WriteInt32(frameworkOffset);
                }
            }
        }

        private void WriteUsageSources()
        {
            var usageSources = _builder._usageSources.Values;

            _usageSourcesTable.WriteInt32(usageSources.Count);

            foreach (var row in usageSources)
            {
                _usageSourceOffsets.Add(row, _usageSourcesTable.Offset);

                var nameOffset = WriteString(row.Name);
                var dayNumber = row.Date.DayNumber;

                _usageSourcesTable.WriteInt32(nameOffset);
                _usageSourcesTable.WriteInt32(dayNumber);
            }
        }

        private void WriteApis()
        {
            var roots = _builder._rootApis;

            _apiTable.WriteInt32(roots.Count);

            var childArrayStart = _apiTable.Offset;

            for (var i = 0; i < roots.Count; i++)
                _apiTable.WriteInt32(-1);

            WriteApis(roots, -1, childArrayStart);
        }

        private void WriteApis(IReadOnlyList<IntermediaApi> apis, int parentOffset, int parentChildArrayStart)
        {
            for (var apiIndex = 0; apiIndex < apis.Count; apiIndex++)
            {
                var api = apis[apiIndex];
                var children = (IReadOnlyList<IntermediaApi>?) api.Children ?? Array.Empty<IntermediaApi>();

                var declarations = GetDeclarations(_builder, api).ToArray();
                var usages = GetUsages(_builder, api).ToArray();

                var rowOffset = _apiTable.Offset;
                _apiOffsetByIndex.Add(api.Index, rowOffset);
                _apiOffsetByGuid.Add(api.Fingerprint, rowOffset);

                _apiTable.Offset = parentChildArrayStart + apiIndex * 4;
                _apiTable.WriteInt32(rowOffset);
                _apiTable.Offset = rowOffset;

                var nameOffset = WriteString(api.Name);

                _apiTable.WriteGuid(api.Fingerprint);
                _apiTable.WriteByte((byte)api.Kind);
                _apiTable.WriteInt32(parentOffset);
                _apiTable.WriteInt32(nameOffset);
                _apiTable.WriteInt32(children.Count);

                var childArrayStart = _apiTable.Offset;

                for (var i = 0; i < children.Count; i++)
                    _apiTable.WriteInt32(-1);

                _apiTable.WriteInt32(declarations.Length);

                foreach (var (assembly, declaration) in declarations)
                {
                    _apiTable.WriteInt32(_assemblyOffsetByIndex[assembly.Index]);
                    _apiTable.WriteInt32(WriteSyntax(declaration.Syntax));
                }

                _apiTable.WriteInt32(usages.Length);

                foreach (var (usageSource, percentage) in usages)
                {
                    _apiTable.WriteInt32(_usageSourceOffsets[usageSource]);
                    _apiTable.WriteSingle(percentage);
                }

                WriteApis(children, rowOffset, childArrayStart);
            }

            static IEnumerable<(IntermediaAssembly Assembly, IntermediateDeclaration Declaration)> GetDeclarations(CatalogBuilder builder, IntermediaApi api)
            {
                foreach (var assembly in builder._assemblies)
                {
                    if (assembly.Declarations.TryGetValue(api, out var declaration))
                        yield return (assembly, declaration);
                }
            }

            static IReadOnlyList<(IntermediateUsageSource UsageSource, float Percentage)> GetUsages(CatalogBuilder builder, IntermediaApi api)
            {
                var result = new List<(IntermediateUsageSource, float)>();

                foreach (var usageSource in builder._usageSources.Values)
                {
                    if (usageSource.Usages.TryGetValue(api.Fingerprint, out var percentage))
                        result.Add((usageSource, percentage));
                }

                return result.ToArray();
            }
        }

        private void WriteObsoletions()
        {
            var entries = _builder._assemblies
                .SelectMany(a => a.Declarations, (a, d) => (Assembly: a, Api: d.Key, d.Value.Obsoletion))
                .Where(d => d.Obsoletion is not null)
                .Select(d => (
                    ApiOffset: _apiOffsetByIndex[d.Api.Index],
                    AssemblyOffset: _assemblyOffsetByIndex[d.Assembly.Index],
                    MessageOffset: WriteString(d.Obsoletion!.Message),
                    d.Obsoletion.IsError,
                    DiagnosticIdOffset: WriteString(d.Obsoletion.DiagnosticId),
                    UrlFormatOffset: WriteString(d.Obsoletion.UrlFormat)
                ))
                .OrderBy(t => t.ApiOffset)
                .ThenBy(t => t.AssemblyOffset)
                .ToArray();
                
            _obsoletionTable.WriteInt32(entries.Length);

            foreach (var entry in entries)
            {
                _obsoletionTable.WriteInt32(entry.ApiOffset);
                _obsoletionTable.WriteInt32(entry.AssemblyOffset);
                _obsoletionTable.WriteInt32(entry.MessageOffset);
                _obsoletionTable.WriteBoolean(entry.IsError);
                _obsoletionTable.WriteInt32(entry.DiagnosticIdOffset);
                _obsoletionTable.WriteInt32(entry.UrlFormatOffset);
            }
        }

        private void WritePlatformSupport()
        {
            var assemblyPlatformSupport = _builder._assemblies
                .Select(a => (Assembly: a, Api: (IntermediaApi?)null, a.PlatformSupport));

            var apiPlatformSupport = _builder._assemblies
                .SelectMany(a => a.Declarations, (a, d) => (Assembly: a, Api: (IntermediaApi?)d.Key, d.Value.PlatformSupport));

            var entries = assemblyPlatformSupport
                .Concat(apiPlatformSupport)
                .Where(e => e.PlatformSupport is not null)
                .SelectMany(e => e.PlatformSupport!, (e, p) => (
                    ApiOffset: e.Api is null ? -1 : _apiOffsetByIndex[e.Api.Index],
                    AssemblyOffset: _assemblyOffsetByIndex[e.Assembly.Index],
                    PlatformOffset: WriteString(p.PlatformName),
                    p.IsSupported
                ))
                .OrderBy(t => t.ApiOffset)
                .ThenBy(t => t.AssemblyOffset)
                .ToArray();

            _platformSupportTable.WriteInt32(entries.Length);

            foreach (var entry in entries)
            {
                _platformSupportTable.WriteInt32(entry.ApiOffset);
                _platformSupportTable.WriteInt32(entry.AssemblyOffset);
                _platformSupportTable.WriteInt32(entry.PlatformOffset);
                _platformSupportTable.WriteBoolean(entry.IsSupported);
            }
        }

        private void WritePreviewRequirements()
        {
            var assemblyPreviewRequirements = _builder._assemblies
                .Select(a => (Assembly: a, Api: (IntermediaApi?)null, a.PreviewRequirement));

            var apiPreviewRequirements = _builder._assemblies
                .SelectMany(a => a.Declarations, (a, d) => (Assembly: a, Api: (IntermediaApi?)d.Key, d.Value.PreviewRequirement));

            var entries = assemblyPreviewRequirements
                .Concat(apiPreviewRequirements)
                .Where(e => e.PreviewRequirement is not null)
                .Select(e => (
                    ApiOffset: e.Api is null ? -1 : _apiOffsetByIndex[e.Api.Index],
                    AssemblyOffset: _assemblyOffsetByIndex[e.Assembly.Index],
                    MessageOffset: WriteString(e.PreviewRequirement!.Message),
                    UrlOffset: WriteString(e.PreviewRequirement.Url)
                ))
                .OrderBy(t => t.ApiOffset)
                .ThenBy(t => t.AssemblyOffset)
                .ToArray();

            _previewRequirementTable.WriteInt32(entries.Length);

            foreach (var entry in entries)
            {
                _previewRequirementTable.WriteInt32(entry.ApiOffset);
                _previewRequirementTable.WriteInt32(entry.AssemblyOffset);
                _previewRequirementTable.WriteInt32(entry.MessageOffset);
                _previewRequirementTable.WriteInt32(entry.UrlOffset);
            }
        }

        private void WriteExperimentals()
        {
            var assemblyExperimental = _builder._assemblies
                .Select(a => (Assembly: a, Api: (IntermediaApi?)null, a.Experimental));

            var apiExperimental = _builder._assemblies
                .SelectMany(a => a.Declarations, (a, d) => (Assembly: a, Api: (IntermediaApi?)d.Key, d.Value.Experimental));

            var entries = assemblyExperimental
                .Concat(apiExperimental)
                .Where(e => e.Experimental is not null)
                .Select(e => (
                    ApiOffset: e.Api is null ? -1 : _apiOffsetByIndex[e.Api.Index],
                    AssemblyOffset: _assemblyOffsetByIndex[e.Assembly.Index],
                    DiagnosticIdOffset: WriteString(e.Experimental!.DiagnosticId),
                    UrlFormatOffset: WriteString(e.Experimental.UrlFormat)
                ))
                .OrderBy(t => t.ApiOffset)
                .ThenBy(t => t.AssemblyOffset)
                .ToArray();

            _experimentalTable.WriteInt32(entries.Length);

            foreach (var entry in entries)
            {
                _experimentalTable.WriteInt32(entry.ApiOffset);
                _experimentalTable.WriteInt32(entry.AssemblyOffset);
                _experimentalTable.WriteInt32(entry.DiagnosticIdOffset);
                _experimentalTable.WriteInt32(entry.UrlFormatOffset);
            }
        }

        private void WriteExtensionMethods()
        {
            var entries = _builder._apis
                .Where(a => a.Extensions is not null)
                .SelectMany(a => a.Extensions!, (a, e) => (
                    ExtensionMethodGuid: e.Fingerprint,
                    ExtendedTypeOffset: _apiOffsetByIndex[a.Index],
                    ExtensionMethodOffset: _apiOffsetByIndex[e.Method.Index]))
                .OrderBy(t => t.ExtendedTypeOffset)
                .ThenBy(t => t.ExtensionMethodOffset)
                .ToArray();

            _extensionMethodsTable.WriteInt32(entries.Length);

            foreach (var entry in entries)
            {
                _extensionMethodsTable.WriteGuid(entry.ExtensionMethodGuid);
                _extensionMethodsTable.WriteInt32(entry.ExtendedTypeOffset);
                _extensionMethodsTable.WriteInt32(entry.ExtensionMethodOffset);
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
                    _frameworkTable.WriteInt32(_assemblyOffsetByIndex[assemblyId]);
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
                    _packageTable.WriteInt32(_assemblyOffsetByIndex[assemblyId]);
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
                    _assemblyTable.WriteInt32(_apiOffsetByIndex[apiId]);
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

                set
                {
                    _stream.Position = value;
                }
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

            public void WriteBoolean(bool value)
            {
                WriteByte(value ? 1 : 0);
            }

            public void WriteSingle(float value)
            {
                _writer.Write(value);
            }

            public void WriteGuid(Guid guid)
            {
                var span = (Span<byte>)stackalloc byte[16];
                var success = guid.TryWriteBytes(span); 
                Debug.Assert(success);
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
                _stream.ReadAtLeast(byteSpan, byteSpan.Length);
                return BinaryPrimitives.ReadInt32LittleEndian(byteSpan);
            }

            public void CopyTo(Stream destination)
            {
                _writer.Flush();
                var position = _stream.Position;
                _stream.Position = 0;
                _stream.CopyTo(destination);
                _stream.Position = position;
            }
        }
    }
}