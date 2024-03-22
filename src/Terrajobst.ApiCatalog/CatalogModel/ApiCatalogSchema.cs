namespace Terrajobst.ApiCatalog;

internal static partial class ApiCatalogSchema
{
    internal static ReadOnlySpan<byte> MagicNumber => "APICATFB"u8;

    internal const int FormatVersion = 7;
    internal const int NumberOfTables = 14;

    // Tables

    public sealed class PlatformRowLayout(LayoutBuilder t) : TableLayout
    {
        public readonly Field<string> Name = t.DefineString();
        public override int Size { get; } = t.Size;
    }

    public sealed class FrameworkRowLayout(LayoutBuilder t) : TableLayout
    {
        public readonly Field<string> Name = t.DefineString();
        public readonly Field<ArrayEnumerator<AssemblyModel>> Assemblies = t.DefineArray(AssemblyElement);
        public override int Size { get; } = t.Size;
    }

    public sealed class PackageRowLayout(LayoutBuilder t) : TableLayout
    {
        public readonly Field<string> Name = t.DefineString();
        public readonly Field<string> Version = t.DefineString();
        public readonly Field<ArrayOfStructuresEnumerator<PackageAssemblyTupleLayout>> Assemblies = t.DefineArray(PackageAssemblyTuple);
        public override int Size { get; } = t.Size;
    }

    public sealed class AssemblyRowLayout(LayoutBuilder t) : TableLayout
    {
        public readonly Field<Guid> Guid = t.DefineGuid();
        public readonly Field<string> Name = t.DefineString();
        public readonly Field<string> PublicKeyToken = t.DefineString();
        public readonly Field<string> Version = t.DefineString();
        public readonly Field<ArrayEnumerator<ApiModel>> RootApis = t.DefineArray(ApiElement);
        public readonly Field<ArrayEnumerator<FrameworkModel>> Frameworks = t.DefineArray(FrameworkElement);
        public readonly Field<ArrayOfStructuresEnumerator<AssemblyPackageTupleLayout>> Packages = t.DefineArray(AssemblyPackageTuple);
        public override int Size { get; } = t.Size;
    }

    public sealed class UsageSourceRowLayout(LayoutBuilder t) : TableLayout
    {
        public readonly Field<string> Name = t.DefineString();
        public readonly Field<DateOnly> Date = t.DefineDate();
        public override int Size { get; } = t.Size;
    }

    public sealed class ApiRowLayout(LayoutBuilder t) : TableLayout
    {
        public readonly Field<Guid> Guid = t.DefineGuid();
        public readonly Field<ApiKind> Kind = t.DefineApiKind();
        public readonly Field<ApiModel?> Parent = t.DefineOptionalApi();
        public readonly Field<string> Name = t.DefineString();
        public readonly Field<ArrayEnumerator<ApiModel>> Children = t.DefineArray(ApiElement);
        public readonly Field<ArrayOfStructuresEnumerator<ApiDeclarationLayout>> Declarations = t.DefineArray(ApiDeclarationStructure);
        public readonly Field<ArrayOfStructuresEnumerator<ApiUsageLayout>> Usages = t.DefineArray(ApiUsageStructure);
        public override int Size { get; } = t.Size;
    }

    public sealed class RootApiRowLayout(LayoutBuilder t) : TableLayout
    {
        public readonly Field<ApiModel> Api = t.DefineApi();
        public override int Size { get; } = t.Size;
    }

    public sealed class ExtensionMethodRowLayout(LayoutBuilder t) : TableLayout
    {
        public readonly Field<Guid> Guid = t.DefineGuid();
        public readonly Field<ApiModel> ExtendedType = t.DefineApi();
        public readonly Field<ApiModel> ExtensionMethod = t.DefineApi();
        public override int Size { get; } = t.Size;
    }

    public sealed class ObsoletionRowLayout(LayoutBuilder t) : TableLayout
    {
        public readonly Field<ApiModel?> Api = t.DefineOptionalApi();
        public readonly Field<AssemblyModel> Assembly = t.DefineAssembly();
        public readonly Field<string> Message = t.DefineString();
        public readonly Field<bool> IsError = t.DefineBoolean();
        public readonly Field<string> DiagnosticId = t.DefineString();
        public readonly Field<string> UrlFormat = t.DefineString();
        public override int Size { get; } = t.Size;
    }

    public sealed class PlatformSupportRowLayout(LayoutBuilder t) : TableLayout
    {
        public readonly Field<ApiModel?> Api = t.DefineOptionalApi();
        public readonly Field<AssemblyModel> Assembly = t.DefineAssembly();
        public readonly Field<ArrayOfStructuresEnumerator<PlatformIsSupportedTupleLayout>> Platforms = t.DefineArray(PlatformIsSupportedTuple);
        public override int Size { get; } = t.Size;
    }

    public sealed class PreviewRequirementRowLayout(LayoutBuilder t) : TableLayout
    {
        public readonly Field<ApiModel?> Api = t.DefineOptionalApi();
        public readonly Field<AssemblyModel> Assembly = t.DefineAssembly();
        public readonly Field<string> Message = t.DefineString();
        public readonly Field<string> Url = t.DefineString();
        public override int Size { get; } = t.Size;
    }

    public sealed class ExperimentalRowLayout(LayoutBuilder t) : TableLayout
    {
        public readonly Field<ApiModel?> Api = t.DefineOptionalApi();
        public readonly Field<AssemblyModel> Assembly = t.DefineAssembly();
        public readonly Field<string> DiagnosticId = t.DefineString();
        public readonly Field<string> UrlFormat = t.DefineString();
        public override int Size { get; } = t.Size;
    }

    // Structures

    public sealed class ApiDeclarationLayout(LayoutBuilder t) : StructureLayout
    {
        public Field<AssemblyModel> Assembly { get; } = t.DefineAssembly();
        public Field<int> SyntaxOffset { get; } = t.DefineInt32();
        public override int Size { get; } = t.Size;
    }

    public sealed class ApiUsageLayout(LayoutBuilder t) : StructureLayout
    {
        public Field<UsageSourceModel> UsageSource { get; } = t.DefineUsageSource();
        public Field<float> Percentage { get; } = t.DefineSingle();
        public override int Size { get; } = t.Size;
    }

    public sealed class PackageAssemblyTupleLayout(LayoutBuilder t) : StructureLayout
    {
        public Field<FrameworkModel> Framework { get; } = t.DefineFramework();
        public Field<AssemblyModel> Assembly { get; } = t.DefineAssembly();
        public override int Size { get; } = t.Size;
    }

    public sealed class AssemblyPackageTupleLayout(LayoutBuilder t) : StructureLayout
    {
        public Field<PackageModel> Package { get; } = t.DefinePackage();
        public Field<FrameworkModel> Framework { get; } = t.DefineFramework();
        public override int Size { get; } = t.Size;
    }

    public sealed class PlatformIsSupportedTupleLayout(LayoutBuilder t) : StructureLayout
    {
        public Field<string> Platform { get; } = t.DefineString();
        public Field<bool> IsSupported { get; } = t.DefineBoolean();
        public override int Size { get; } = t.Size;
    }
}
