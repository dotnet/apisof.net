namespace Terrajobst.UsageCrawling.Collectors;

public sealed class CollectionSetResults
{
    public static CollectionSetResults Empty { get; } = new(Array.Empty<VersionedFeatureSet>());

    public CollectionSetResults(IReadOnlyList<VersionedFeatureSet> featureSets)
    {
        ThrowIfNull(featureSets);

        FeatureSets = featureSets;
    }

    public IReadOnlyList<VersionedFeatureSet> FeatureSets { get; }

    public async Task SaveAsync(string fileName)
    {
        ThrowIfNullOrEmpty(fileName);

        await using var stream = File.Create(fileName);
        await SaveAsync(stream);
    }

    public async Task SaveAsync(Stream stream)
    {
        ThrowIfNull(stream);

        var lines = new List<string>();

        foreach (var versionedSet in FeatureSets)
        {
            lines.Add(versionedSet.Version.ToString());

            foreach (var feature in versionedSet.Features)
                lines.Add(feature.ToString("N"));
        }

        await using var textWriter = new StreamWriter(stream, leaveOpen: true);
        foreach (var line in lines)
            await textWriter.WriteLineAsync(line);
    }

    public static async Task<CollectionSetResults> LoadAsync(string fileName)
    {
        ThrowIfNullOrEmpty(fileName);

        await using var stream = File.OpenRead(fileName);
        return await LoadAsync(stream);
    }

    public static async Task<CollectionSetResults> LoadAsync(Stream stream)
    {
        ThrowIfNull(stream);

        using var reader = new StreamReader(stream, leaveOpen: true);

        var featureSets = new List<VersionedFeatureSet>();
        var features = new List<Guid>();
        var currentVersion = 0;

        while (await reader.ReadLineAsync() is { } line)
        {
            if (int.TryParse(line, out var version))
            {
                CompleteCurrentFeatureSet();
                currentVersion = version;
            }
            else if (Guid.TryParse(line, out var feature))
            {
                // Feature line
                features.Add(feature);
            }
            else
            {
                throw new InvalidDataException("The line '{line}' is neither a version nor a feature");
            }
        }

        CompleteCurrentFeatureSet();

        return new CollectionSetResults(featureSets);

        void CompleteCurrentFeatureSet()
        {
            if (features.Count == 0)
                return;

            var featureSet = new VersionedFeatureSet(currentVersion, new HashSet<Guid>(features));
            featureSets.Add(featureSet);

            currentVersion = 0;
            features.Clear();
        }
    }
}