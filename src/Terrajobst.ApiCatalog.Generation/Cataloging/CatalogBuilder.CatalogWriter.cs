using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace Terrajobst.ApiCatalog;

public sealed partial class CatalogBuilder
{
    public sealed class CatalogWriter
    {
        private readonly CatalogBuilder _builder;

        private readonly BlobHeap _blobHeap = new();
        private readonly StringHeap _stringHeap = new();
        private readonly PlatformTable _platformTable = new();
        private readonly FrameworkTable _frameworkTable = new();
        private readonly PackageTable _packageTable = new();
        private readonly AssemblyTable _assemblyTable = new();
        private readonly ApiTable _apiTable = new();
        private readonly RootApiTable _rootApiTable = new();
        private readonly ExtensionMethodTable _extensionMethodTable = new();
        private readonly ObsoletionTable _obsoletionTable = new();
        private readonly PlatformSupportTable _platformSupportTable = new();
        private readonly PreviewRequirementTable _previewRequirementTable = new();
        private readonly ExperimentalTable _experimentalTable = new();

        private readonly Dictionary<IntermediateFramework, FrameworkOffset> _frameworkOffsets = new();
        private readonly Dictionary<IntermediatePackage, PackageOffset> _packageOffsets = new();
        private readonly AssemblyOffset[] _assemblyOffsets;
        private readonly Dictionary<IntermediateUsageSource, UsageSourceOffset> _usageSourceOffsets = new();
        private readonly ApiOffset[] _apiOffsets;

        public CatalogWriter(CatalogBuilder builder)
        {
            _builder = builder;
            _assemblyOffsets = new AssemblyOffset[_builder._assemblies.Count];
            _apiOffsets = new ApiOffset[_builder._apis.Count];
        }

        private AssemblyOffset GetAssemblyOffset(IntermediaAssembly assembly)
        {
            return _assemblyOffsets[assembly.Index];
        }

        private void SetAssemblyOffset(IntermediaAssembly assembly, AssemblyOffset offset)
        {
            _assemblyOffsets[assembly.Index] = offset;
        }

        private ApiOffset GetApiOffset(IntermediaApi api)
        {
            return _apiOffsets[api.Index];
        }

        private void SetApiOffset(IntermediaApi api, ApiOffset offset)
        {
            _apiOffsets[api.Index] = offset;
        }

        public void Write(Stream stream)
        {
            var heapsAndTables = new HeapOrTable[] {
                _stringHeap,
                _blobHeap,
                _platformTable,
                _frameworkTable,
                _packageTable,
                _assemblyTable,
                _apiTable,
                _rootApiTable,
                _extensionMethodTable,
                _obsoletionTable,
                _platformSupportTable,
                _previewRequirementTable,
                _experimentalTable
            };

            WritePlatforms();
            WriteFrameworks();
            WritePackages();
            WriteAssemblies();
            _blobHeap.PatchAssemblyOffsets(_builder._assemblies, _assemblyOffsets);
            WriteApis();
            WriteRootApis();
            WriteExtensionMethods();
            WriteObsoletions();
            WritePlatformSupports();
            WritePreviewRequirements();
            WriteExperimentals();

            _blobHeap.PatchSyntaxes(_stringHeap, _builder._apiByFingerprint);
            _blobHeap.PatchApiOffsets(_builder._apis, _apiOffsets);

            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                // Magic value

                writer.Write(ApiCatalogSchema.MagicNumber);

                // Version

                writer.Write(ApiCatalogSchema.FormatVersion);

                // Table Sizes
                writer.Write(heapsAndTables.Length);

                foreach (var heapOrTable in heapsAndTables)
                {
                    var length = heapOrTable.Memory.GetLength();
                    writer.Write(length);
                }
            }

            using (var deflateStream = new DeflateStream(stream, CompressionLevel.Optimal, leaveOpen: true))
            {
                foreach (var heapOrTable in heapsAndTables)
                    heapOrTable.Memory.CopyTo(deflateStream);
            }
        }

        private void WritePlatforms()
        {
            Console.WriteLine("Writing platforms...");

            var platforms = _builder._platformNames;

            foreach (var platform in platforms)
            {
                var name = _stringHeap.Store(platform);
                _platformTable.WriteRow(name);
            }
        }

        private void WriteFrameworks()
        {
            Console.WriteLine("Writing frameworks...");

            var frameworks = _builder._frameworkByName.Values;

            foreach (var framework in frameworks)
            {
                var name = _stringHeap.Store(framework.Name);
                var assemblies = _blobHeap.StoreAssemblies(framework.Assemblies);
                var assemblyPacks = _blobHeap.StoreAssemblyPacks(framework.AssemblyPacks, _stringHeap);
                var assemblyProfiles = _blobHeap.StoreAssemblyProfiles(framework.AssemblyProfiles, _stringHeap);

                var offset = _frameworkTable.WriteRow(name, assemblies, assemblyPacks, assemblyProfiles);
                _frameworkOffsets.Add(framework, offset);
            }
        }

        private void WritePackages()
        {
            Console.WriteLine("Writing packages...");

            var packages = _builder._packageByFingerprint.Values;

            foreach (var package in packages)
            {
                var name = _stringHeap.Store(package.Name);
                var version = _stringHeap.Store(package.Version);
                var assemblies = _blobHeap.StoreAssemblies(package.Assemblies, _frameworkOffsets);

                var rowOffset = _packageTable.WriteRow(name, version, assemblies);
                _packageOffsets.Add(package, rowOffset);
            }
        }

        private void WriteAssemblies()
        {
            Console.WriteLine("Writing assemblies...");

            var assemblies = _builder._assemblies;

            foreach (var assembly in assemblies)
            {
                var fingerprint = assembly.Fingerprint;
                var name = _stringHeap.Store(assembly.Name);
                var publicKeyToken = _stringHeap.Store(assembly.PublicKeyToken);
                var version = _stringHeap.Store(assembly.Version);
                var rootApis = _blobHeap.StoreApis(assembly.RootApis);
                var frameworks = _blobHeap.StoreFrameworks(assembly.Frameworks, _frameworkOffsets);
                var packages = _blobHeap.StorePackages(assembly.Packages, _packageOffsets, _frameworkOffsets);

                var rowOffset = _assemblyTable.WriteRow(fingerprint, name, publicKeyToken, version, rootApis, frameworks, packages);
                SetAssemblyOffset(assembly, rowOffset);
            }
        }

        private void WriteApis()
        {
            Console.WriteLine("Writing APIs...");

            WriteApis(_builder._rootApis);
        }

        private void WriteApis(IReadOnlyList<IntermediaApi> apis)
        {

            Console.WriteLine($"  Writing {apis.Count} APIs...");
            var chunkSize = 10_000;
            var apiIndex = 0;
            foreach (var api in apis)
            {
                var intermediateChildren = (IReadOnlyList<IntermediaApi>?) api.Children ?? Array.Empty<IntermediaApi>();

                var fingerprint = api.Fingerprint;
                var kind = (byte)api.Kind;
                var parent = api.Parent is null ? ApiOffset.Nil : GetApiOffset(api.Parent); // NOTE: This is safe because we know the parent was already written.
                var name = _stringHeap.Store(api.Name);
                var children = _blobHeap.StoreApis(intermediateChildren);
                var declarations = _blobHeap.StoreDeclarations(api, _builder._assemblies, _assemblyOffsets, _stringHeap, _builder._apiByFingerprint);

                var rowOffset = _apiTable.WriteRow(fingerprint, kind, parent, name, children, declarations);
                SetApiOffset(api, rowOffset);

                if (api.Children is not null)
                    WriteApis(api.Children);
                
                if ((apiIndex + 1) % chunkSize == 0)
                {
                    Console.WriteLine($"    API chunk {(apiIndex + 1) / chunkSize}: {apiIndex + 1:N0}-{Math.Min(apiIndex + chunkSize, apis.Count):N0} of {apis.Count:N0}");
                    GC.Collect(2, GCCollectionMode.Forced, blocking: false, compacting: false);
                }

                apiIndex++;
            }
        }

        private void WriteRootApis()
        {
            Console.WriteLine("Writing root APIs...");

            foreach (var entry in _builder._rootApis)
            {
                var api = GetApiOffset(entry);
                _rootApiTable.WriteRow(api);
            }
        }

        private void WriteExtensionMethods()
        {
            Console.WriteLine("Writing existing methods...");

            var entries = _builder._apis
                .Where(a => a.Extensions is not null)
                .SelectMany(type => type.Extensions!, (type, extension) => (
                    ExtensionMethodGuid: extension.Fingerprint,
                    ExtendedType: GetApiOffset(type),
                    ExtensionMethod: GetApiOffset(extension.Method)))
                .OrderBy(t => t.ExtendedType.Value)
                .ThenBy(t => t.ExtensionMethod.Value);

            foreach (var entry in entries)
            {
                var extensionMethodGuid = entry.ExtensionMethodGuid;
                var extendedType = entry.ExtendedType;
                var extensionMethod = entry.ExtensionMethod;
                _extensionMethodTable.WriteRow(extensionMethodGuid, extendedType, extensionMethod);
            }
        }

        private void WriteObsoletions()
        {
            Console.WriteLine("Writing obsoletions...");

            var entries = _builder._assemblies
                .SelectMany(a => a.Declarations.Values)
                .Where(d => d.Obsoletion is not null)
                .Select(d => (
                    Api: GetApiOffset(d.Api),
                    Assembly: GetAssemblyOffset(d.Assembly),
                    Message: _stringHeap.Store(d.Obsoletion!.Message),
                    d.Obsoletion.IsError,
                    DiagnosticId: _stringHeap.Store(d.Obsoletion.DiagnosticId),
                    UrlFormat: _stringHeap.Store(d.Obsoletion.UrlFormat)
                ))
                .OrderBy(t => t.Api.Value)
                .ThenBy(t => t.Assembly.Value);

            foreach (var entry in entries)
            {
                var api = entry.Api;
                var assembly = entry.Assembly;
                var message = entry.Message;
                var isError = entry.IsError;
                var diagnosticId = entry.DiagnosticId;
                var urlFormat = entry.UrlFormat;

                _obsoletionTable.WriteRow(api, assembly, message, isError, diagnosticId, urlFormat);
            }
        }

        private void WritePlatformSupports()
        {
            Console.WriteLine("Writing platform support...");

            var assemblyPlatformSupport = _builder._assemblies
                .Select(a => (Assembly: a, Api: (IntermediaApi?)null, a.PlatformSupport));

            var apiPlatformSupport = _builder._assemblies
                .SelectMany(a => a.Declarations, (a, d) => (Assembly: a, Api: (IntermediaApi?)d.Key, d.Value.PlatformSupport));

            var entries = assemblyPlatformSupport
                .Concat(apiPlatformSupport)
                .Where(e => e.PlatformSupport is not null)
                .Select(e => (
                    Api: e.Api is null ? ApiOffset.Nil : GetApiOffset(e.Api),
                    Assembly: GetAssemblyOffset(e.Assembly),
                    Platforms: _blobHeap.StorePlatformSupport(e.PlatformSupport!, _stringHeap)
                ))
                .OrderBy(t => t.Api.Value)
                .ThenBy(t => t.Assembly.Value);

            foreach (var entry in entries)
            {
                var api = entry.Api;
                var assembly = entry.Assembly;
                var platforms = entry.Platforms;
                _platformSupportTable.WriteRow(api, assembly, platforms);
            }
        }

        private void WritePreviewRequirements()
        {
            Console.WriteLine("Writing preview requirements...");

            var assemblyPreviewRequirements = _builder._assemblies
                .Select(a => (Assembly: a, Api: (IntermediaApi?)null, a.PreviewRequirement));

            var apiPreviewRequirements = _builder._assemblies
                .SelectMany(a => a.Declarations, (a, d) => (Assembly: a, Api: (IntermediaApi?)d.Key, d.Value.PreviewRequirement));

            var entries = assemblyPreviewRequirements
                .Concat(apiPreviewRequirements)
                .Where(e => e.PreviewRequirement is not null)
                .Select(e => (
                    Api: e.Api is null ? ApiOffset.Nil : GetApiOffset(e.Api),
                    Assembly: GetAssemblyOffset(e.Assembly),
                    Message: _stringHeap.Store(e.PreviewRequirement!.Message),
                    Url: _stringHeap.Store(e.PreviewRequirement.Url)
                ))
                .OrderBy(t => t.Api.Value)
                .ThenBy(t => t.Assembly.Value);

            foreach (var entry in entries)
            {
                var api = entry.Api;
                var assembly = entry.Assembly;
                var message = entry.Message;
                var url = entry.Url;

                _previewRequirementTable.WriteRow(api, assembly, message, url);
            }
        }

        private void WriteExperimentals()
        {
            Console.WriteLine("Writing experimentals...");

            var assemblyExperimental = _builder._assemblies
                .Select(a => (Assembly: a, Api: (IntermediaApi?)null, a.Experimental));

            var apiExperimental = _builder._assemblies
                .SelectMany(a => a.Declarations, (a, d) => (Assembly: a, Api: (IntermediaApi?)d.Key, d.Value.Experimental));

            var entries = assemblyExperimental
                .Concat(apiExperimental)
                .Where(e => e.Experimental is not null)
                .Select(e => (
                    Api: e.Api is null ? ApiOffset.Nil : GetApiOffset(e.Api),
                    Assembly: GetAssemblyOffset(e.Assembly),
                    DiagnosticId: _stringHeap.Store(e.Experimental!.DiagnosticId),
                    UrlFormat: _stringHeap.Store(e.Experimental.UrlFormat)
                ))
                .OrderBy(t => t.Api.Value)
                .ThenBy(t => t.Assembly.Value);

            foreach (var entry in entries)
            {
                var api = entry.Api;
                var assembly = entry.Assembly;
                var diagnosticId = entry.DiagnosticId;
                var urlFormat = entry.UrlFormat;

                _experimentalTable.WriteRow(api, assembly, diagnosticId, urlFormat);
            }
        }

        private class Memory
        {
            private readonly MemoryStream _data = new();
            private readonly BinaryWriter _writer;

            public Memory()
            {
                _writer = new BinaryWriter(_data, Encoding.UTF8, leaveOpen: true);
            }

            public void Clear()
            {
                _writer.Flush();
                _data.SetLength(0);
                Debug.Assert(_data.Position == 0);
            }

            public void Seek(int offset)
            {
                _writer.Flush();
                _data.Position = offset;
            }

            public int GetLength()
            {
                _writer.Flush();
                return (int)_data.Length;
            }

            public int PeekInt32()
            {
                _writer.Flush();

                var location = _data.Position;
                var span = (Span<byte>) stackalloc byte[4];
                _data.ReadExactly(span);
                _data.Position = location;
                return BinaryPrimitives.ReadInt32LittleEndian(span);
            }

            public ArraySegment<byte> GetData()
            {
                _writer.Flush();
                var success = _data.TryGetBuffer(out var buffer);
                Debug.Assert(success);
                return buffer;
            }

            public void CopyTo(Stream destination)
            {
                _writer.Flush();
                _data.Position = 0;
                _data.CopyTo(destination);
            }

            public void WriteStringOffset(StringOffset offset)
            {
                WriteInt32(offset.Value);
            }

            public void WriteBlobOffset(BlobOffset offset)
            {
                WriteInt32(offset.Value);
            }

            public void WriteFrameworkOffset(FrameworkOffset offset)
            {
                WriteInt32(offset.Value);
            }

            public void WritePackageOffset(PackageOffset offset)
            {
                WriteInt32(offset.Value);
            }

            public void WriteAssemblyOffset(AssemblyOffset offset)
            {
                WriteInt32(offset.Value);
            }

            public void WriteApiOffset(ApiOffset offset)
            {
                WriteInt32(offset.Value);
            }

            public void WriteString(string value)
            {
                // NOTE: We don't do _writer.Write(value) because that writes a 7-bit encoded int and I'm too lazy to
                //       implement that on the reader side.
                //
                // Doing so would shave off ~3 MB.

                var length = value.Length;
                var bytes = Encoding.UTF8.GetBytes(value);
                WriteInt32(length);
                _writer.Write(bytes);
            }

            public void WriteBool(bool value)
            {
                var b = value ? (byte)1 : (byte)0;
                WriteByte(b);
            }

            public void WriteByte(byte value)
            {
                _writer.Write(value);
            }

            public void WriteGuid(Guid guid)
            {
                var bytes = guid.ToByteArray();
                WriteBytes(bytes);
            }

            public void WriteInt32(int value)
            {
                _writer.Write(value);
            }

            public void WriteBytes(ReadOnlySpan<byte> bytes)
            {
                _writer.Write(bytes);
            }
        }

        private abstract class HeapOrTable
        {
            public Memory Memory { get; } = new();
        }

        private sealed class DeduplicatedMemory : Memory
        {
            private readonly Memory _underlyingMemory;
            private readonly Dictionary<int, (int Start, int Length)> _existingBlobs = new();

            public DeduplicatedMemory(Memory underlyingMemory)
            {
                _underlyingMemory = underlyingMemory;
            }

            public BlobOffset Commit()
            {
                var data = GetData();
                var offset = _underlyingMemory.GetLength();

                (var added, offset) = AddBlob(offset, data);
                if (added)
                    _underlyingMemory.WriteBytes(data);

                Clear();
                return new BlobOffset(offset);
            }

            // This algorithm is taken from System.Reflection.Metadata.
            //
            // The idea is to use an int-based hash for blobs and do double hashing to resolve conflicts.

            private (bool Added, int Offset) AddBlob(int offset, ArraySegment<byte> newBlob)
            {
                var dictionaryKey = Hash.GetFNVHashCode(newBlob);
                while (true)
                {
                    // First lets see whether we can find the bucket for the hash

                    if (!_existingBlobs.TryGetValue(dictionaryKey, out var entry))
                    {
                        // No value for that key. That means the blob wasn't added yet.
                        entry = (offset, newBlob.Count);
                        _existingBlobs.Add(dictionaryKey, entry);
                        return (true, offset);
                    }

                    // We found an for the key. However it could be taken by another blob.
                    // Let's compare contents:

                    var existingBlob = _underlyingMemory.GetData().Slice(entry.Start, entry.Length);
                    if (existingBlob.SequenceEqual(newBlob))
                    {
                        // We have seen the blob. Nice!
                        return (false, entry.Start);
                    }

                    // We found the entry for a different blob. Keep looking.

                    dictionaryKey = GetNextDictionaryKey(dictionaryKey);
                }
            }

            private static int GetNextDictionaryKey(int dictionaryKey) => (int)((uint)dictionaryKey * 747796405 + 2891336453);

            internal static class Hash
            {
                private const int FnvOffsetBias = unchecked((int)2166136261);
                private const int FnvPrime = 16777619;

                public static int GetFNVHashCode(ReadOnlySpan<byte> data)
                {
                    var hashCode = FnvOffsetBias;

                    for (var i = 0; i < data.Length; i++)
                        hashCode = unchecked((hashCode ^ data[i]) * FnvPrime);

                    return hashCode;
                }
            }
        }

        private sealed class BlobHeap : HeapOrTable
        {
            private const int PatchChunkSize = 250_000;
            private const int SyntaxOffsetCacheLimit = 200_000;

            private readonly List<BlobOffset> _assemblyPatchups = new();
            private readonly List<BlobOffset> _apiPatchups = new();
            private readonly List<(BlobOffset, IntermediateDeclaration)> _syntaxPatchups = new();
            private readonly Dictionary<string, BlobOffset> _syntaxOffsets = new(StringComparer.Ordinal);

            public BlobHeap()
            {
                DeduplicatedMemory = new DeduplicatedMemory(Memory);
            }

            private DeduplicatedMemory DeduplicatedMemory { get; }

            private void WriteAssemblyPatchup(IntermediaAssembly assembly)
            {
                var offset = SeekEnd();
                _assemblyPatchups.Add(offset);
                Memory.WriteInt32(assembly.Index);
            }

            private void WriteApiPatchup(IntermediaApi api)
            {
                var offset = SeekEnd();
                _apiPatchups.Add(offset);
                Memory.WriteInt32(api.Index);
            }

            private void WriteSyntaxPatchup(IntermediateDeclaration declaration)
            {
                var offset = SeekEnd();
                _syntaxPatchups.Add((offset, declaration));
                Memory.WriteInt32(-1);
            }

            private BlobOffset SeekEnd()
            {
                var end = Memory.GetLength();
                Memory.Seek(end);
                return new BlobOffset(end);
            }

            public BlobOffset StoreAssemblies(IReadOnlyList<IntermediaAssembly> assemblies)
            {
                if (assemblies.Count == 0)
                    return BlobOffset.Nil;

                var result = SeekEnd();

                Memory.WriteInt32(assemblies.Count);
                foreach (var assembly in assemblies)
                    WriteAssemblyPatchup(assembly);

                return result;
            }

            public BlobOffset StoreAssemblies(IReadOnlyList<(IntermediateFramework, IntermediaAssembly)> assemblies,
                                              IReadOnlyDictionary<IntermediateFramework, FrameworkOffset> frameworkOffsets)
            {
                if (assemblies.Count == 0)
                    return BlobOffset.Nil;

                var result = SeekEnd();

                Memory.WriteInt32(assemblies.Count);
                foreach (var (framework, assembly) in assemblies)
                {
                    Memory.WriteFrameworkOffset(frameworkOffsets[framework]);
                    WriteAssemblyPatchup(assembly);
                }

                return result;
            }

            public BlobOffset StoreAssemblyPacks(IReadOnlyList<(string Pack, IReadOnlyList<IntermediaAssembly> Assemblies)> assemblyPacks,
                                                 StringHeap stringHeap)
            {
                if (assemblyPacks.Count == 0)
                    return BlobOffset.Nil;

                var assemblyArrayOffsets = new BlobOffset[assemblyPacks.Count];
                for (var i = 0; i < assemblyPacks.Count; i++)
                    assemblyArrayOffsets[i] = StoreAssemblies(assemblyPacks[i].Assemblies);

                var result = SeekEnd();

                Memory.WriteInt32(assemblyPacks.Count);

                for (var i = 0; i < assemblyPacks.Count; i++)
                {
                    var packName = stringHeap.Store(assemblyPacks[i].Pack);
                    var assemblies = assemblyArrayOffsets[i];

                    Memory.WriteStringOffset(packName);
                    Memory.WriteBlobOffset(assemblies);
                }

                return result;
            }

            public BlobOffset StoreAssemblyProfiles(IReadOnlyList<(string Profile, IReadOnlyList<IntermediaAssembly> Assemblies)> assemblyProfiles,
                                                    StringHeap stringHeap)
            {
                if (assemblyProfiles.Count == 0)
                    return BlobOffset.Nil;

                var assemblyArrayOffsets = new BlobOffset[assemblyProfiles.Count];
                for (var i = 0; i < assemblyProfiles.Count; i++)
                    assemblyArrayOffsets[i] = StoreAssemblies(assemblyProfiles[i].Assemblies);

                var result = SeekEnd();

                Memory.WriteInt32(assemblyProfiles.Count);

                for (var i = 0; i < assemblyProfiles.Count; i++)
                {
                    var packName = stringHeap.Store(assemblyProfiles[i].Profile);
                    var assemblies = assemblyArrayOffsets[i];

                    Memory.WriteStringOffset(packName);
                    Memory.WriteBlobOffset(assemblies);
                }

                return result;
            }

            public BlobOffset StoreFrameworks(IReadOnlyList<IntermediateFramework> frameworks,
                                              IReadOnlyDictionary<IntermediateFramework, FrameworkOffset> frameworkOffsets)
            {
                if (frameworks.Count == 0)
                    return BlobOffset.Nil;

                var result = SeekEnd();

                Memory.WriteInt32(frameworks.Count);
                foreach (var framework in frameworks)
                    Memory.WriteFrameworkOffset(frameworkOffsets[framework]);

                return result;
            }

            public BlobOffset StorePackages(IReadOnlyList<(IntermediatePackage, IntermediateFramework)> packages,
                                            IReadOnlyDictionary<IntermediatePackage, PackageOffset> packageOffsets,
                                            IReadOnlyDictionary<IntermediateFramework, FrameworkOffset> frameworkOffsets)
            {
                if (packages.Count == 0)
                    return BlobOffset.Nil;

                var result = SeekEnd();

                Memory.WriteInt32(packages.Count);
                foreach (var (package, framework) in packages)
                {
                    Memory.WritePackageOffset(packageOffsets[package]);
                    Memory.WriteFrameworkOffset(frameworkOffsets[framework]);
                }

                return result;
            }

            public BlobOffset StoreApis(IReadOnlyList<IntermediaApi> apis)
            {
                if (apis.Count == 0)
                    return BlobOffset.Nil;

                var result = SeekEnd();

                Memory.WriteInt32(apis.Count);
                foreach (var api in apis)
                    WriteApiPatchup(api);

                return result;
            }

            public BlobOffset StoreApis(IEnumerable<IntermediaApi> apis)
            {
                var count = 0;
                foreach (var _ in apis)
                    count++;

                if (count == 0)
                    return BlobOffset.Nil;

                var result = SeekEnd();

                Memory.WriteInt32(count);
                foreach (var api in apis)
                    WriteApiPatchup(api);

                return result;
            }

            public BlobOffset StoreDeclarations(IReadOnlyList<IntermediateDeclaration> declarations,
                                                IReadOnlyDictionary<IntermediaAssembly, AssemblyOffset> assemblyOffsets)
            {
                if (declarations.Count == 0)
                    return BlobOffset.Nil;

                var result = SeekEnd();

                Memory.WriteInt32(declarations.Count);
                foreach (var declaration in declarations)
                {
                    Memory.WriteAssemblyOffset(assemblyOffsets[declaration.Assembly]);
                    WriteSyntaxPatchup(declaration);
                }

                return result;
            }

            public BlobOffset StoreDeclarations(IntermediaApi api,
                                                IReadOnlyList<IntermediaAssembly> assemblies,
                                                IReadOnlyList<AssemblyOffset> assemblyOffsets,
                                                StringHeap stringHeap,
                                                IReadOnlyDictionary<Guid, IntermediaApi> apiByFingerprint)
            {
                var count = 0;
                foreach (var assembly in assemblies)
                {
                    if (assembly.Declarations.ContainsKey(api))
                        count++;
                }

                if (count == 0)
                    return BlobOffset.Nil;

                var result = SeekEnd();

                Memory.WriteInt32(count);
                foreach (var assembly in assemblies)
                {
                    if (assembly.Declarations.TryGetValue(api, out var declaration))
                    {
                        Memory.WriteAssemblyOffset(assemblyOffsets[assembly.Index]);
                        var syntaxOffset = GetOrStoreSyntaxOffset(declaration.Syntax, stringHeap, apiByFingerprint);
                        Memory.WriteBlobOffset(syntaxOffset);
                    }
                }

                return result;
            }

            public BlobOffset StorePlatformSupport(IReadOnlyList<IntermediatePlatformSupport> platforms,
                                                   StringHeap stringHeap)
            {
                if (platforms.Count == 0)
                    return BlobOffset.Nil;

                DeduplicatedMemory.WriteInt32(platforms.Count);
                foreach (var platformSupport in platforms
                             .OrderBy(p => p.PlatformName, StringComparer.OrdinalIgnoreCase)
                             .ThenBy(p => p.IsSupported))
                {
                    DeduplicatedMemory.WriteStringOffset(stringHeap.Store(platformSupport.PlatformName));
                    DeduplicatedMemory.WriteBool(platformSupport.IsSupported);
                }

                return DeduplicatedMemory.Commit();
            }

            private BlobOffset StoreSyntax(string syntax,
                                           StringHeap stringHeap,
                                           IReadOnlyDictionary<Guid, IntermediaApi> apiByFingerprint)
            {
                var markup = Markup.FromXml(syntax);

                var result = SeekEnd();
                Memory.WriteInt32(markup.Tokens.Length);

                foreach (var token in markup.Tokens)
                {
                    var kind = (byte)token.Kind;
                    var text = stringHeap.Store(token.Text);
                    var hasIntrinsicText = token.Kind.GetTokenText() is not null;

                    Memory.WriteByte(kind);
                    if (!hasIntrinsicText)
                        Memory.WriteStringOffset(text);

                    if (token.Kind == MarkupTokenKind.ReferenceToken)
                    {
                        if (token.Reference is not null && apiByFingerprint.TryGetValue(token.Reference.Value, out var api))
                            WriteApiPatchup(api);
                        else
                            Memory.WriteInt32(-1);
                    }
                }

                return result;
            }

            private BlobOffset GetOrStoreSyntaxOffset(string syntax,
                                                      StringHeap stringHeap,
                                                      IReadOnlyDictionary<Guid, IntermediaApi> apiByFingerprint)
            {
                if (_syntaxOffsets.TryGetValue(syntax, out var syntaxOffset))
                    return syntaxOffset;

                if (_syntaxOffsets.Count >= SyntaxOffsetCacheLimit)
                    _syntaxOffsets.Clear();

                syntaxOffset = StoreSyntax(syntax, stringHeap, apiByFingerprint);
                _syntaxOffsets.Add(syntax, syntaxOffset);
                return syntaxOffset;
            }

            public void PatchSyntaxes(StringHeap stringHeap,
                                      IReadOnlyDictionary<Guid, IntermediaApi> apiByFingerprint)
            {
                Console.WriteLine("Patching syntaxes...");

                var total = _syntaxPatchups.Count;
                var chunkStart = 0;
                var syntaxOffsets = new Dictionary<string, BlobOffset>(StringComparer.Ordinal);

                for (var i = 0; i < _syntaxPatchups.Count; i++)
                {
                    if (i % PatchChunkSize == 0)
                    {
                        if (i > 0)
                        {
                            // Drop references from the previous chunk so declarations can be collected sooner.
                            for (var j = chunkStart; j < i; j++)
                                _syntaxPatchups[j] = default;

                            syntaxOffsets.Clear();
                            syntaxOffsets.TrimExcess();
                            syntaxOffsets = new Dictionary<string, BlobOffset>(StringComparer.Ordinal);
                            chunkStart = i;
                            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: false);
                        }

                        var chunk = i / PatchChunkSize + 1;
                        var chunkEnd = Math.Min(i + PatchChunkSize, total);
                        Console.WriteLine($"  Syntax chunk {chunk}: {i + 1:N0}-{chunkEnd:N0} of {total:N0}");
                    }

                    var (patchOffset, declaration) = _syntaxPatchups[i];

                    if (!syntaxOffsets.TryGetValue(declaration.Syntax, out var syntaxOffset))
                    {
                        if (syntaxOffsets.Count >= SyntaxOffsetCacheLimit)
                            syntaxOffsets.Clear();

                        syntaxOffset = StoreSyntax(declaration.Syntax, stringHeap, apiByFingerprint);
                        syntaxOffsets.Add(declaration.Syntax, syntaxOffset);
                    }

                    Memory.Seek(patchOffset.Value);
                    Memory.WriteInt32(syntaxOffset.Value);
                }

                Memory.Seek(Memory.GetLength());
                for (var j = chunkStart; j < _syntaxPatchups.Count; j++)
                    _syntaxPatchups[j] = default;

                syntaxOffsets.Clear();
                syntaxOffsets.TrimExcess();
                _syntaxPatchups.Clear();
                _syntaxPatchups.TrimExcess();
            }

            public void PatchApiOffsets(IReadOnlyList<IntermediaApi> apis,
                                        IReadOnlyList<ApiOffset> apiOffsets)
            {
                Console.WriteLine("Patching API offsets...");
                var total = _apiPatchups.Count;
                var chunkStart = 0;

                for (var i = 0; i < total; i++)
                {
                    if (i % PatchChunkSize == 0)
                    {
                        if (i > 0)
                        {
                            for (var j = chunkStart; j < i; j++)
                                _apiPatchups[j] = default;

                            chunkStart = i;
                            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: false);
                        }

                        var chunk = i / PatchChunkSize + 1;
                        var chunkEnd = Math.Min(i + PatchChunkSize, total);
                        Console.WriteLine($"  API chunk {chunk}: {i + 1:N0}-{chunkEnd:N0} of {total:N0}");
                    }

                    var patchOffset = _apiPatchups[i];
                    Memory.Seek(patchOffset.Value);
                    var apiIndex = Memory.PeekInt32();
                    var apiOffset = apiOffsets[apiIndex];
                    Memory.WriteApiOffset(apiOffset);
                }

                Memory.Seek(Memory.GetLength());
                for (var j = chunkStart; j < total; j++)
                    _apiPatchups[j] = default;

                _apiPatchups.Clear();
                _apiPatchups.TrimExcess();
            }

            public void PatchAssemblyOffsets(IReadOnlyList<IntermediaAssembly> assemblies,
                                             IReadOnlyList<AssemblyOffset> assemblyOffsets)
            {
                Console.WriteLine("Patching assembly offsets...");
                var total = _assemblyPatchups.Count;
                var chunkStart = 0;

                for (var i = 0; i < total; i++)
                {
                    if (i % PatchChunkSize == 0)
                    {
                        if (i > 0)
                        {
                            for (var j = chunkStart; j < i; j++)
                                _assemblyPatchups[j] = default;

                            chunkStart = i;
                            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: false);
                        }

                        var chunk = i / PatchChunkSize + 1;
                        var chunkEnd = Math.Min(i + PatchChunkSize, total);
                        Console.WriteLine($"  Assembly chunk {chunk}: {i + 1:N0}-{chunkEnd:N0} of {total:N0}");
                    }

                    var patchOffset = _assemblyPatchups[i];
                    Memory.Seek(patchOffset.Value);
                    var assemblyIndex = Memory.PeekInt32();
                    var assemblyOffset = assemblyOffsets[assemblyIndex];
                    Memory.WriteAssemblyOffset(assemblyOffset);
                }

                Memory.Seek(Memory.GetLength());
                for (var j = chunkStart; j < total; j++)
                    _assemblyPatchups[j] = default;

                _assemblyPatchups.Clear();
                _assemblyPatchups.TrimExcess();
            }
        }

        private sealed class StringHeap : HeapOrTable
        {
            private readonly Dictionary<string, StringOffset> _stringOffsets = new (StringComparer.Ordinal);

            public StringOffset Store(string text)
            {
                if (!_stringOffsets.TryGetValue(text, out var offset))
                {
                    offset = new StringOffset(Memory.GetLength());
                    Memory.WriteString(text);
                    _stringOffsets.Add(text, offset);
                }

                return offset;
            }
        }

        private sealed class PlatformTable : HeapOrTable
        {
            public void WriteRow(StringOffset name)
            {
                Memory.WriteStringOffset(name);
            }
        }

        private sealed class FrameworkTable : HeapOrTable
        {
            public FrameworkOffset WriteRow(StringOffset name,
                                            BlobOffset assemblies,
                                            BlobOffset assemblyPacks,
                                            BlobOffset assemblyProfiles)
            {
                var offset = new FrameworkOffset(Memory.GetLength());

                Memory.WriteStringOffset(name);
                Memory.WriteBlobOffset(assemblies);
                Memory.WriteBlobOffset(assemblyPacks);
                Memory.WriteBlobOffset(assemblyProfiles);

                return offset;
            }
        }

        private sealed class PackageTable : HeapOrTable
        {
            public PackageOffset WriteRow(StringOffset packageName,
                                          StringOffset packageVersion,
                                          BlobOffset assemblies)
            {
                var offset = new PackageOffset(Memory.GetLength());

                Memory.WriteStringOffset(packageName);
                Memory.WriteStringOffset(packageVersion);
                Memory.WriteBlobOffset(assemblies);

                return offset;
            }
        }

        private sealed class AssemblyTable : HeapOrTable
        {
            public AssemblyOffset WriteRow(Guid fingerprint,
                                           StringOffset name,
                                           StringOffset publicKeyToken,
                                           StringOffset version,
                                           BlobOffset rootApis,
                                           BlobOffset frameworks,
                                           BlobOffset packages)
            {
                var offset = new AssemblyOffset(Memory.GetLength());

                Memory.WriteGuid(fingerprint);
                Memory.WriteStringOffset(name);
                Memory.WriteStringOffset(publicKeyToken);
                Memory.WriteStringOffset(version);
                Memory.WriteBlobOffset(rootApis);
                Memory.WriteBlobOffset(frameworks);
                Memory.WriteBlobOffset(packages);

                return offset;
            }
        }

        private sealed class ApiTable : HeapOrTable
        {
            public ApiOffset WriteRow(Guid fingerprint,
                                      byte kind,
                                      ApiOffset parent,
                                      StringOffset name,
                                      BlobOffset children,
                                      BlobOffset declarations)
            {
                var offset = new ApiOffset(Memory.GetLength());

                Memory.WriteGuid(fingerprint);
                Memory.WriteByte(kind);
                Memory.WriteApiOffset(parent);
                Memory.WriteStringOffset(name);
                Memory.WriteBlobOffset(children);
                Memory.WriteBlobOffset(declarations);

                return offset;
            }
        }

        private sealed class RootApiTable : HeapOrTable
        {
            public void WriteRow(ApiOffset api)
            {
                Memory.WriteApiOffset(api);
            }
        }

        private sealed class ObsoletionTable : HeapOrTable
        {
            public void WriteRow(ApiOffset api,
                                 AssemblyOffset assembly,
                                 StringOffset message,
                                 bool isError,
                                 StringOffset diagnosticId,
                                 StringOffset urlFormat)
            {
                Memory.WriteApiOffset(api);
                Memory.WriteAssemblyOffset(assembly);
                Memory.WriteStringOffset(message);
                Memory.WriteBool(isError);
                Memory.WriteStringOffset(diagnosticId);
                Memory.WriteStringOffset(urlFormat);
            }
        }

        private sealed class PlatformSupportTable : HeapOrTable
        {
            public void WriteRow(ApiOffset api,
                                 AssemblyOffset assembly,
                                 BlobOffset platforms)
            {
                Memory.WriteApiOffset(api);
                Memory.WriteAssemblyOffset(assembly);
                Memory.WriteBlobOffset(platforms);
            }
        }

        private sealed class PreviewRequirementTable : HeapOrTable
        {
            public void WriteRow(ApiOffset api,
                                 AssemblyOffset assembly,
                                 StringOffset message,
                                 StringOffset url)
            {
                Memory.WriteApiOffset(api);
                Memory.WriteAssemblyOffset(assembly);
                Memory.WriteStringOffset(message);
                Memory.WriteStringOffset(url);
            }
        }

        private sealed class ExperimentalTable : HeapOrTable
        {
            public void WriteRow(ApiOffset api,
                                 AssemblyOffset assembly,
                                 StringOffset diagnosticId,
                                 StringOffset urlFormat)
            {
                Memory.WriteApiOffset(api);
                Memory.WriteAssemblyOffset(assembly);
                Memory.WriteStringOffset(diagnosticId);
                Memory.WriteStringOffset(urlFormat);
            }
        }

        private sealed class ExtensionMethodTable : HeapOrTable
        {
            public void WriteRow(Guid extensionMethodGuid,
                                 ApiOffset extendedType,
                                 ApiOffset extensionMethod)
            {
                Memory.WriteGuid(extensionMethodGuid);
                Memory.WriteApiOffset(extendedType);
                Memory.WriteApiOffset(extensionMethod);
            }
        }
    }
}