using Terrajobst.UsageCrawling.Storage;

namespace GenUsageNuGet.Tests;

public class UsageDatabaseTests : IDisposable
{
    private readonly string _fileName;

    public UsageDatabaseTests()
    {
        _fileName = Path.GetTempFileName();
        File.Delete(_fileName);
    }

    public void Dispose()
    {
        File.Delete(_fileName);
    }

    [Fact]
    public async Task UsageDatabase_OpenOrCreateAsync_CreateIsEmpty()
    {
        using var db = await UsageDatabase.OpenOrCreateAsync(_fileName);
        var referenceUnits = (await db.GetReferenceUnitsAsync()).ToArray();

        Assert.Empty(referenceUnits);
    }

    [Fact]
    public async Task UsageDatabase_OpenOrCreateAsync_OpenReturnsExistingData()
    {
        using (var db = await UsageDatabase.OpenOrCreateAsync(_fileName))
        {
            await db.AddReferenceUnitAsync("Test1");
            await db.AddReferenceUnitAsync("Test2");
        }

        using (var db = await UsageDatabase.OpenOrCreateAsync(_fileName))
        {
            var result = await db.GetReferenceUnitsAsync();
            Assert.Equal(["Test1", "Test2"], result.Select(r => r.Identifier));
        }
    }

    [Fact]
    public async Task UsageDatabase_CloseOpen_ReleasesAndReacquiresFileLock()
    {
        using var db = await UsageDatabase.OpenOrCreateAsync(_fileName);

        Assert.Throws<IOException>(() => OpenAndCloseFileForExclusiveRead(_fileName));

        await db.CloseAsync();

        OpenAndCloseFileForExclusiveRead(_fileName);

        await db.OpenAsync();

        Assert.Throws<IOException>(() => OpenAndCloseFileForExclusiveRead(_fileName));

        static void OpenAndCloseFileForExclusiveRead(string fileName)
        {
            using (File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.None))
            {
            }
        }
    }

    [Fact]
    public async Task UsageDatabase_CloseOpen_CanWriteAndRead()
    {
        using var db = await UsageDatabase.OpenOrCreateAsync(_fileName);

        await db.CloseAsync();
        await db.OpenAsync();

        var feature = Guid.NewGuid();
        await db.TryAddFeatureAsync(feature);
        var features = await db.GetFeaturesAsync();

        var actualFeature = Assert.Single(features);
        Assert.Equal(feature, actualFeature.Feature);
    }

    [Fact]
    public async Task UsageDatabase_AddReference_ReturnsIt()
    {
        using var db = await UsageDatabase.OpenOrCreateAsync(_fileName);
        await db.AddReferenceUnitAsync("Test");

        var referenceUnits = await db.GetReferenceUnitsAsync();
        var result = Assert.Single(referenceUnits);

        Assert.Equal("Test", result.Identifier);
    }

    [Fact]
    public async Task UsageDatabase_DeleteReference_RemovesIt()
    {
        using var db = await UsageDatabase.OpenOrCreateAsync(_fileName);
        await db.AddReferenceUnitAsync("Test1");
        await db.AddReferenceUnitAsync("Test2");
        await db.AddReferenceUnitAsync("Test3");

        await db.DeleteReferenceUnitsAsync(["Test1", "Test3"]);

        var referenceUnits = await db.GetReferenceUnitsAsync();
        var result = Assert.Single(referenceUnits);

        Assert.Equal("Test2", result.Identifier);

        await db.AddReferenceUnitAsync("Test1");
        await db.AddReferenceUnitAsync("Test3");

        var actualUnits = (await db.GetReferenceUnitsAsync()).Select(r => r.Identifier).Order();
        Assert.Equal(["Test1", "Test2", "Test3"], actualUnits);
    }

    [Fact]
    public async Task UsageDatabase_TryAddFeature_ReturnsIt()
    {
        using var db = await UsageDatabase.OpenOrCreateAsync(_fileName);

        var feature1 = Guid.Parse("937e76cd-85cc-47a1-9839-6940b52a2a27");
        var feature2 = Guid.Parse("c62ebd6f-d809-4672-9f0b-982508f7dfea");
        Assert.True(await db.TryAddFeatureAsync(feature1));
        Assert.True(await db.TryAddFeatureAsync(feature2));

        var expectedResults = new[] { feature1, feature2 };
        var actualResults = (await db.GetFeaturesAsync()).Select(r => r.Feature).Order();
        Assert.Equal(expectedResults, actualResults);
    }

    [Fact]
    public async Task UsageDatabase_TryAddFeature_IgnoresDuplicates()
    {
        using var db = await UsageDatabase.OpenOrCreateAsync(_fileName);

        var feature = Guid.NewGuid();
        Assert.True(await db.TryAddFeatureAsync(feature));
        Assert.False(await db.TryAddFeatureAsync(feature));

        var results = Assert.Single(await db.GetFeaturesAsync());
        Assert.Equal(feature, results.Feature);
    }

    [Fact]
    public async Task UsageDatabase_DeleteFeature_RemovesIt()
    {
        using var db = await UsageDatabase.OpenOrCreateAsync(_fileName);

        var feature1 = Guid.Parse("0125d657-66c9-434b-b64d-b5bfba69d244");
        var feature2 = Guid.Parse("4159727d-da9b-4968-987e-5a22cbbc32b8");
        var feature3 = Guid.Parse("9e56a746-2194-41fa-bd32-f7788e5041c9");
        Assert.True(await db.TryAddFeatureAsync(feature1));
        Assert.True(await db.TryAddFeatureAsync(feature2));
        Assert.True(await db.TryAddFeatureAsync(feature3));

        await db.DeleteFeaturesAsync([feature1, feature3]);

        Assert.True(await db.TryAddFeatureAsync(feature1));
        Assert.True(await db.TryAddFeatureAsync(feature3));

        var actualFeatures = (await db.GetFeaturesAsync()).Select(f => f.Feature).Order();
        Assert.Equal([feature1, feature2, feature3], actualFeatures);
    }

    [Fact]
    public async Task UsageDatabase_GetUsages_ComputesPercentage()
    {
        using var db = await UsageDatabase.OpenOrCreateAsync(_fileName);

        var unit1 = "Unit1";
        var unit2 = "Unit2";
        await db.AddReferenceUnitAsync(unit1);
        await db.AddReferenceUnitAsync(unit2);

        var feature1 = Guid.Parse("8f6f963b-e0ab-4832-8825-cc4564bb9761");
        var feature2 = Guid.Parse("b6dce64b-20d8-4679-a5e6-32f11d65d5dc");
        await db.TryAddFeatureAsync(feature1);
        await db.TryAddFeatureAsync(feature2);

        await db.AddUsageAsync(unit1, feature1);
        await db.AddUsageAsync(unit1, feature2);
        await db.AddUsageAsync(unit2, feature2);

        var expectedUsages = new[] {
            (feature1, 0.5f),
            (feature2, 1.0f),
        };

        var actualUsages = (await db.GetUsagesAsync()).OrderBy(u => u.Feature).ToArray();

        Assert.Equal(expectedUsages, actualUsages);
    }

    [Fact]
    public async Task UsageDatabase_GetUsages_DoesNotReturnUnusedFeatures()
    {
        using var db = await UsageDatabase.OpenOrCreateAsync(_fileName);

        var unit = "Unit";
        await db.AddReferenceUnitAsync(unit);

        var feature1 = Guid.Parse("8f6f963b-e0ab-4832-8825-cc4564bb9761");
        var feature2 = Guid.Parse("b6dce64b-20d8-4679-a5e6-32f11d65d5dc");
        await db.TryAddFeatureAsync(feature1);

        await db.AddUsageAsync(unit, feature1);

        var expectedUsages = new[] {
            (feature1, 1.0f),
            // We expect feature 2 to be omitted
        };

        var actualUsages = (await db.GetUsagesAsync()).OrderBy(u => u.Feature).ToArray();

        Assert.Equal(expectedUsages, actualUsages);
    }

    [Fact]
    public async Task UsageDatabase_GetUsages_ComputesPercentage_AndHonorsVersion()
    {
        using var db = await UsageDatabase.OpenOrCreateAsync(_fileName);

        var unit1_V1 = "Unit1";
        var unit2_V2 = "Unit2";
        await db.AddReferenceUnitAsync(unit1_V1, 1);
        await db.AddReferenceUnitAsync(unit2_V2, 2);

        var feature1_V1 = Guid.Parse("8f6f963b-e0ab-4832-8825-cc4564bb9761");
        var feature2_V2 = Guid.Parse("b6dce64b-20d8-4679-a5e6-32f11d65d5dc");
        var feature3_V1 = Guid.Parse("fb3920f0-eeca-4d09-b9f7-ca4764a8ddb6");
        await db.TryAddFeatureAsync(feature1_V1, 1);
        await db.TryAddFeatureAsync(feature2_V2, 2);
        await db.TryAddFeatureAsync(feature3_V1, 1);

        await db.AddUsageAsync(unit1_V1, feature1_V1);

        await db.AddUsageAsync(unit2_V2, feature2_V2);

        await db.AddUsageAsync(unit1_V1, feature3_V1);
        await db.AddUsageAsync(unit2_V2, feature3_V1);

        var expectedUsages = new (Guid Feature, float Percentage)[] {
            (feature1_V1, 0.5f),
            (feature2_V2, 1.0f),
            (feature3_V1, 1.0f)
        };

        var actualUsages = (await db.GetUsagesAsync()).OrderBy(u => u.Feature).ToArray();

        Assert.Equal(expectedUsages, actualUsages);
    }

    [Fact]
    public async Task UsageDatabase_GetUsagesAsyncWithParents_ComputesPercentage()
    {
        using var db = await UsageDatabase.OpenOrCreateAsync(_fileName);

        var unit1 = "Unit1";
        var unit2 = "Unit2";
        var unit3 = "Unit3";
        var unit4 = "Unit4";
        await db.AddReferenceUnitAsync(unit1);
        await db.AddReferenceUnitAsync(unit2);
        await db.AddReferenceUnitAsync(unit3);
        await db.AddReferenceUnitAsync(unit4);

        var featureRoot = Guid.Parse("8f6f963b-e0ab-4832-8825-cc4564bb9761");
        var featureA = Guid.Parse("b6dce64b-20d8-4679-a5e6-32f11d65d5dc");
        var featureA_B = Guid.Parse("eb4f167d-6543-4e8b-aa89-a374f578f599");
        var featureX = Guid.Parse("fb3920f0-eeca-4d09-b9f7-ca4764a8ddb6");

        await db.TryAddFeatureAsync(featureRoot);
        await db.TryAddFeatureAsync(featureA);
        await db.TryAddFeatureAsync(featureA_B);
        await db.TryAddFeatureAsync(featureX);

        await db.AddUsageAsync(unit1, featureA_B);
        await db.AddUsageAsync(unit2, featureA_B);
        await db.AddUsageAsync(unit3, featureX);
        await db.AddUsageAsync(unit4, featureA);

        var featureRoot_Ancestors = new[] {
            (featureRoot, featureRoot)
        };

        var featureA_Ancestors = new[] {
            (featureA, featureA),
            (featureA, featureRoot)
        };

        var featureA_B_Ancestors = new[] {
            (featureA_B, featureA_B),
            (featureA_B, featureA),
            (featureA_B, featureRoot)
        };

        var featureX_Ancestors = new[] {
            (featureX, featureX),
            (featureX, featureRoot)
        };

        var ancestors = featureRoot_Ancestors
            .Concat(featureA_Ancestors)
            .Concat(featureA_B_Ancestors)
            .Concat(featureX_Ancestors);

        foreach (var (child, parent) in ancestors)
            await db.AddParentFeatureAsync(child, parent);

        var expectedUsages = new[] {
            (featureRoot, 1.0f),
            (featureA, 3/4f),
            (featureA_B, 0.5f),
            (featureX, 1/4f),
        };

        var actualUsages = (await db.GetUsagesAsync()).OrderBy(u => u.Feature).ToArray();

        Assert.Equal(expectedUsages, actualUsages);
    }
}