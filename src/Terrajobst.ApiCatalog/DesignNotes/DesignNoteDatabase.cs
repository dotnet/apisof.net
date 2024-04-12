using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;

namespace Terrajobst.ApiCatalog.DesignNotes;

public sealed class DesignNoteDatabase
{
    public static DesignNoteDatabase Empty { get; } = new(FrozenDictionary<int, ImmutableArray<DesignNote>>.Empty);

    public DesignNoteDatabase(FrozenDictionary<int, ImmutableArray<DesignNote>> linkByApiId)
    {
        ThrowIfNull(linkByApiId);

        LinkByApiId = linkByApiId;
    }

    public FrozenDictionary<int, ImmutableArray<DesignNote>> LinkByApiId { get; }

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

        var linkCount = reader.ReadInt32();
        var links = new List<DesignNote>(linkCount);

        for (var i = 0; i < linkCount; i++)
        {
            var reviewDateText = reader.ReadString();
            var reviewDate = DateTimeOffset.ParseExact(reviewDateText, "O", CultureInfo.InvariantCulture);
            var url = reader.ReadString();
            var urlText = reader.ReadString();
            var context = reader.ReadString();

            var link = new DesignNote(reviewDate, url, urlText, context);
            links.Add(link);
        }

        var mappingCount = reader.ReadInt32();
        var mappings = new List<KeyValuePair<int, ImmutableArray<DesignNote>>>(mappingCount);

        for (var i = 0; i < mappingCount; i++)
        {
            var apiId = reader.ReadInt32();
            var associatedLinkCount = reader.ReadInt32();
            var associatedLinkBuilder = ImmutableArray.CreateBuilder<DesignNote>(associatedLinkCount);

            for (var j = 0; j < associatedLinkCount; j++)
            {
                var linkIndex = reader.ReadInt32();
                var link = links[linkIndex];
                associatedLinkBuilder.Add(link);
            }

            mappings.Add(KeyValuePair.Create(apiId, associatedLinkBuilder.MoveToImmutable()));
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

        var indexByReviewLink = new Dictionary<DesignNote, int>();

        foreach (var reviewLinks in LinkByApiId.Values)
        foreach (var reviewLink in reviewLinks)
        {
            if (!indexByReviewLink.ContainsKey(reviewLink))
                indexByReviewLink.Add(reviewLink, indexByReviewLink.Count);
        }

        using var binaryWriter = new BinaryWriter(stream);
        binaryWriter.Write(MagicNumber);
        binaryWriter.Write(FormatVersion);

        binaryWriter.Write(indexByReviewLink.Count);

        foreach (var (link, _) in indexByReviewLink.OrderBy(kv => indexByReviewLink[kv.Key]))
        {
            binaryWriter.Write(link.Date.ToString("O", CultureInfo.InvariantCulture));
            binaryWriter.Write(link.Url);
            binaryWriter.Write(link.UrlText);
            binaryWriter.Write(link.Context);
        }

        binaryWriter.Write(LinkByApiId.Count);

        foreach (var (apiId, reviewLinks) in LinkByApiId)
        {
            binaryWriter.Write(apiId);
            binaryWriter.Write(reviewLinks.Length);
            foreach (var reviewLink in reviewLinks)
                binaryWriter.Write(indexByReviewLink[reviewLink]);
        }
    }

    private static readonly byte[] MagicNumber = "apisof.net Design Note DB"u8.ToArray();
    private const int FormatVersion = 1;
}