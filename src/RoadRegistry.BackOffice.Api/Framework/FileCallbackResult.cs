namespace RoadRegistry.BackOffice.Api.Framework;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

// source: https://blog.stephencleary.com/2016/11/streaming-zip-on-aspnet-core.html
public class FileCallbackResult : FileResult
{
    private readonly Func<Stream, ActionContext, Task> _callback;

    public FileCallbackResult(FileResponse fileResponse)
        : this(fileResponse.MediaTypeHeaderValue, async (stream, actionContext) => await fileResponse.Callback(stream, CancellationToken.None))
    {
        FileDownloadName = fileResponse.FileName ?? throw new ArgumentNullException(nameof(fileResponse.FileName));
    }

    public FileCallbackResult(MediaTypeHeaderValue contentType, Func<Stream, ActionContext, Task> callback)
        : base(contentType?.ToString())
    {
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    public override Task ExecuteResultAsync(ActionContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
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
            var unknownContentLength = (long?)null;
            SetHeadersAndLog(context, result, unknownContentLength, false);
            return result._callback(context.HttpContext.Response.Body, context);
        }
    }
}
