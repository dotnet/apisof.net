namespace Terrajobst.UsageCrawling.Storage;

public abstract class UsageDatabase<TReferenceUnit> : IDisposable
{
    private readonly UsageDatabase _usageDatabase;

    protected UsageDatabase(UsageDatabase usageDatabase)
    {
        _usageDatabase = usageDatabase;
    }

    internal UsageDatabase UnderlyingDatabase => _usageDatabase;

    protected abstract TReferenceUnit ParseReferenceUnit(string referenceIdentifier);

    protected abstract string FormatReferenceUnit(TReferenceUnit referenceUnit);

    public void Dispose()
    {
        _usageDatabase.Dispose();
    }

    public Task OpenAsync()
    {
        return _usageDatabase.OpenAsync();
    }

    public Task CloseAsync()
    {
        return _usageDatabase.CloseAsync();
    }

    public Task VacuumAsync()
    {
        return _usageDatabase.VacuumAsync();
    }

    public async Task<IEnumerable<(TReferenceUnit ReferenceUnit, int CollectorVersion)>> GetReferenceUnitsAsync()
    {
        var result = await _usageDatabase.GetReferenceUnitsAsync();
        return result.Select(t => (ParseReferenceUnit(t.Identifier), t.CollectorVersion));
    }

    public Task<IEnumerable<(Guid Feature, int CollectorVersion)>> GetFeaturesAsync()
    {
        return _usageDatabase.GetFeaturesAsync();
    }

    public async Task DeleteReferenceUnitsAsync(IEnumerable<TReferenceUnit> referenceUnits)
    {
        ThrowIfNull(referenceUnits);

        var referenceUnitIdentifiers = referenceUnits.Select(FormatReferenceUnit);
        await _usageDatabase.DeleteReferenceUnitsAsync(referenceUnitIdentifiers);
    }

    public async Task DeleteFeaturesAsync(IEnumerable<Guid> features)
    {
        await _usageDatabase.DeleteFeaturesAsync(features);
    }

    public ValueTask AddReferenceUnitAsync(TReferenceUnit referenceUnit, int collectorVersion = 0)
    {
        ThrowIfNull(referenceUnit);

        var referenceUnitIdentifier = FormatReferenceUnit(referenceUnit);
        return _usageDatabase.AddReferenceUnitAsync(referenceUnitIdentifier, collectorVersion);
    }

    public ValueTask<bool> TryAddFeatureAsync(Guid feature, int collectorVersion = 0)
    {
        return _usageDatabase.TryAddFeatureAsync(feature, collectorVersion);
    }

    public ValueTask AddParentFeatureAsync(Guid childFeature, Guid parentFeature)
    {
        return _usageDatabase.AddParentFeatureAsync(childFeature, parentFeature);
    }

    public ValueTask AddUsageAsync(TReferenceUnit referenceUnit, Guid feature)
    {
        ThrowIfNull(referenceUnit);

        var referenceUnitIdentifier = FormatReferenceUnit(referenceUnit);
        return _usageDatabase.AddUsageAsync(referenceUnitIdentifier, feature);
    }

    public Task<IReadOnlyCollection<(Guid Feature, float percentage)>> GetUsagesAsync()
    {
        return _usageDatabase.GetUsagesAsync();
    }

    public Task ExportUsagesAsync(string fileName)
    {
        return _usageDatabase.ExportUsagesAsync(fileName);
    }
}
