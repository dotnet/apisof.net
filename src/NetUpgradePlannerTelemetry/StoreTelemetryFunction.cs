using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Azure.Storage.Blobs;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NetUpgradePlannerTelemetry;

public sealed class StoreTelemetryFunction
{
    private readonly IConfiguration _configuration;

    public StoreTelemetryFunction(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [FunctionName("store-telemetry")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest request,
        ILogger logger)
    {
        var fingerprintValues = request.Query["fingerprint"];
        if (fingerprintValues.Count != 1)
            return new BadRequestResult();

        var fingerprint = fingerprintValues.Single();
        if (fingerprint.Length != 64)
            return new BadRequestResult();

        foreach (var c in fingerprint)
        {
            var isHex = c >= '0' && c <= '9' ||
                        c >= 'A' && c <= 'F';
            if (!isHex)
                return new BadRequestResult();
        }

        var apis = new List<Guid>();
        using var reader = new StreamReader(request.Body);            
        while (await reader.ReadLineAsync() is string line)
        {
            if (Guid.TryParse(line.Trim(), out var api))
                apis.Add(api);
            else
                return new BadRequestResult();
        }

        if (apis.Count == 0)
            return new BadRequestResult();

        var connectionString = _configuration["AzureStorageConnectionString"];

        var container = "planner";
        var blobName = fingerprint;
        var blobClient = new BlobClient(connectionString, container, blobName);
        var blobText = string.Join(Environment.NewLine, apis);
        var blobData = BinaryData.FromString(blobText);
        await blobClient.UploadAsync(blobData, overwrite: true);

        logger.LogInformation($"Stored {apis.Count} APIs for {fingerprint}.");

        return new OkResult();
    }
}
