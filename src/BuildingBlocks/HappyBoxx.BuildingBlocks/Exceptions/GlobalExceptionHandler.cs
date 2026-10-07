using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HappyBoxx.BuildingBlocks.Exceptions;

internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "The resource was modified by another request. Reload and try again."),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "The request is invalid."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandledException(logger, exception);
        }

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = statusCode, Title = title },
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception while processing the request")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception);
}
