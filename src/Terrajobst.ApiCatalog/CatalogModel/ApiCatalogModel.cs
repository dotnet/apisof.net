using System.Buffers.Binary;
using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

public sealed class ApiCatalogModel
{
    public static ApiCatalogModel Empty { get; } = new();

    public static string Url => "https://apisof.net/catalog/download";

    private readonly int _formatVersion;
    private readonly int _compressedSize;
    private readonly byte[] _buffer;

    private readonly TableRange _stringHeapRange;
    private readonly TableRange _blobHeapRange;
    private readonly TableRange _platformTableRange;
    private readonly TableRange _frameworkTableRange;
    private readonly TableRange _packageTableRange;
    private readonly TableRange _assemblyTableRange;
    private readonly TableRange _usageSourcesTableRange;
    private readonly TableRange _apiTableRange;
    private readonly TableRange _rootApiTableRange;
    private readonly TableRange _extensionMethodTableRange;
    private readonly TableRange _obsoletionTableRange;
    private readonly TableRange _platformSupportTableRange;
    private readonly TableRange _previewRequirementTableRange;
    private readonly TableRange _experimentalTableRange;

    private FrozenDictionary<Guid, int>? _apiOffsetByGuid;
    private FrozenDictionary<Guid, int>? _extensionMethodOffsetByGuid;
    private FrozenDictionary<int, int>? _forwardedApis;
    private FrozenSet<int>? _previewFrameworks;
    private readonly ApiAvailabilityContext _availabilityContext;

    private readonly struct TableRange(int offset, int length)
    {
        public int Offset { get; } = offset;
        public int Length { get; } = length;
        public int End => Offset + Length;

        public ReadOnlySpan<byte> GetBytes(byte[] buffer)
        {
            return buffer.AsSpan(Offset, Length);
        }
    }

    private ApiCatalogModel()
        : this(ApiCatalogSchema.FormatVersion, 0, Array.Empty<byte>(), new int[ApiCatalogSchema.NumberOfTables])
    {
    }
        
    private ApiCatalogModel(int formatVersion, int compressedSize, byte[] buffer, int[] tableSizes)
    {
        Debug.Assert(tableSizes.Length == ApiCatalogSchema.NumberOfTables);

        _formatVersion = formatVersion;

        var tableIndex = 0;
        _stringHeapRange = new(0, tableSizes[tableIndex++]);
        _blobHeapRange = new(_stringHeapRange.End, tableSizes[tableIndex++]);
        _platformTableRange = new(_blobHeapRange.End, tableSizes[tableIndex++]);
        _frameworkTableRange = new(_platformTableRange.End, tableSizes[tableIndex++]);
        _packageTableRange = new(_frameworkTableRange.End, tableSizes[tableIndex++]);
        _assemblyTableRange = new(_packageTableRange.End, tableSizes[tableIndex++]);
        _usageSourcesTableRange = new(_assemblyTableRange.End, tableSizes[tableIndex++]);
        _apiTableRange = new(_usageSourcesTableRange.End, tableSizes[tableIndex++]);
        _rootApiTableRange = new(_apiTableRange.End, tableSizes[tableIndex++]);
        _extensionMethodTableRange = new(_rootApiTableRange.End, tableSizes[tableIndex++]);
        _obsoletionTableRange = new(_extensionMethodTableRange.End, tableSizes[tableIndex++]);
        _platformSupportTableRange = new(_obsoletionTableRange.End, tableSizes[tableIndex++]);
        _previewRequirementTableRange = new(_platformSupportTableRange.End, tableSizes[tableIndex++]);
        _experimentalTableRange = new(_previewRequirementTableRange.End, tableSizes[tableIndex++]);

        Debug.Assert(tableIndex == tableSizes.Length);

        _buffer = buffer;
        _compressedSize = compressedSize;
        _availabilityContext = new ApiAvailabilityContext(this);
    }

    public int FormatVersion => _formatVersion;

    internal ReadOnlySpan<byte> StringHeap => _stringHeapRange.GetBytes(_buffer);

    internal ReadOnlySpan<byte> BlobHeap => _blobHeapRange.GetBytes(_buffer);

    internal ReadOnlySpan<byte> PlatformTable => _platformTableRange.GetBytes(_buffer);

    internal ReadOnlySpan<byte> FrameworkTable => _frameworkTableRange.GetBytes(_buffer);

    internal ReadOnlySpan<byte> PackageTable => _packageTableRange.GetBytes(_buffer);

    internal ReadOnlySpan<byte> AssemblyTable => _assemblyTableRange.GetBytes(_buffer);

    internal ReadOnlySpan<byte> UsageSourceTable => _usageSourcesTableRange.GetBytes(_buffer);

    internal ReadOnlySpan<byte> ApiTable => _apiTableRange.GetBytes(_buffer);

    internal ReadOnlySpan<byte> RootApiTable => _rootApiTableRange.GetBytes(_buffer);

    internal ReadOnlySpan<byte> ExtensionMethodTable => _extensionMethodTableRange.GetBytes(_buffer);

    internal ReadOnlySpan<byte> ObsoletionTable => _obsoletionTableRange.GetBytes(_buffer);

    internal ReadOnlySpan<byte> PlatformSupportTable => _platformSupportTableRange.GetBytes(_buffer);

    internal ReadOnlySpan<byte> PreviewRequirementTable => _previewRequirementTableRange.GetBytes(_buffer);

    internal ReadOnlySpan<byte> ExperimentalTable => _experimentalTableRange.GetBytes(_buffer);

    internal ApiAvailabilityContext AvailabilityContext => _availabilityContext;
    
    public FrameworkEnumerator Frameworks
    {
        get
        {
            return new FrameworkEnumerator(this);
        }
    }

    public PlatformEnumerator Platforms
    {
        get
        {
            return new PlatformEnumerator(this);
        }
    }

    public PackageEnumerator Packages
    {
        get
        {
            return new PackageEnumerator(this);
        }
    }

    public AssemblyEnumerator Assemblies
    {
        get
        {
            return new AssemblyEnumerator(this);
        }
    }

    public UsageSourceEnumerator UsageSources
    {
        get
        {
            return new UsageSourceEnumerator(this);
        }
    }

    public RootApisEnumerator RootApis
    {
        get
        {
            return new RootApisEnumerator(this);
        }
    }

    public AllApisEnumerator AllApis
    {
        get
        {
            return new AllApisEnumerator(this);
        }
    }

    public ExtensionMethodEnumerator ExtensionMethods
    {
        get
        {
            return new ExtensionMethodEnumerator(this);
        }
    }

    public FrameworkModel? GetFramework(NuGetFramework framework)
    {
        ThrowIfNull(framework);

        return _availabilityContext.GetFramework(framework);
    }
    
    public ApiModel GetApiById(int id)
    {
        return new ApiModel(this, id);
    }

    public AssemblyModel GetAssemblyById(int id)
    {
        return new AssemblyModel(this, id);
    }

    public ApiModel GetApiByGuid(Guid guid)
    {
        if (_apiOffsetByGuid is null)
        {
            var apiByGuid = AllApis.ToFrozenDictionary(a => a.Guid, a => a.Id);
            Interlocked.CompareExchange(ref _apiOffsetByGuid, apiByGuid, null);
        }

        var offset = _apiOffsetByGuid[guid];
        return new ApiModel(this, offset);
    }

    public ExtensionMethodModel GetExtensionMethodByGuid(Guid guid)
    {
        if (_extensionMethodOffsetByGuid is null)
        {
            var extensionMethodOffsetByGuid = ExtensionMethods.ToFrozenDictionary(a => a.Guid, a => a.Id);
            Interlocked.CompareExchange(ref _extensionMethodOffsetByGuid, extensionMethodOffsetByGuid, null);
        }

        var offset = _extensionMethodOffsetByGuid[guid];
        return new ExtensionMethodModel(this, offset);
    }

    internal bool IsPreviewFramework(FrameworkModel model)
    {
        if (_previewFrameworks is null)
        {
            var previewFrameworks = GetPreviewFrameworks(this);
            Interlocked.CompareExchange(ref _previewFrameworks, previewFrameworks, null);
        }

        return _previewFrameworks.Contains(model.Id);

        static FrozenSet<int> GetPreviewFrameworks(ApiCatalogModel apiCatalogModel)
        {
            var latestNetCoreAppVersion = apiCatalogModel.Frameworks
                .Where(fx => string.Equals(fx.NuGetFramework.Framework, ".NETCoreApp", StringComparison.OrdinalIgnoreCase))
                .Select(fx => fx.NuGetFramework.Version)
                .Max();
            
            if (latestNetCoreAppVersion is null)
                return FrozenSet<int>.Empty;

            return apiCatalogModel.Frameworks
                .Where(fx => string.Equals(fx.NuGetFramework.Framework, ".NETCoreApp", StringComparison.OrdinalIgnoreCase) &&
                             fx.NuGetFramework.Version == latestNetCoreAppVersion)
                .Select(fx => fx.Id)
                .ToFrozenSet();
        }
    }

    internal string GetString(int offset)
    {
        var stringLength = StringHeap.ReadInt32(offset);
        var stringSpan = StringHeap.Slice(offset + 4, stringLength);
        return Encoding.UTF8.GetString(stringSpan);
    }

    internal Markup GetMarkup(int offset)
    {
        var span = BlobHeap[offset..];
        var tokenCount = BinaryPrimitives.ReadInt32LittleEndian(span);
        span = span[4..];

        var tokens = new List<MarkupToken>(tokenCount);

        for (var i = 0; i < tokenCount; i++)
        {
            var kind = (MarkupTokenKind)span[0];
            span = span[1..];

            string? text;

            if (kind.GetTokenText() is not null)
            {
                text = null;
            }
            else
            {
                var textOffset = BinaryPrimitives.ReadInt32LittleEndian(span);
                text = GetString(textOffset);
                span = span[4..];
            }

            Guid? reference;

            if (kind == MarkupTokenKind.ReferenceToken)
            {
                var apiOffset = BinaryPrimitives.ReadInt32LittleEndian(span);
                if (apiOffset < 0)
                    reference = null;
                else
                    reference = new ApiModel(this, apiOffset).Guid;
                span = span[4..];
            }
            else
            {
                reference = null;
            }

            var token = new MarkupToken(kind, text, reference);
            tokens.Add(token);
        }

        return new Markup(tokens);
    }

    internal int GetExtensionMethodOffset(int extensionTypeId)
    {
        var rowSize = ApiCatalogSchema.ExtensionMethodRow.Size;
        Debug.Assert(ExtensionMethodTable.Length % rowSize == 0);

        var low = 0;
        var high = ExtensionMethodTable.Length / rowSize - 1;

        while (low <= high)
        {
            var middle = low + ((high - low) >> 1);
            var rowStart = middle * rowSize;

            var rowExtensionTypeId = ApiCatalogSchema.ExtensionMethodRow.ExtendedType.Read(this, rowStart);
            var comparison = extensionTypeId.CompareTo(rowExtensionTypeId.Id);

            if (comparison == 0)
            {
                // The table is allowed to contain multiple entries. Just look backwards and adjust the row offset
                // until we find a row with different values.
                var previousRowStart = rowStart - rowSize;
                while (previousRowStart >= 0)
                {
                    var previousRowExtensionTypeId = ApiCatalogSchema.ExtensionMethodRow.ExtendedType.Read(this, previousRowStart);
                    if (previousRowExtensionTypeId != rowExtensionTypeId)
                        break;

                    rowStart = previousRowStart;
                    previousRowStart -= rowSize;
                }

                return rowStart;
            }

            if (comparison < 0)
                high = middle - 1;
            else
                low = middle + 1;
        }

        return -1;
    }

    private int GetDeclarationTableOffset(ReadOnlySpan<byte> table,
                                          int rowSize,
                                          ApiCatalogSchema.Field<ApiModel?> apiField,
                                          ApiCatalogSchema.Field<AssemblyModel> assemblyField,
                                          ApiModel? api,
                                          AssemblyModel assembly)
    {
        Debug.Assert(table.Length % rowSize == 0);

        var low = 0;
        var high = table.Length / rowSize - 1;

        var lookupKey = (api?.Id ?? -1, assembly.Id);

        while (low <= high)
        {
            var middle = low + ((high - low) >> 1);
            var rowStart = middle * rowSize;

            var rowApi = apiField.Read(this, rowStart);
            var rowAssembly = assemblyField.Read(this, rowStart);

            var rowKey = (rowApi?.Id ?? -1, rowAssembly.Id);
            var comparison = lookupKey.CompareTo(rowKey);

            if (comparison == 0)
            {
                // The declaration table is allowed to contain multiple entries for a given apiId/assemblyId
                // combination. Our binary search may have jumped in the middle of sequence of rows with the same
                // apiId/assemblyId. Just look backwards and adjust the row offset until we find a row with different
                // values.
                var previousRowStart = rowStart - rowSize;
                while (previousRowStart >= 0)
                {
                    var previousRowApi = apiField.Read(this, previousRowStart);
                    var previousRowAssembly = assemblyField.Read(this, previousRowStart);
                    var previousRowKey = (previousRowApi?.Id ?? -1, previousRowAssembly.Id);

                    var same = previousRowKey == lookupKey;
                    if (!same)
                        break;

                    rowStart = previousRowStart;
                    previousRowStart -= rowSize;
                }

                return rowStart;
            }

            if (comparison < 0)
                high = middle - 1;
            else
                low = middle + 1;
        }

        return -1;
    }

    internal ObsoletionModel? GetObsoletion(ApiModel api, AssemblyModel assembly)
    {
        var offset = GetDeclarationTableOffset(ObsoletionTable,
                                               ApiCatalogSchema.ObsoletionRow.Size,
                                               ApiCatalogSchema.ObsoletionRow.Api,
                                               ApiCatalogSchema.ObsoletionRow.Assembly,
                                               api,
                                               assembly);

        return offset < 0 ? null : new ObsoletionModel(this, offset);
    }

    internal IEnumerable<PlatformSupportModel> GetPlatformSupport(ApiModel? api, AssemblyModel assembly)
    {
        var offset = GetDeclarationTableOffset(PlatformSupportTable,
                                               ApiCatalogSchema.PlatformSupportRow.Size,
                                               ApiCatalogSchema.PlatformSupportRow.Api,
                                               ApiCatalogSchema.PlatformSupportRow.Assembly,
                                               api,
                                               assembly);
        if (offset < 0)
            yield break;

        var enumerator = ApiCatalogSchema.PlatformSupportRow.Platforms.Read(this, offset);
        while (enumerator.MoveNext())
        {
            var o = enumerator.Current;
            yield return new PlatformSupportModel(this, o);
        }
    }

    internal PreviewRequirementModel? GetPreviewRequirement(ApiModel? api, AssemblyModel assembly)
    {
        var offset = GetDeclarationTableOffset(PreviewRequirementTable,
                                               ApiCatalogSchema.PreviewRequirementRow.Size,
                                               ApiCatalogSchema.PreviewRequirementRow.Api,
                                               ApiCatalogSchema.PreviewRequirementRow.Assembly,
                                               api,
                                               assembly);

        return offset < 0 ? null : new PreviewRequirementModel(this, offset);
    }

    internal ExperimentalModel? GetExperimental(ApiModel? api, AssemblyModel assembly)
    {
        var offset = GetDeclarationTableOffset(ExperimentalTable,
                                               ApiCatalogSchema.ExperimentalRow.Size,
                                               ApiCatalogSchema.ExperimentalRow.Api,
                                               ApiCatalogSchema.ExperimentalRow.Assembly,
                                               api,
                                               assembly);

        return offset < 0 ? null : new ExperimentalModel(this, offset);
    }

    public ApiCatalogStatistics GetStatistics()
    {
        var allApis = RootApis.SelectMany(a => a.DescendantsAndSelf());
        var tableSizes = new[] {
            ("String Heap", StringHeap.Length, -1),
            ("Blob Heap", BlobHeap.Length, -1),
            ("Platform Table", PlatformTable.Length, PlatformTable.Length / ApiCatalogSchema.PlatformRow.Size),
            ("Framework Table", FrameworkTable.Length, FrameworkTable.Length / ApiCatalogSchema.FrameworkRow.Size),
            ("Package Table", PackageTable.Length, PackageTable.Length / ApiCatalogSchema.PackageRow.Size),
            ("Assembly Table", AssemblyTable.Length, AssemblyTable.Length / ApiCatalogSchema.AssemblyRow.Size),
            ("Usage Source Table", UsageSourceTable.Length, UsageSourceTable.Length / ApiCatalogSchema.UsageSourceRow.Size),
            ("API Table", ApiTable.Length, ApiTable.Length / ApiCatalogSchema.ApiRow.Size),
            ("Root API Table", RootApiTable.Length, RootApiTable.Length / ApiCatalogSchema.RootApiRow.Size),
            ("Obsoletion Table", ObsoletionTable.Length, ObsoletionTable.Length / ApiCatalogSchema.ObsoletionRow.Size),
            ("Platform Support Table", PlatformSupportTable.Length, PlatformSupportTable.Length / ApiCatalogSchema.PlatformSupportRow.Size),
            ("Preview Requirement Table", PreviewRequirementTable.Length, PreviewRequirementTable.Length / ApiCatalogSchema.PreviewRequirementRow.Size),
            ("Experimental Table", ExperimentalTable.Length, ExperimentalTable.Length / ApiCatalogSchema.ExperimentalRow.Size),
            ("Extension Method Table", ExtensionMethodTable.Length, ExtensionMethodTable.Length / ApiCatalogSchema.ExtensionMethodRow.Size)
        };

        return new ApiCatalogStatistics(
            sizeCompressed: _compressedSize,
            sizeUncompressed: _buffer.Length,
            numberOfApis: allApis.Count(),
            numberOfExtensionMethods: ExtensionMethods.Count(),
            numberOfDeclarations: allApis.SelectMany(a => a.Declarations).Count(),
            numberOfAssemblies: Assemblies.Count(),
            numberOfFrameworks: Frameworks.Count(),
            numberOfFrameworkAssemblies: Assemblies.SelectMany(a => a.Frameworks).Count(),
            numberOfPackages: Packages.Select(p => p.Name).Distinct().Count(),
            numberOfPackageVersions: Packages.Count(),
            numberOfPackageAssemblies: Assemblies.SelectMany(a => a.Packages).Count(),
            numberOfUsageSources: UsageSources.Count(),
            tableSizes
        );
    }

    public HashSet<string> GetAssemblyNames()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in Assemblies)
            result.Add(assembly.Name);

        return result;
    }

    public ApiModel? GetForwardedApi(ApiModel api)
    {
        if (_forwardedApis is null)
        {
            var forwardedApis = ComputeForwardedApis();
            Interlocked.CompareExchange(ref _forwardedApis, forwardedApis, null);
        }

        if (_forwardedApis.TryGetValue(api.Id, out var forwardedId))
            return GetApiById(forwardedId);

        return null;
    }

    private FrozenDictionary<int, int> ComputeForwardedApis()
    {
        var result = new Dictionary<int, int>();
        ForwardTypeMembers(result, this, "System.Reflection.TypeInfo", "System.Type");
        ForwardTypeMembers(result, this, "System.Type", "System.Reflection.MemberInfo");
        ForwardTypeMembers(result, this, "Microsoft.Win32.FileDialog", "Microsoft.Win32.CommonItemDialog");
        return result.ToFrozenDictionary();

        static void ForwardTypeMembers(Dictionary<int, int> receiver, ApiCatalogModel catalog, string fromTypeFullName, string toTypeFullName)
        {
            var toApi = catalog.AllApis.Single(a => a.GetFullName() == toTypeFullName);
            var fromApi = catalog.AllApis.Single(a => a.GetFullName() == fromTypeFullName);

            var toMemberByRelativeName = toApi.Descendants()
                                              .Select(a => (Name: a.GetFullName()[(toTypeFullName.Length + 1)..], Api: a))
                                              .ToDictionary(t => t.Name, t => t.Api);

            var fromMembers = fromApi.Descendants()
                                     .Select(a => (Name: a.GetFullName()[(fromTypeFullName.Length + 1)..], Api: a));

            foreach (var (name, fromMember) in fromMembers)
            {
                if (toMemberByRelativeName.TryGetValue(name, out var toMember))
                    receiver.TryAdd(fromMember.Id, toMember.Id);
            }
        }
    }

    public static async Task<ApiCatalogModel> LoadFromWebAsync()
    {
        using var client = new HttpClient();
        await using var stream = await client.GetStreamAsync(Url);
        await using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        return await LoadAsync(memoryStream);
    }

    public static async Task DownloadFromWebAsync(string fileName)
    {
        using var client = new HttpClient();
        await using var networkStream = await client.GetStreamAsync(Url);
        await using var fileStream = File.Create(fileName);
        await networkStream.CopyToAsync(fileStream);
    }

    public static async Task<ApiCatalogModel> LoadAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        return await LoadAsync(stream);
    }

    public static async Task<ApiCatalogModel> LoadAsync(Stream stream)
    {
        var start = stream.Position;

        using var reader = new BinaryReader(stream);

        var magicNumber = reader.ReadBytes(ApiCatalogSchema.MagicNumber.Length);
        if (!ApiCatalogSchema.MagicNumber.SequenceEqual(magicNumber))
            throw new InvalidDataException("API Catalog magic number doesn't match.");

        var formatVersion = reader.ReadInt32();
        if (formatVersion != ApiCatalogSchema.FormatVersion)
            throw new InvalidDataException($"API Catalog version doesn't match. Found {formatVersion}, expected {ApiCatalogSchema.FormatVersion}.");

        var numberOfTables = reader.ReadInt32();
        if (numberOfTables != ApiCatalogSchema.NumberOfTables)
            throw new InvalidDataException($"API Catalog table count doesn't match. Found {numberOfTables}, expected {ApiCatalogSchema.NumberOfTables}.");

        var tableSizes = new int[numberOfTables];
        for (var i = 0; i < tableSizes.Length; i++)
            tableSizes[i] = reader.ReadInt32();

        var bufferSize = tableSizes.Sum();

        await using var decompressedStream = new DeflateStream(stream, CompressionMode.Decompress);

        var buffer = new byte[bufferSize];
        var offset = 0;

        while (offset < buffer.Length)
            offset += await decompressedStream.ReadAsync(buffer, offset, buffer.Length - offset);

        var sizeOnDisk = (int)(stream.Position - start);

        return new ApiCatalogModel(formatVersion, sizeOnDisk, buffer, tableSizes);
    }

    public struct FrameworkEnumerator : IEnumerable<FrameworkModel>, IEnumerator<FrameworkModel>
    {
        private readonly ApiCatalogModel _catalog;
        private ApiCatalogSchema.TableRowEnumerator _enumerator;

        internal FrameworkEnumerator(ApiCatalogModel catalog)
        {
            _enumerator = new ApiCatalogSchema.TableRowEnumerator(catalog.FrameworkTable, ApiCatalogSchema.FrameworkRow.Size);
            _catalog = catalog;
        }

        IEnumerator<FrameworkModel> IEnumerable<FrameworkModel>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public FrameworkEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public FrameworkModel Current
        {
            get
            {
                var offset = _enumerator.Current;
                return new FrameworkModel(_catalog, offset);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDisposable.Dispose()
        {
        }
    }

    public struct PlatformEnumerator : IEnumerable<PlatformModel>, IEnumerator<PlatformModel>
    {
        private readonly ApiCatalogModel _catalog;
        private ApiCatalogSchema.TableRowEnumerator _enumerator;

        internal PlatformEnumerator(ApiCatalogModel catalog)
        {
            _enumerator = new ApiCatalogSchema.TableRowEnumerator(catalog.PlatformTable, ApiCatalogSchema.PlatformRow.Size);
            _catalog = catalog;
        }

        IEnumerator<PlatformModel> IEnumerable<PlatformModel>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public PlatformEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public PlatformModel Current
        {
            get
            {
                var offset = _enumerator.Current;
                return new PlatformModel(_catalog, offset);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDisposable.Dispose()
        {
        }
    }

    public struct PackageEnumerator : IEnumerable<PackageModel>, IEnumerator<PackageModel>
    {
        private readonly ApiCatalogModel _catalog;
        private ApiCatalogSchema.TableRowEnumerator _enumerator;

        internal PackageEnumerator(ApiCatalogModel catalog)
        {
            _catalog = catalog;
            _enumerator = new ApiCatalogSchema.TableRowEnumerator(catalog.PackageTable, ApiCatalogSchema.PackageRow.Size);
        }

        IEnumerator<PackageModel> IEnumerable<PackageModel>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public PackageEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public PackageModel Current
        {
            get
            {
                var offset = _enumerator.Current;
                return new PackageModel(_catalog, offset);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDisposable.Dispose()
        {
        }
    }

    public struct AssemblyEnumerator : IEnumerable<AssemblyModel>, IEnumerator<AssemblyModel>
    {
        private readonly ApiCatalogModel _catalog;
        private ApiCatalogSchema.TableRowEnumerator _enumerator;

        internal AssemblyEnumerator(ApiCatalogModel catalog)
        {
            _catalog = catalog;
            _enumerator = new ApiCatalogSchema.TableRowEnumerator(catalog.AssemblyTable, ApiCatalogSchema.AssemblyRow.Size);
        }

        IEnumerator<AssemblyModel> IEnumerable<AssemblyModel>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public AssemblyEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public AssemblyModel Current
        {
            get
            {
                var offset = _enumerator.Current;
                return new AssemblyModel(_catalog, offset);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDisposable.Dispose()
        {
        }
    }

    public struct UsageSourceEnumerator : IEnumerable<UsageSourceModel>, IEnumerator<UsageSourceModel>
    {
        private readonly ApiCatalogModel _catalog;
        private ApiCatalogSchema.TableRowEnumerator _enumerator;

        internal UsageSourceEnumerator(ApiCatalogModel catalog)
        {
            _catalog = catalog;
            _enumerator = new ApiCatalogSchema.TableRowEnumerator(catalog.UsageSourceTable, ApiCatalogSchema.UsageSourceRow.Size);
        }

        IEnumerator<UsageSourceModel> IEnumerable<UsageSourceModel>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public UsageSourceEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public UsageSourceModel Current
        {
            get
            {
                var offset = _enumerator.Current;
                return new UsageSourceModel(_catalog, offset);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDisposable.Dispose()
        {
        }
    }

    public struct RootApisEnumerator : IEnumerable<ApiModel>, IEnumerator<ApiModel>
    {
        private readonly ApiCatalogModel _catalog;
        private ApiCatalogSchema.TableRowEnumerator _enumerator;

        internal RootApisEnumerator(ApiCatalogModel catalog)
        {
            _catalog = catalog;
            _enumerator = new ApiCatalogSchema.TableRowEnumerator(_catalog.RootApiTable, ApiCatalogSchema.RootApiRow.Size);
        }

        IEnumerator<ApiModel> IEnumerable<ApiModel>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public RootApisEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public ApiModel Current
        {
            get
            {
                var rowOffset = _enumerator.Current;
                return ApiCatalogSchema.RootApiRow.Api.Read(_catalog, rowOffset);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDisposable.Dispose()
        {
        }
    }

    public struct AllApisEnumerator : IEnumerable<ApiModel>, IEnumerator<ApiModel>
    {
        private readonly ApiCatalogModel _catalog;
        private ApiCatalogSchema.TableRowEnumerator _enumerator;

        public AllApisEnumerator(ApiCatalogModel catalog)
        {
            _catalog = catalog;
            _enumerator = new ApiCatalogSchema.TableRowEnumerator(_catalog.ApiTable, ApiCatalogSchema.ApiRow.Size);
        }

        IEnumerator<ApiModel> IEnumerable<ApiModel>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public AllApisEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public ApiModel Current
        {
            get
            {
                var offset = _enumerator.Current;
                return new ApiModel(_catalog, offset);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDisposable.Dispose()
        {
        }
    }

    public struct ExtensionMethodEnumerator : IEnumerable<ExtensionMethodModel>, IEnumerator<ExtensionMethodModel>
    {
        private readonly ApiCatalogModel _catalog;
        private ApiCatalogSchema.TableRowEnumerator _enumerator;

        public ExtensionMethodEnumerator(ApiCatalogModel catalog)
        {
            _catalog = catalog;
            _enumerator = new ApiCatalogSchema.TableRowEnumerator(_catalog.ExtensionMethodTable, ApiCatalogSchema.ExtensionMethodRow.Size);
        }

        IEnumerator<ExtensionMethodModel> IEnumerable<ExtensionMethodModel>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public ExtensionMethodEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public ExtensionMethodModel Current
        {
            get
            {
                var offset = _enumerator.Current;
                return new ExtensionMethodModel(_catalog, offset);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDisposable.Dispose()
        {
        }
    }
}