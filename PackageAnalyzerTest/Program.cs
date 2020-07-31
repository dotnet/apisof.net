using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration.UserSecrets;

using Newtonsoft.Json;

using PackageIndexing;

namespace PackageAnalyzerTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //var id = "System.Collections.Immutable";
            //var version = "5.0.0-preview.7.20364.11";
            var secrets = UserSecrets.Load();
            var id = "System.Memory";
            var version = "4.5.4";
            var stopwatch = Stopwatch.StartNew();
            await Indexer.Index(id, version, secrets.SqlConnectionString);
            Console.WriteLine($"Completed in {stopwatch.Elapsed}");
        }
    }

    class UserSecrets
    {
        public string SqlConnectionString { get; set; }

        public static UserSecrets Load()
        {
            var path = PathHelper.GetSecretsPathFromSecretsId("a65cd530-6c72-4fa1-a7d6-002260365e65");
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<UserSecrets>(json);
        }
    }
}
