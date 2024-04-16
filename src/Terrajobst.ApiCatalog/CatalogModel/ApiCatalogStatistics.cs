using System.Text;

namespace Terrajobst.ApiCatalog;

public sealed class ApiCatalogStatistics
{
    public ApiCatalogStatistics(int sizeCompressed,
                                int sizeUncompressed,
                                int numberOfApis,
                                int numberOfExtensionMethods,
                                int numberOfDeclarations,
                                int numberOfAssemblies,
                                int numberOfFrameworks,
                                int numberOfFrameworkAssemblies,
                                int numberOfPackages,
                                int numberOfPackageVersions,
                                int numberOfPackageAssemblies,
                                IReadOnlyCollection<(string TableName, int Bytes, int Rows)> tableSizes)
    {
        ThrowIfNegative(sizeCompressed);
        ThrowIfNegative(sizeUncompressed);
        ThrowIfNegative(numberOfApis);
        ThrowIfNegative(numberOfExtensionMethods);
        ThrowIfNegative(numberOfDeclarations);
        ThrowIfNegative(numberOfAssemblies);
        ThrowIfNegative(numberOfFrameworks);
        ThrowIfNegative(numberOfFrameworkAssemblies);
        ThrowIfNegative(numberOfPackages);
        ThrowIfNegative(numberOfPackageVersions);
        ThrowIfNegative(numberOfPackageAssemblies);
        ThrowIfNull(tableSizes);

        SizeCompressed = sizeCompressed;
        SizeUncompressed = sizeUncompressed;
        NumberOfApis = numberOfApis;
        NumberOfExtensionMethods = numberOfExtensionMethods;
        NumberOfDeclarations = numberOfDeclarations;
        NumberOfAssemblies = numberOfAssemblies;
        NumberOfFrameworks = numberOfFrameworks;
        NumberOfFrameworkAssemblies = numberOfFrameworkAssemblies;
        NumberOfPackages = numberOfPackages;
        NumberOfPackageVersions = numberOfPackageVersions;
        NumberOfPackageAssemblies = numberOfPackageAssemblies;
        TableSizes = tableSizes;
    }

    public int SizeCompressed { get; }
    public int SizeUncompressed { get; }
    public int NumberOfApis { get; }
    public int NumberOfExtensionMethods { get; }
    public int NumberOfDeclarations { get; }
    public int NumberOfAssemblies { get; }
    public int NumberOfFrameworks { get; }
    public int NumberOfFrameworkAssemblies { get; }
    public int NumberOfPackages { get; }
    public int NumberOfPackageVersions { get; }
    public int NumberOfPackageAssemblies { get; }
    public IReadOnlyCollection<(string TableName, int Bytes, int Rows)> TableSizes { get; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Size Compressed           : {SizeCompressed,12:N0} bytes");
        sb.AppendLine($"Size Uncompressed         : {SizeUncompressed,12:N0} bytes");
        sb.AppendLine($"APIs                      : {NumberOfApis,12:N0}");
        sb.AppendLine($"Extension methods         : {NumberOfExtensionMethods,12:N0}");
        sb.AppendLine($"Declarations              : {NumberOfDeclarations,12:N0}");
        sb.AppendLine($"Assemblies                : {NumberOfAssemblies,12:N0}");
        sb.AppendLine($"Frameworks                : {NumberOfFrameworks,12:N0}");
        sb.AppendLine($"Framework assemblies      : {NumberOfFrameworkAssemblies,12:N0}");
        sb.AppendLine($"Packages                  : {NumberOfPackages,12:N0}");
        sb.AppendLine($"Package versions          : {NumberOfPackageVersions,12:N0}");
        sb.AppendLine($"Package assemblies        : {NumberOfPackageAssemblies,12:N0}");

        foreach (var tableSize in TableSizes)
        {
            sb.AppendLine($"{tableSize.TableName,-25} : {tableSize.Bytes,12:N0} bytes");
            if (tableSize.Rows >= 0)
                sb.AppendLine($"{tableSize.TableName,-25} : {tableSize.Rows,12:N0} rows");
        }

        return sb.ToString();
    }
}