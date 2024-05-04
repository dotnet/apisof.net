using ApisOfDotNet.Services;
using Microsoft.AspNetCore.Components;
using NuGet.Frameworks;

using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Shared;

public partial class FrameworkPicker
{
    [Inject]
    public required CatalogService CatalogService { get; set; }

    [Parameter]
    public NuGetFramework? Selected { get; set; }

    [Parameter]
    public EventCallback<NuGetFramework?> SelectedChanged { get; set; }

    private FrameworkModel Model { get; set; } = null!;

    protected override void OnInitialized()
    {
        var frameworksNew = CatalogService.Catalog.Frameworks.Select(fx => fx.NuGetFramework).Where(fx => fx.IsRelevantForCatalog());
        Model = new FrameworkModel(frameworksNew);
        Model.Changed += Model_Changed;
    }

    protected override void OnParametersSet()
    {
        if (Selected is not null)
            Model.SelectFramework(Selected);
    }

    private async void Model_Changed(object? sender, EventArgs e)
    {
        var selected = Model.GetSelectedFramework();
        if (Selected != selected)
        {
            Selected = selected;
            await SelectedChanged.InvokeAsync(Selected);
        }
    }

    private sealed class FrameworkModel
    {
        public FrameworkModel(IEnumerable<NuGetFramework> frameworks)
        {
            FamilyByValue = frameworks
                .GroupBy(fx => fx.GetFrameworkDisplayString())
                .Select(g => new FrameworkFamily(g.Key, g))
                .ToDictionary(ff => ff.Value);

            var selectedFramework = frameworks.First();
            SelectFramework(selectedFramework);
        }

        public IReadOnlyDictionary<string, FrameworkFamily> FamilyByValue { get; }

        public IEnumerable<FrameworkFamily> Families => FamilyByValue.Values.Order();

        public IEnumerable<FrameworkVersion> Versions => SelectedFamily.Versions;

        public IEnumerable<FrameworkPlatform> Platforms => SelectedVersion.Platforms;

        public IEnumerable<FrameworkPlatformVersion> PlatformVersions => SelectedPlatform.Versions;

        public FrameworkFamily SelectedFamily { get; private set; } = null!;

        public FrameworkVersion SelectedVersion { get; private set; } = null!;

        public FrameworkPlatform SelectedPlatform { get; private set; } = null!;

        public FrameworkPlatformVersion? SelectedPlatformVersion { get; private set; } = null!;

        public string SelectedFamilyValue
        {
            get => SelectedFamily.Value;
            set
            {
                SelectFamily(value);
                OnChanged();
            }
        }

        public string SelectedVersionValue
        {
            get => SelectedVersion.Value;
            set
            {
                SelectVersion(value);
                OnChanged();
            }
        }

        public string SelectedPlatformValue
        {
            get => SelectedPlatform.Value;
            set
            {
                SelectPlatform(value);
                OnChanged();
            }
        }

        public string? SelectedPlatformVersionValue
        {
            get => SelectedPlatformVersion?.Value;
            set
            {
                SelectPlatformVersion(value);
                OnChanged();
            }
        }

        public void SelectFramework(NuGetFramework? framework)
        {
            if (framework is null)
                return;

            var familyValue = framework.GetFrameworkDisplayString();
            if (!FamilyByValue.TryGetValue(familyValue, out var family))
                return;

            SelectedFamily = family;

            var versionValue = framework.Version.GetVersionDisplayString();
            if (!family.VersionByValue.TryGetValue(versionValue, out var version))
                return;

            SelectedVersion = version;

            if (!framework.HasPlatform)
            {
                SelectedPlatform = FrameworkPlatform.Empty;
                SelectedPlatformVersion = null;
                return;
            }

            var platformValue = framework.Platform;
            if (!version.PlatformByValue.TryGetValue(platformValue, out var platform))
                return;

            SelectedPlatform = platform;

            var platformVersionValue = framework.PlatformVersion.GetVersionDisplayString();
            if (!platform.VersionByValue.TryGetValue(platformVersionValue, out var platformVersion))
                return;

            SelectedPlatformVersion = platformVersion;
            OnChanged();
        }

        public NuGetFramework GetSelectedFramework()
        {
            var frameworkName = SelectedFamily.FrameworkName;
            var frameworkVersion = SelectedVersion.Version;
            var hasPlatform = SelectedPlatform != FrameworkPlatform.Empty;
            if (!hasPlatform)
                return new NuGetFramework(frameworkName, frameworkVersion);

            var platform = SelectedPlatform.Name;
            var platformVersion = SelectedPlatformVersion!.Version;
            return new NuGetFramework(frameworkName, frameworkVersion, platform, platformVersion);
        }

        private void SelectFamily(string value)
        {
            if (!FamilyByValue.TryGetValue(value, out var family))
                return;

            var newVersion = SelectedVersion is not null && family.VersionByValue.ContainsKey(SelectedVersion.Value)
                ? SelectedVersion.Value
                : family.Versions.First().Value;

            SelectedFamily = family;
            SelectVersion(newVersion);
        }

        private void SelectVersion(string value)
        {
            if (SelectedFamily is null || !SelectedFamily.VersionByValue.TryGetValue(value, out var version))
                return;

            var newPlatform = SelectedPlatform is not null && version.PlatformByValue.ContainsKey(SelectedPlatform.Value)
                ? SelectedPlatform.Value
                : version.Platforms.First().Value;

            SelectedVersion = version;
            SelectPlatform(newPlatform);
        }

        private void SelectPlatform(string value)
        {
            if (SelectedVersion is null || !SelectedVersion.PlatformByValue.TryGetValue(value, out var platform))
                return;

            var newPlatformVersion = SelectedPlatformVersion is not null && platform.VersionByValue.ContainsKey(SelectedPlatformVersion.Value)
                ? SelectedPlatformVersion.Value
                : platform.Versions.FirstOrDefault()?.Value;

            SelectedPlatform = platform;
            SelectPlatformVersion(newPlatformVersion);
        }

        private void SelectPlatformVersion(string? value)
        {
            if (value is null)
            {
                SelectedPlatformVersion = null;
                return;
            }

            if (SelectedPlatform is null || !SelectedPlatform.VersionByValue.TryGetValue(value, out var version))
                return;

            SelectedPlatformVersion = version;
        }

        private void OnChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? Changed;
    }

    private sealed class FrameworkFamily : IComparable<FrameworkFamily>
    {
        public FrameworkFamily(string name, IEnumerable<NuGetFramework> frameworks)
        {
            Name = name;
            FrameworkName = frameworks.First().Framework;
            VersionByValue = frameworks
                .GroupBy(fx => fx.Version)
                .Select(g => new FrameworkVersion(g.Key, g))
                .ToDictionary(fv => fv.Value);
        }

        public string Value => Name;
        public string Name { get; }
        public string FrameworkName { get; }
        public IReadOnlyDictionary<string, FrameworkVersion> VersionByValue { get; }
        public IEnumerable<FrameworkVersion> Versions => VersionByValue.Values.OrderDescending();

        public int CompareTo(FrameworkFamily? other)
        {
            return Name.CompareTo(other?.Name);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    private sealed class FrameworkVersion : IComparable<FrameworkVersion>
    {
        public FrameworkVersion(Version version, IEnumerable<NuGetFramework> frameworks)
        {
            Version = version;
            VersionDisplay = version.GetVersionDisplayString();
            PlatformByValue = frameworks
                .Where(fx => fx.HasPlatform)
                .GroupBy(fx => fx.Platform)
                .Select(g => new FrameworkPlatform(g.Key, g))
                .Prepend(FrameworkPlatform.Empty)
                .ToDictionary(fp => fp.Value);
        }

        public string Value => VersionDisplay;
        public Version Version { get; }
        public string VersionDisplay { get; }
        public IReadOnlyDictionary<string, FrameworkPlatform> PlatformByValue { get; }
        public IEnumerable<FrameworkPlatform> Platforms => PlatformByValue.Values.Order();

        public int CompareTo(FrameworkVersion? other)
        {
            return Version.CompareTo(other?.Version);
        }

        public override string ToString()
        {
            return VersionDisplay;
        }
    }

    private sealed class FrameworkPlatform : IComparable<FrameworkPlatform>
    {
        public static FrameworkPlatform Empty { get; } = new("(Cross Platform)", Array.Empty<NuGetFramework>());

        public FrameworkPlatform(string name, IEnumerable<NuGetFramework> frameworks)
        {
            Name = name;
            NameDisplay = PlatformAnnotationEntry.FormatPlatform(name);
            VersionByValue = frameworks
                .Select(fx => new FrameworkPlatformVersion(fx))
                .ToDictionary(fpv => fpv.Value);
        }

        public string Value => Name;
        public string Name { get; }
        public string NameDisplay { get; }
        public IReadOnlyDictionary<string, FrameworkPlatformVersion> VersionByValue { get; }
        public IEnumerable<FrameworkPlatformVersion> Versions => VersionByValue.Values.OrderDescending();

        public int CompareTo(FrameworkPlatform? other)
        {
            return Name.CompareTo(other?.Name);
        }

        public override string ToString()
        {
            return NameDisplay;
        }
    }

    private sealed class FrameworkPlatformVersion : IComparable<FrameworkPlatformVersion>
    {
        public FrameworkPlatformVersion(NuGetFramework framework)
        {
            Version = framework.PlatformVersion;
            VersionDisplay = framework.PlatformVersion.GetVersionDisplayString();
            Framework = framework;
        }

        public string Value => VersionDisplay;
        public Version Version { get; }
        public string VersionDisplay { get; }
        public NuGetFramework Framework { get; }

        public int CompareTo(FrameworkPlatformVersion? other)
        {
            return Version.CompareTo(other?.Version);
        }

        public override string ToString()
        {
            return VersionDisplay;
        }
    }
}