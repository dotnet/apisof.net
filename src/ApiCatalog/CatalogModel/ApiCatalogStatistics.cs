using System;

namespace ApiCatalog.CatalogModel
{
    public class ApiCatalogStatistics
    {
        public ApiCatalogStatistics(int numberOfApis,
                                    int numberOfDeclarations,
                                    int numberOfAssemblies,
                                    int numberOfFrameworks,
                                    int numberOfFrameworkAssemblies,
                                    int numberOfPackages,
                                    int numberOfPackageVersions,
                                    int numberOfPackageAssemblies)
        {
            NumberOfApis = numberOfApis;
            NumberOfDeclarations = numberOfDeclarations;
            NumberOfAssemblies = numberOfAssemblies;
            NumberOfFrameworks = numberOfFrameworks;
            NumberOfFrameworkAssemblies = numberOfFrameworkAssemblies;
            NumberOfPackages = numberOfPackages;
            NumberOfPackageVersions = numberOfPackageVersions;
            NumberOfPackageAssemblies = numberOfPackageAssemblies;
        }

        public int NumberOfApis { get; }
        public int NumberOfDeclarations { get; }
        public int NumberOfAssemblies { get; }
        public int NumberOfFrameworks { get; }
        public int NumberOfFrameworkAssemblies { get; }
        public int NumberOfPackages { get; }
        public int NumberOfPackageVersions { get; }
        public int NumberOfPackageAssemblies { get; }

        public void Dump()
        {
            Console.WriteLine($"APIs                 : {NumberOfApis,12:N0}");
            Console.WriteLine($"Declarations         : {NumberOfDeclarations,12:N0}");
            Console.WriteLine($"Assemblies           : {NumberOfAssemblies,12:N0}");
            Console.WriteLine($"Frameworks           : {NumberOfFrameworks,12:N0}");
            Console.WriteLine($"Framework assemblies : {NumberOfFrameworkAssemblies,12:N0}");
            Console.WriteLine($"Packages             : {NumberOfPackages,12:N0}");
            Console.WriteLine($"Package versions     : {NumberOfPackageVersions,12:N0}");
            Console.WriteLine($"Package assemblies   : {NumberOfPackageAssemblies,12:N0}");
        }
    }
}
