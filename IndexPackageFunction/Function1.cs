using System;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using PackageIndexing;

namespace IndexPackageFunction
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task Run([QueueTrigger("package-queue", Connection = "")]string messageJson,
                                     ILogger log)
        {
            var sqlConnectionString = Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTIONSTRING");
            var message = JsonConvert.DeserializeObject<PackageQueueMessage>(messageJson);
            //await Indexer.Index(message.PackageId, message.PackageVersion, sqlConnectionString);
        }

        public class PackageQueueMessage
        {
            public string PackageId { get; set; }
            public string PackageVersion { get; set; }
        }
    }
}
