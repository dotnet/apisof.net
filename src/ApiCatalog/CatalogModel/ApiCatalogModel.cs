using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiCatalog.CatalogModel
{
    // Missing:
    //   - Store usage
    //
    // The format is organized as follows:
    //
    //   - Magic Header Value ('APICATFB')
    //   - Format version
    //   - Number of tables
    //   - Table sizes
    //   - Tables
    //
    // The tables are written as follows:
    //
    //   - Strings
    //       Length
    //       UTF8 Bytes
    //   - Frameworks
    //       Offsets
    //       Name
    //       Assemblies [patched]
    //   - Packages
    //       Offsets
    //       Id
    //       Version
    //       (Framework, Assembly) [patched]
    //   - Assemblies
    //       Offsets
    //       NameOffset
    //       VersionOffset
    //       PublicKeyTokenOffset
    //       RootApiOffsets [patched]
    //       FrameworkOffsets
    //       PackageOffsets
    //   - Usage Sources
    //       Offsets
    //       NameOffset
    //       Usaget Data Sets
    //          SourceOffset
    //          Date
    //   - Apis
    //       Guid
    //       Kind
    //       NameOffset
    //       ParentOffset
    //       ChildrenOffsets
    //       (AssemblyOffset, SyntaxOffset)*
    //       (UsageDataSetOffset, Percentage)*
    //       GetSyntax(AssemblyModel) -> binary searches by assembly offset
    //       GetPercentage(UsageDataSet) -> binary searches by data set offset

    public sealed partial class ApiCatalogModel
    {
        private static IReadOnlyList<byte> MagicHeader { get; } = Encoding.ASCII.GetBytes("APICATFB");
        private const int FormatVersion = 1;

        private readonly byte[] _buffer;
        private readonly int _stringTableLength;
        private readonly int _frameworkTableOffset;
        private readonly int _frameworkTableLength;
        private readonly int _packageTableOffset;
        private readonly int _packageTableLength;
        private readonly int _assemblyTableOffset;
        private readonly int _assemblyTableLength;
        private readonly int _apiTableOffset;
        private readonly int _apiTableLength;

        private ApiCatalogModel(byte[] buffer, int[] tableSizes)
        {
            Debug.Assert(tableSizes.Length == 5);

            _stringTableLength = tableSizes[0];

            _frameworkTableOffset = _stringTableLength;
            _frameworkTableLength = tableSizes[1];

            _packageTableOffset = _frameworkTableOffset + _frameworkTableLength;
            _packageTableLength = tableSizes[2];

            _assemblyTableOffset = _packageTableOffset + _packageTableLength;
            _assemblyTableLength = tableSizes[3];

            _apiTableOffset = _assemblyTableOffset + _assemblyTableLength;
            _apiTableLength = tableSizes[4];

            _buffer = buffer;
        }

        internal ReadOnlySpan<byte> StringTable => new ReadOnlySpan<byte>(_buffer, 0, _stringTableLength);

        internal ReadOnlySpan<byte> FrameworkTable => new ReadOnlySpan<byte>(_buffer, _frameworkTableOffset, _frameworkTableLength);

        internal ReadOnlySpan<byte> PackageTable => new ReadOnlySpan<byte>(_buffer, _packageTableOffset, _packageTableLength);

        internal ReadOnlySpan<byte> AssemblyTable => new ReadOnlySpan<byte>(_buffer, _assemblyTableOffset, _assemblyTableLength);

        internal ReadOnlySpan<byte> ApiTable => new ReadOnlySpan<byte>(_buffer, _apiTableOffset, _apiTableLength);

        public IEnumerable<FrameworkModel> Frameworks
        {
            get
            {
                var count = GetFrameworkTableInt32(0);

                for (var i = 0; i < count; i++)
                {
                    var offset = GetFrameworkTableInt32(4 + 4 * i);
                    yield return new FrameworkModel(this, offset);
                }
            }
        }

        public IEnumerable<PackageModel> Packages
        {
            get
            {
                var count = GetPackageTableInt32(0);

                for (var i = 0; i < count; i++)
                {
                    var offset = GetPackageTableInt32(4 + 4 * i);
                    yield return new PackageModel(this, offset);
                }
            }
        }

        public IEnumerable<AssemblyModel> Assemblies
        {
            get
            {
                var count = GetAssemblyTableInt32(0);

                for (var i = 0; i < count; i++)
                {
                    var offset = GetAssemblyTableInt32(4 + 4 * i);
                    yield return new AssemblyModel(this, offset);
                }
            }
        }

        public IEnumerable<ApiModel> RootApis => GetApis(0);

        public IEnumerable<ApiModel> GetAllApis()
        {
            return RootApis.SelectMany(r => r.DescendantsAndSelf());
        }

        public ApiModel GetApiById(int id)
        {
            return new ApiModel(this, id);
        }

        internal IEnumerable<ApiModel> GetApis(int offset)
        {
            var childCount = GetApiTableInt32(offset);

            for (var i = 0; i < childCount; i++)
            {
                var childOffset = GetApiTableInt32(offset + 4 + 4 * i);
                yield return new ApiModel(this, childOffset);
            }
        }

        internal string GetString(int offset)
        {
            var stringSpan = StringTable.Slice(offset);
            var nameLength = BinaryPrimitives.ReadInt32LittleEndian(stringSpan);
            var nameSpan = stringSpan.Slice(4, nameLength);
            var name = Encoding.UTF8.GetString(nameSpan);
            return name;
        }

        internal Markup GetMarkup(int offset)
        {
            var span = StringTable.Slice(offset);
            var partsCount = BinaryPrimitives.ReadInt32LittleEndian(span);
            span = span.Slice(4);

            var parts = new List<MarkupPart>(partsCount);

            for (var i = 0; i < partsCount; i++)
            {
                var kind = (MarkupPartKind)span[0];
                span = span.Slice(1);
                var textOffset = BinaryPrimitives.ReadInt32LittleEndian(span);
                var text = GetString(textOffset);
                span = span.Slice(4);

                Guid? reference;

                if (kind == MarkupPartKind.Reference)
                {
                    var apiOffset = BinaryPrimitives.ReadInt32LittleEndian(span);
                    if (apiOffset < 0)
                        reference = null;
                    else
                        reference = new ApiModel(this, apiOffset).Guid;
                    span = span.Slice(4);
                }
                else
                {
                    reference = null;
                }

                var part = new MarkupPart(kind, text, reference);
                parts.Add(part);
            }

            return new Markup(parts);
        }

        internal int GetFrameworkTableInt32(int offset)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(FrameworkTable.Slice(offset));
        }

        internal int GetPackageTableInt32(int offset)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(PackageTable.Slice(offset));
        }

        internal int GetAssemblyTableInt32(int offset)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(AssemblyTable.Slice(offset));
        }

        internal int GetApiTableInt32(int offset)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(ApiTable.Slice(offset));
        }

        public void Dump(string fileName)
        {
            Console.WriteLine($"Size on disk    : {new FileInfo(fileName).Length,12:N0} bytes");
            Console.WriteLine($"Size in memory  : {_buffer.Length,12:N0} bytes");
            Console.WriteLine($"String table    : {_stringTableLength,12:N0} bytes");
            Console.WriteLine($"Framework table : {_frameworkTableLength,12:N0} bytes");
            Console.WriteLine($"Assembly table  : {_assemblyTableLength,12:N0} bytes");
            Console.WriteLine($"API table       : {_apiTableLength,12:N0} bytes");
        }

        public ApiCatalogStatistics GetStatistics()
        {
            var allApis = RootApis.SelectMany(a => a.DescendantsAndSelf());
            return new ApiCatalogStatistics(
                numberOfApis: allApis.Count(),
                numberOfDeclarations: allApis.SelectMany(a => a.Declarations).Count(),
                numberOfAssemblies: Assemblies.Count(),
                numberOfFrameworks: Frameworks.Count(),
                numberOfFrameworkAssemblies: Assemblies.SelectMany(a => a.Frameworks).Count(),
                numberOfPackages: Packages.Select(p => p.Name).Distinct().Count(),
                numberOfPackageVersions: Packages.Count(),
                numberOfPackageAssemblies: Assemblies.SelectMany(a => a.Packages).Count()
            );
        }

        public static ApiCatalogModel Load(string path)
        {
            using var stream = File.OpenRead(path);
            return Load(stream);
        }

        public static ApiCatalogModel Load(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                var magicHeader = reader.ReadBytes(8);
                if (!magicHeader.SequenceEqual(MagicHeader))
                    throw new InvalidDataException();

                var formatVersion = reader.ReadInt32();
                if (formatVersion != FormatVersion)
                    throw new InvalidDataException();

                var numberOfTables = reader.ReadInt32();
                var tableSizes = new int[numberOfTables];
                for (var i = 0; i < tableSizes.Length; i++)
                    tableSizes[i] = reader.ReadInt32();

                var bufferSize = tableSizes.Sum();

                using (var decompressedStream = new DeflateStream(stream, CompressionMode.Decompress))
                using (var decompressedReader = new BinaryReader(decompressedStream))
                {
                    var buffer = decompressedReader.ReadBytes(bufferSize);
                    return new ApiCatalogModel(buffer, tableSizes);
                }
            }
        }

        public static async Task ConvertAsync(string sqliteDbPath, string outputPath)
        {
            using (var stream = new MemoryStream())
            {
                await ConvertAsync(sqliteDbPath, stream);

                stream.Position = 0;

                using (var fileStream = File.Create(outputPath))
                    await stream.CopyToAsync(fileStream);
            }
        }

        public static Task ConvertAsync(string sqliteDbPath, Stream stream)
        {
            return Converter.ConvertAsync(sqliteDbPath, stream);
        }
    }
}
