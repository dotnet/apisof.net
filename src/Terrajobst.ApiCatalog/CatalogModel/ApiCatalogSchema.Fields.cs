namespace Terrajobst.ApiCatalog;

internal static partial class ApiCatalogSchema
{
    // Note: The order of these fields is important due to initialization dependencies.
    //       The correct order is primitives, structures, and then tables.

    // Definitions of array primitives

    public static readonly Field<FrameworkModel> FrameworkElement = new FrameworkField();

    public static readonly Field<AssemblyModel> AssemblyElement = new AssemblyField();

    public static readonly Field<ApiModel> ApiElement = new ApiField();

    // Definitions of array structures

    public static readonly ApiDeclarationLayout ApiDeclarationStructure = new(new LayoutBuilder(c => c.BlobHeap));

    public static readonly ApiUsageLayout ApiUsageStructure = new(new LayoutBuilder(c => c.BlobHeap));

    public static readonly PackageAssemblyTupleLayout PackageAssemblyTuple = new(new LayoutBuilder(c => c.BlobHeap));

    public static readonly AssemblyPackageTupleLayout AssemblyPackageTuple = new(new LayoutBuilder(c => c.BlobHeap));

    public static readonly PlatformIsSupportedTupleLayout PlatformIsSupportedTuple = new(new LayoutBuilder(c => c.BlobHeap));

    // Definition of Tables

    public static readonly PlatformRowLayout PlatformRow = new(new LayoutBuilder(c => c.PlatformTable));
    public static readonly FrameworkRowLayout FrameworkRow = new(new LayoutBuilder(c => c.FrameworkTable));
    public static readonly PackageRowLayout PackageRow = new(new LayoutBuilder(c => c.PackageTable));
    public static readonly AssemblyRowLayout AssemblyRow = new(new LayoutBuilder(c => c.AssemblyTable));
    public static readonly UsageSourceRowLayout UsageSourceRow = new(new LayoutBuilder(c => c.UsageSourceTable));
    public static readonly ApiRowLayout ApiRow = new(new LayoutBuilder(c => c.ApiTable));
    public static readonly RootApiRowLayout RootApiRow = new(new LayoutBuilder(c => c.RootApiTable));
    public static readonly ExtensionMethodRowLayout ExtensionMethodRow = new(new LayoutBuilder(c => c.ExtensionMethodTable));
    public static readonly ObsoletionRowLayout ObsoletionRow = new(new LayoutBuilder(c => c.ObsoletionTable));
    public static readonly PlatformSupportRowLayout PlatformSupportRow = new(new LayoutBuilder(c => c.PlatformSupportTable));
    public static readonly PreviewRequirementRowLayout PreviewRequirementRow = new(new LayoutBuilder(c => c.PreviewRequirementTable));
    public static readonly ExperimentalRowLayout ExperimentalRow = new(new LayoutBuilder(c => c.ExperimentalTable));
}
