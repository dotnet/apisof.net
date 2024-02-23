namespace Terrajobst.ApiCatalog;

// Definition of frameworks
//
// With the advent of shared frameworks and workloads, a framework is
// compromised of packs, which are special NuGet packages. They are special
// because they are implicitly referenced by the SDK.
//
// Packs have multiple different flavors:
//
//    - Framework
//    - Library
//    - Sdk
//    - Template
//
// For the purpose of API Catalog, we can ignore the flavors 'Sdk' and
// 'Template' as they only contribute tooling, not APIs.
//
// Framework packs have a FrameworkList.xml and are basically specially packaged
// reference assemblies. They don't have TFM specific folders and need to be
// indexed differently from how we'd index NuGet packages.
//
// Library packs are just regular NuGet packages. The only special thing here is
// that they are implicitly referenced by the SDK. Hence, we should treat them
// as "in the box". However, since these are regular packages and can contribute
// assets to any framework, so we still need to index them as regular packages
// as well.
//
// I don't think it's particular useful to include which properties / SDKs
// provide a given pack; there is too much variation and this data is virtually
// impossible to extract from the MSBuild files in an automatic fashion and
// since we don't surface it anyway we might as well remove it. However, it
// might be worthwhile to think about having an enum like "reference context"
// that lists a well-known set of reference semantics (e.g. Default, Web,
// UseWinForms, UseWpf, UseMaui). This way, we can statically assign a known
// value to each ref pack, much like how we assign a TFM as we can't easily
// extract that mapping either.
//
// TODO: Right now, the manifest loses the information about packs and
//       workloads. We should figure out how we want to store the information in
//       the catalog and surface the data in the manifest accordingly.
public sealed partial class FrameworkDefinition(string name, bool isPreview = false)
{
    public string Name { get; } = name;

    public bool IsPreview { get; } = isPreview;

    public required IReadOnlyList<PackReference> BuiltInPacks { get; init; }

    public IReadOnlyList<PackReference> WorkloadPacks { get; init; } = [];
}