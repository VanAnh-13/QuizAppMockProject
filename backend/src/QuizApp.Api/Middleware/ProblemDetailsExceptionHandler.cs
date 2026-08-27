using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QuizApp.Application.Common.Exceptions;

namespace QuizApp.Api.Middleware;

/// <summary>
/// Maps domain errors to 400/401/403/404/409 and unexpected errors to 500,
/// always producing RFC 7807 ProblemDetails with a correlation id.
/// </summary>
public class ProblemDetailsExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ProblemDetailsExceptionHandler> _logger;

    public ProblemDetailsExceptionHandler(ILogger<ProblemDetailsExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        int statusCode;
        string title;
        string detail;
        IDictionary<string, string[]>? errors = null;

        switch (exception)
        {
            case AppException appException:
                statusCode = appException.StatusCode;
                title = appException switch
                {
                    ValidationFailedException => "Validation failed",
                    ConflictException => "Conflict",
                    NotFoundException => "Not found",
                    ForbiddenException => "Forbidden",
                    UnauthorizedException => "Unauthorized",
                    _ => "Bad request"
                };
                detail = appException.Message;
                errors = appException.Errors;
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                title = "An unexpected error occurred";
                detail = "An unexpected error occurred. Please try again later.";
                _logger.LogError(exception, "Unhandled exception {TraceId}", httpContext.TraceIdentifier);
                break;
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.io/{statusCode}",
            Instance = httpContext.Request.Path
        };
        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        if (errors is not null)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
