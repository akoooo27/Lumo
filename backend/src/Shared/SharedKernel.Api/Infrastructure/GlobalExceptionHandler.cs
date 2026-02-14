using System.Diagnostics;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SharedKernel.Api.Infrastructure;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        Activity? activity = httpContext.Features.Get<IHttpActivityFeature>()?.Activity;

        string title = "An unexpected error occurred.";

        string type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";

        logger.LogError(exception,
            "An unexpected error occurred while processing the request. RequestId: {RequestId}, TraceId: {TraceId}",
            httpContext.TraceIdentifier, activity?.Id);

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Type = type,
                Title = title,
                Detail = "An unexpected error occurred",
                Instance = $"{httpContext.Request.Method}:{httpContext.Request.Path}",
                Status = httpContext.Response.StatusCode,
                Extensions = new Dictionary<string, object?>
                {
                    ["requestId"] = httpContext.TraceIdentifier,
                    ["traceId"] = activity?.Id
                }
            }
        });
    }
}