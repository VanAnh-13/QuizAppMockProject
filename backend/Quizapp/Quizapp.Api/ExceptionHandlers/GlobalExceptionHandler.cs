using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Quizapp.Domain.Exceptions;
using ValidationException = Quizapp.Domain.Exceptions.ValidationException;

namespace Quizapp.Api.ExceptionHandlers;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
        {
            logger.LogWarning(
                "The response has already started; the global exception handler will not be executed.");
            return false;
        }

        var statusCode = MapStatusCode(exception);

        if (statusCode >= 500)
            logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        else
            logger.LogWarning(exception, "Domain exception ({Type}): {Message}",
                exception.GetType().Name, exception.Message);

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Error = (exception as QuizAppException)?.ErrorCode ?? "INTERNAL_ERROR",
            Message = statusCode >= 500 && exception is not QuizAppException
                ? "An unexpected error occurred."
                : exception.Message,
            TraceId = Activity.Current?.Id ?? httpContext.TraceIdentifier,
            Rule = (exception as BusinessRuleException)?.RuleName,
            Errors = (exception as ValidationException)?.Errors
        }, cancellationToken);

        return true;
    }

    private static int MapStatusCode(Exception exception) => exception switch
    {
        NotFoundException => StatusCodes.Status404NotFound,
        ConflictException => StatusCodes.Status409Conflict,
        ValidationException => StatusCodes.Status422UnprocessableEntity,
        BusinessRuleException => StatusCodes.Status400BadRequest,
        AuthenticationException => StatusCodes.Status401Unauthorized,
        ForbiddenException => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };
}

public sealed class ErrorResponse
{
    public required string Error { get; init; }
    public required string Message { get; init; }
    public string? TraceId { get; init; }
    public string? Rule { get; init; }
    public IDictionary<string, string[]>? Errors { get; init; }
}