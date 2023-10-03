using System.Net;
using System.Web;

using Azure.Storage.Blobs;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NetUpgradePlannerTelemetry;

public sealed class StoreTelemetryFunction
{
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;

    public StoreTelemetryFunction(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<StoreTelemetryFunction>();
        _configuration = configuration;
    }

    [Function("store-telemetry")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData request)
    {
        var query = HttpUtility.ParseQueryString(request.Url.Query);
        var fingerprintValues = query.GetValues("fingerprint");
        if (fingerprintValues?.Length != 1)
            return request.CreateResponse(HttpStatusCode.BadRequest);

        var fingerprint = fingerprintValues.Single();
        if (fingerprint.Length != 64)
            return request.CreateResponse(HttpStatusCode.BadRequest);

        foreach (var c in fingerprint)
        {
            var isHex = c >= '0' && c <= '9' ||
                        c >= 'A' && c <= 'F';
            if (!isHex)
                return request.CreateResponse(HttpStatusCode.BadRequest);
        }

        var apis = new List<Guid>();
        using var reader = new StreamReader(request.Body);
        while (await reader.ReadLineAsync() is string line)
        {
            if (Guid.TryParse(line.Trim(), out var api))
                apis.Add(api);
            else
                return request.CreateResponse(HttpStatusCode.BadRequest);
        }

        if (apis.Count == 0)
            return request.CreateResponse(HttpStatusCode.BadRequest);

        var connectionString = _configuration["AzureStorageConnectionString"];

        var container = "planner";
        var blobName = fingerprint;
        var blobClient = new BlobClient(connectionString, container, blobName);
        var blobText = string.Join(Environment.NewLine, apis);
        var blobData = BinaryData.FromString(blobText);
        await blobClient.UploadAsync(blobData, overwrite: true);

        _logger.LogInformation($"Stored {apis.Count} APIs for {fingerprint}.");

        return request.CreateResponse(HttpStatusCode.OK);
    }
}
