using System.Collections.ObjectModel;
using System.Text;

namespace Terrajobst.ApiCatalog.Features;

public sealed class FeatureUsageData
{
    private static readonly byte[] MagicNumber = "apisof.net Feature Usage Data"u8.ToArray();
    private const int FormatVersion = 1;

    public static FeatureUsageData Empty { get; } = new(ReadOnlyCollection<(FeatureUsageSource Source, IReadOnlyList<(Guid FeatureId, float Percentage)> Values)>.Empty);

    private readonly FeatureUsageSource[] _usageSources;
    private readonly Guid[] _guids;
    private readonly UsageDataRow[] _rows;

    public FeatureUsageData(IReadOnlyCollection<(FeatureUsageSource Source, IReadOnlyList<(Guid FeatureId, float Percentage)> Values)> data)
    {
        ThrowIfNull(data);

        var guids = data.SelectMany(ds => ds.Values)
                        .Select(v => v.FeatureId)
                        .ToHashSet()
                        .ToArray();

        Array.Sort(guids);

        var usageSources = new List<FeatureUsageSource>();
        var rows = new List<UsageDataRow>();

        foreach (var (dataSource, values) in data)
        {
            var dataSourceIndex = usageSources.Count;
            usageSources.Add(dataSource);

            foreach (var (featureId, percentage) in values)
            {
                var guidIndex = Array.BinarySearch(guids, featureId);
                var row = new UsageDataRow(guidIndex, dataSourceIndex, percentage);
                rows.Add(row);
            }
        }

        rows.Sort((x, y) =>
        {
            var result = x.GuidIndex.CompareTo(y.GuidIndex);
            if (result == 0)
                result = x.DataSourceIndex.CompareTo(y.DataSourceIndex);
            return result;
        });

        _usageSources = usageSources.ToArray();
        _guids = guids;
        _rows = rows.ToArray();
    }

    private FeatureUsageData(FeatureUsageSource[] usageSources, Guid[] guids, UsageDataRow[] rows)
    {
        _usageSources = usageSources;
        _guids = guids;
        _rows = rows;
    }

    public static FeatureUsageData Load(string path)
    {
        ThrowIfNullOrEmpty(path);

        using var stream = File.OpenRead(path);
        return Load(stream);
    }

    public static FeatureUsageData Load(Stream stream)
    {
        ThrowIfNull(stream);

        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        var magicNumber = reader.ReadBytes(MagicNumber.Length);
        if (!magicNumber.SequenceEqual(MagicNumber))
            throw new InvalidDataException("Magic number doesn't match");

        var version = reader.ReadInt32();
        if (version != FormatVersion)
            throw new InvalidDataException($"Version {version} isn't supported.");

        var dataSourceCount = reader.ReadInt32();
        var dataSources = new FeatureUsageSource[dataSourceCount];
        for (var i = 0; i < dataSources.Length; i++)
        {
            var name = reader.ReadString();
            var dayNumber = reader.ReadInt32();
            var date = DateOnly.FromDayNumber(dayNumber);

            dataSources[i] = new FeatureUsageSource(name, date);
        }

        var guidCount = reader.ReadInt32();
        var guids = new Guid[guidCount];

        for (var i = 0; i < guids.Length; i++)
        {
            var guidBytes = reader.ReadBytes(16);
            guids[i] = new Guid(guidBytes);
        }

        var rowCount = reader.ReadInt32();
        var rows = new UsageDataRow[rowCount];

        for (var i = 0; i < rows.Length; i++)
        {
            var guidIndex = reader.Read7BitEncodedInt();
            var dataSourceIndex = reader.Read7BitEncodedInt();
            var percentage = reader.ReadSingle();
            rows[i] = new UsageDataRow(guidIndex, dataSourceIndex, percentage);
        }

        return new FeatureUsageData(dataSources, guids, rows);
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

        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        writer.Write(MagicNumber);
        writer.Write(FormatVersion);

        writer.Write(_usageSources.Length);

        foreach (var ds in _usageSources)
        {
            writer.Write(ds.Name);
            writer.Write(ds.Date.DayNumber);
        }

        writer.Write(_guids.Length);

        var guidBytes = (Span<byte>)stackalloc byte[16];

        foreach (var guid in _guids)
        {
            guid.TryWriteBytes(guidBytes);
            writer.Write(guidBytes);
        }

        writer.Write(_rows.Length);

        foreach (var row in _rows)
        {
            writer.Write7BitEncodedInt(row.GuidIndex);
            writer.Write7BitEncodedInt(row.DataSourceIndex);
            writer.Write(row.Percentage);
        }
    }

    public IEnumerable<(FeatureUsageSource DataSource, float Percentage)> GetUsage(Guid featureId)
    {
        var guidIndex = Array.BinarySearch(_guids, featureId);
        if (guidIndex < 0)
            yield break;

        var rowIndex = FindRow(guidIndex);
        if (rowIndex < 0)
            yield break;

        while (rowIndex < _rows.Length && _rows[rowIndex].GuidIndex == guidIndex)
        {
            var row = _rows[rowIndex];
            var dataSource = _usageSources[row.DataSourceIndex];
            var percentage = row.Percentage;
            rowIndex++;
            yield return (dataSource, percentage);
        }
    }

    private int FindRow(int guidIndex)
    {
        var low = 0;
        var high = _rows.Length - 1;

        while (low <= high)
        {
            var middle = low + ((high - low) >> 1);
            var result = middle;
            var comparison = guidIndex.CompareTo(_rows[middle].GuidIndex);

            if (comparison == 0)
            {
                while (result - 1 >= 0 && _rows[result - 1].GuidIndex == guidIndex)
                    result--;

                return result;
            }

            if (comparison < 0)
                high = middle - 1;
            else
                low = middle + 1;
        }

        return -1;
    }

    private readonly struct UsageDataRow(int guidIndex, int dataSourceIndex, float percentage)
    {
        public int GuidIndex { get; } = guidIndex;
        public int DataSourceIndex { get; } = dataSourceIndex;
        public float Percentage { get; } = percentage;
    }
}