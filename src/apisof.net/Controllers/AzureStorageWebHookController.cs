using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Nodes;
using ApisOfDotNet.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApisOfDotNet.Controllers;

[ApiController]
[Route("azure-storage-web-hook")]
[AllowAnonymous]
public class AzureStorageWebHookController : Controller
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<AzureStorageWebHookController> _logger;
    private readonly CatalogService _catalogService;

    public AzureStorageWebHookController(ILogger<AzureStorageWebHookController> logger,
                                         CatalogService catalogService)
    {
        ThrowIfNull(logger);
        ThrowIfNull(catalogService);

        _logger = logger;
        _catalogService = catalogService;
    }

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        var contentType = new ContentType(Request.ContentType ?? string.Empty);
        if (contentType.MediaType != MediaTypeNames.Application.Json)
        {
            _logger.LogError("Received invalid content type {0}", contentType.MediaType);
            return BadRequest();
        }

        var eventType = HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault();
        if (string.IsNullOrEmpty(eventType))
        {
            _logger.LogError("Event type header is missing");
            return BadRequest();
        }

        GridEvent? payload;
        try
        {
            payload = await Request.ReadFromJsonAsync<GridEvent>(JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Can't deserialize GridEvent: {ex}");
            return BadRequest();
        }

        if (payload is null)
            return BadRequest();

        return eventType switch {
            "SubscriptionValidation" => HandleSubscriptionValidation(payload),
            "Notification" => HandleNotification(payload),
            _ => Ok()
        };
    }

    private IActionResult HandleSubscriptionValidation(GridEvent payload)
    {
        var subscriptionData = payload.Data.Deserialize<SubscriptionValidationData>();
        var validationCode = subscriptionData?.ValidationCode;
        if (string.IsNullOrEmpty(validationCode))
            return BadRequest();

        _logger.LogInformation("Received subscription confirmation request with code {0}", validationCode);
        var response = new SubscriptionValidationResponse(validationCode);
        return Json(response, JsonOptions);
    }

    private IActionResult HandleNotification(GridEvent payload)
    {
        _logger.LogInformation("Received event notification");
        _catalogService.HandleBlobChange(payload.Subject);
        return Ok();
    }

    private sealed class GridEvent
    {
        public GridEvent(string id,
                         string eventType,
                         string topic,
                         string subject,
                         DateTime eventTime,
                         JsonObject data)
        {
            ThrowIfNull(id);
            ThrowIfNull(eventType);
            ThrowIfNull(topic);
            ThrowIfNull(subject);
            ThrowIfNull(data);

            Id = id;
            EventType = eventType;
            Topic = topic;
            Subject = subject;
            EventTime = eventTime;
            Data = data;
        }

        public string Id { get; }
        public string EventType { get; }
        public string Topic { get; }
        public string Subject {get; }
        public DateTime EventTime { get; }
        public JsonObject Data { get; }
    }

    private sealed class SubscriptionValidationData
    {
        public SubscriptionValidationData(string validationCode, string validationUrl)
        {
            ThrowIfNullOrEmpty(validationCode);
            ThrowIfNullOrEmpty(validationUrl);

            ValidationCode = validationCode;
            ValidationUrl = validationUrl;
        }

        public string ValidationCode { get; }

        public string ValidationUrl { get; }
    }

    private sealed class SubscriptionValidationResponse
    {
        public SubscriptionValidationResponse(string validationResponse)
        {
            ThrowIfNullOrEmpty(validationResponse);

            ValidationResponse = validationResponse;
        }

        public string ValidationResponse { get; }
    }
}
