using System.Text;

namespace Terrajobst.ApiCatalog;

public class ApiCatalogStatistics
{
    public ApiCatalogStatistics(int sizeOnDisk,
        int sizeInMemory,
        int numberOfApis,
        int numberOfDeclarations,
        int numberOfAssemblies,
        int numberOfFrameworks,
        int numberOfFrameworkAssemblies,
        int numberOfPackages,
        int numberOfPackageVersions,
        int numberOfPackageAssemblies)
    {
        SizeOnDisk = sizeOnDisk;
        SizeInMemory = sizeInMemory;
        NumberOfApis = numberOfApis;
        NumberOfDeclarations = numberOfDeclarations;
        NumberOfAssemblies = numberOfAssemblies;
        NumberOfFrameworks = numberOfFrameworks;
        NumberOfFrameworkAssemblies = numberOfFrameworkAssemblies;
        NumberOfPackages = numberOfPackages;
        NumberOfPackageVersions = numberOfPackageVersions;
        NumberOfPackageAssemblies = numberOfPackageAssemblies;
    }

    public int SizeOnDisk { get; }
    public int SizeInMemory { get; }
    public int NumberOfApis { get; }
    public int NumberOfDeclarations { get; }
    public int NumberOfAssemblies { get; }
    public int NumberOfFrameworks { get; }
    public int NumberOfFrameworkAssemblies { get; }
    public int NumberOfPackages { get; }
    public int NumberOfPackageVersions { get; }
    public int NumberOfPackageAssemblies { get; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Size on disk         : {SizeOnDisk,12:N0} bytes");
        sb.AppendLine($"Size in memory       : {SizeInMemory,12:N0} bytes");
        sb.AppendLine($"APIs                 : {NumberOfApis,12:N0}");
        sb.AppendLine($"Declarations         : {NumberOfDeclarations,12:N0}");
        sb.AppendLine($"Assemblies           : {NumberOfAssemblies,12:N0}");
        sb.AppendLine($"Frameworks           : {NumberOfFrameworks,12:N0}");
        sb.AppendLine($"Framework assemblies : {NumberOfFrameworkAssemblies,12:N0}");
        sb.AppendLine($"Packages             : {NumberOfPackages,12:N0}");
        sb.AppendLine($"Package versions     : {NumberOfPackageVersions,12:N0}");
        sb.AppendLine($"Package assemblies   : {NumberOfPackageAssemblies,12:N0}");
        return sb.ToString();
    }
}