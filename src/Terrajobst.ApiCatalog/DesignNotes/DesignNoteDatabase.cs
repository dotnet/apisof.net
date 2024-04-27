using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;

namespace Terrajobst.ApiCatalog.DesignNotes;

public sealed class DesignNoteDatabase
{
    private static readonly byte[] MagicNumber = "apisof.net Design Note DB"u8.ToArray();
    private const int FormatVersion = 2;

    public static DesignNoteDatabase Empty { get; } = new(FrozenDictionary<Guid, ImmutableArray<DesignNote>>.Empty);

    private readonly FrozenDictionary<Guid, ImmutableArray<DesignNote>> _designNotesByApiGuid;

    public DesignNoteDatabase(FrozenDictionary<Guid, ImmutableArray<DesignNote>> designNotesByApiGuid)
    {
        ThrowIfNull(designNotesByApiGuid);

        _designNotesByApiGuid = designNotesByApiGuid;
    }

    public ImmutableArray<DesignNote> GetDesignNotes(Guid apiGuid)
    {
        return _designNotesByApiGuid.TryGetValue(apiGuid, out var reviewLinks)
            ? reviewLinks
            : ImmutableArray<DesignNote>.Empty;
    }

    public static DesignNoteDatabase Load(string path)
    {
        ThrowIfNullOrEmpty(path);

        using var stream = File.OpenRead(path);
        return Load(stream);
    }

    public static DesignNoteDatabase Load(Stream stream)
    {
        ThrowIfNull(stream);

        using var reader = new BinaryReader(stream);

        var magicNumber = reader.ReadBytes(MagicNumber.Length);
        if (!magicNumber.SequenceEqual(MagicNumber))
            throw new InvalidDataException("Magic number doesn't match");

        var version = reader.ReadInt32();
        if (version != FormatVersion)
            throw new InvalidDataException($"Version {version} isn't supported.");

        var designNoteCount = reader.ReadInt32();
        var designNotes = new List<DesignNote>(designNoteCount);

        for (var i = 0; i < designNoteCount; i++)
        {
            var reviewDateText = reader.ReadString();
            var reviewDate = DateTimeOffset.ParseExact(reviewDateText, "O", CultureInfo.InvariantCulture);
            var url = reader.ReadString();
            var urlText = reader.ReadString();
            var context = reader.ReadString();

            var designNote = new DesignNote(reviewDate, url, urlText, context);
            designNotes.Add(designNote);
        }

        var mappingCount = reader.ReadInt32();
        var mappings = new List<KeyValuePair<Guid, ImmutableArray<DesignNote>>>(mappingCount);

        var guidBytes = (Span<byte>)stackalloc byte[16];

        for (var i = 0; i < mappingCount; i++)
        {
            var readCount = reader.Read(guidBytes);
            Debug.Assert(readCount == 16);

            var apiGuid = new Guid(guidBytes);
            var associatedDesignNoteCount = reader.Read7BitEncodedInt();
            var associatedDesignNoteBuilder = ImmutableArray.CreateBuilder<DesignNote>(associatedDesignNoteCount);

            for (var j = 0; j < associatedDesignNoteCount; j++)
            {
                var designNoteIndex = reader.Read7BitEncodedInt();
                var designNote = designNotes[designNoteIndex];
                associatedDesignNoteBuilder.Add(designNote);
            }

            mappings.Add(KeyValuePair.Create(apiGuid, associatedDesignNoteBuilder.MoveToImmutable()));
        }

        return new DesignNoteDatabase(mappings.ToFrozenDictionary());
    }

    public void Save(string path)
    {
        ThrowIfNullOrEmpty(path);

        using var stream = File.Create(path);
        Save(stream);
    }

    public void Save(Stream stream)
    {
        ThrowIfNull(stream);

        var indexByDesignNote = new Dictionary<DesignNote, int>();

        foreach (var designNotes in _designNotesByApiGuid.Values)
        foreach (var designNote in designNotes)
        {
            if (!indexByDesignNote.ContainsKey(designNote))
                indexByDesignNote.Add(designNote, indexByDesignNote.Count);
        }

        using var binaryWriter = new BinaryWriter(stream);
        binaryWriter.Write(MagicNumber);
        binaryWriter.Write(FormatVersion);

        // Write Design Notes

        binaryWriter.Write(indexByDesignNote.Count);

        foreach (var (designNote, _) in indexByDesignNote.OrderBy(kv => indexByDesignNote[kv.Key]))
        {
            binaryWriter.Write(designNote.Date.ToString("O", CultureInfo.InvariantCulture));
            binaryWriter.Write(designNote.Url);
            binaryWriter.Write(designNote.UrlText);
            binaryWriter.Write(designNote.Context);
        }

        binaryWriter.Write(_designNotesByApiGuid.Count);

        // Write Mapping Guid -> Design Note

        var guidBytes = (Span<byte>)stackalloc byte[16];

        foreach (var (apiGuid, designNotes) in _designNotesByApiGuid)
        {
            apiGuid.TryWriteBytes(guidBytes);

            binaryWriter.Write(guidBytes);
            binaryWriter.Write7BitEncodedInt(designNotes.Length);
            foreach (var designNote in designNotes)
                binaryWriter.Write7BitEncodedInt(indexByDesignNote[designNote]);
        }
    }
}