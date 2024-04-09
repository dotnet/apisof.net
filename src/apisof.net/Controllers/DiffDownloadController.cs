using ApisOfDotNet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Net.Http.Headers;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Controllers;

[ApiController]
[Route("/catalog/download/diff")]
[AllowAnonymous]
public sealed class DiffDownloadController : Controller
{
    private readonly CatalogService _catalogService;

    public DiffDownloadController(CatalogService catalogService)
    {
        ThrowIfNull(catalogService);

        _catalogService = catalogService;
    }

    [HttpGet]
    public ActionResult Get([FromQuery] string diff)
    {
        var catalog = _catalogService.Catalog;

        var diffParameter = DiffParameter.Parse(catalog, diff);
        if (diffParameter is null)
            return new BadRequestResult();

        var left = diffParameter.Value.Left;
        var right = diffParameter.Value.Right;
        var name = $"{left.GetShortFolderName()}-vs-{right.GetShortFolderName()}.diff";

        return new FileCallbackResult(new MediaTypeHeaderValue("plain/text"), async (outputStream, _) =>
        {
            var diffWriter = new DiffWriter(catalog, left, right);
            await diffWriter.WriteToAsync(outputStream);
        })
        {
            FileDownloadName = name
        };
    }

    private sealed class FileCallbackResult : FileResult
    {
        private readonly Func<Stream, ActionContext, Task> _callback;

        public FileCallbackResult(MediaTypeHeaderValue contentType, Func<Stream, ActionContext, Task> callback)
            : base(contentType.ToString())
        {
            ThrowIfNull(callback);

            _callback = callback;
        }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            ThrowIfNull(context);

            var executor = new FileCallbackResultExecutor(context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>());
            return executor.ExecuteAsync(context, this);
        }

        private sealed class FileCallbackResultExecutor : FileResultExecutorBase
        {
            public FileCallbackResultExecutor(ILoggerFactory loggerFactory)
                : base(CreateLogger<FileCallbackResultExecutor>(loggerFactory))
            {
            }

            public Task ExecuteAsync(ActionContext context, FileCallbackResult result)
            {
                SetHeadersAndLog(context, result, null, false);
                return result._callback(context.HttpContext.Response.Body, context);
            }
        }
    }
}