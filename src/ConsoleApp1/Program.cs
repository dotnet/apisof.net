using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using ApiCatalog;
using ApiCatalog.CatalogModel;

namespace ConsoleApp1
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var catalogDbPath = @"c:\Users\immo\Downloads\apicatalog.db";
            var catalogPath = @"c:\Users\immo\Downloads\apicatalog.dat";

            Console.WriteLine($"Creating {catalogPath}...");
            var stopwatch = Stopwatch.StartNew();
            await ApiCatalogModel.ConvertAsync(catalogDbPath, catalogPath);
            Console.WriteLine($"Conversion took {stopwatch.Elapsed}, {Process.GetCurrentProcess().PeakWorkingSet64:N0} bytes peak working set");

            var catalog = ApiCatalogModel.Load(catalogPath);
            catalog.Dump(catalogPath);
            catalog.GetStatistics().Dump();

            Console.WriteLine("==========================================");
            Console.WriteLine("Availability of System.Collections.Generic");
            Console.WriteLine("==========================================");

            var scgAvailability = catalog.RootApis.Single(a => a.Name == "System.Collections.Generic").Children.Single(a => a.Name == "IAsyncEnumerable<T>").GetAvailability();

            foreach (var row in scgAvailability.Frameworks.GroupBy(fx => fx.Framework.GetFrameworkDisplayString()).OrderBy(x => x.Key))
            {
                Console.WriteLine(row.Key);

                var columns = row.GroupBy(c => c.Framework).OrderBy(x => x.Key.Version);

                foreach (var column in columns)
                {
                    Console.WriteLine($"\t{column.Key.GetVersionDisplayString()}");

                    foreach (var cell in column)
                    {
                        if (cell.IsInBox)
                        {
                            Console.WriteLine($"\t\t{cell.Declaration.Assembly.Name}");
                        }
                        else
                        {
                            Console.WriteLine($"\t\t{cell.Declaration.Assembly.Name}, Package={cell.Package.Name}, LibFolder={cell.PackageFramework.GetShortFolderName()}");
                        }
                    }
                }
            }

            Console.WriteLine("===================================");
            Console.WriteLine("Declarations of IAsyncEnumerable<T>");
            Console.WriteLine("===================================");

            foreach (var scg in catalog.RootApis.Where(a => a.Name == "System.Collections.Generic"))
            {
                var asyncEnumerable = scg.Children.Single(a => a.Name == "IAsyncEnumerable<T>");

                foreach (var d in asyncEnumerable.Declarations)
                {
                    foreach (var fx in d.Assembly.Frameworks)
                        Console.WriteLine($"Framework {fx.Name}, {d.Assembly.Name}");

                    foreach (var (p, fx) in d.Assembly.Packages)
                        Console.WriteLine($"Package {p.Name}, Version={p.Version}, For={fx.Name}, {d.Assembly.Name}");
                }

                Console.WriteLine(asyncEnumerable.Children.First().Declarations.First().GetMarkup());
            }

            Console.WriteLine("=========================================");
            Console.WriteLine("Contents of Microsoft.Bcl.AsyncInterfaces");
            Console.WriteLine("=========================================");

            var asyncInterfaces = catalog.Packages.Single(fx => fx.Name == "Microsoft.Bcl.AsyncInterfaces");
            foreach (var (fx, a) in asyncInterfaces.Assemblies)
            {
                Console.WriteLine($"{fx.Name} -- {a.Name}, Version={a.Version}, PublicKeyToken={a.PublicKeyToken}");

                foreach (var root in a.RootApis)
                    Console.WriteLine($"\t{root.Kind}, {root.Name}");
            }

            Console.WriteLine("==============================");
            Console.WriteLine("Contents of .NET Framework 4.5");
            Console.WriteLine("==============================");

            var net45 = catalog.Frameworks.Single(fx => fx.Name == "net45");
            foreach (var a in net45.Assemblies)
            {
                Console.WriteLine($"{a.Name}, Version={a.Version}, PublicKeyToken={a.PublicKeyToken}");

                foreach (var root in a.RootApis)
                    Console.WriteLine($"\t{root.Kind}, {root.Name}");
            }

            Console.WriteLine("========");
            Console.WriteLine("Packages");
            Console.WriteLine("========");

            foreach (var p in catalog.Packages)
                Console.WriteLine($"{p.Name}, Version={p.Version}");

            Console.WriteLine("==========");
            Console.WriteLine("Assemblies");
            Console.WriteLine("==========");

            foreach (var a in catalog.Assemblies)
                Console.WriteLine($"{a.Name}, Version={a.Version}, PublicKeyToken={a.PublicKeyToken}");
        }
    }
}
