namespace QuizApp.Application.Common.Exceptions;

/// <summary>Base class for errors that map onto HTTP status codes via the global exception handler.</summary>
public abstract class AppException : Exception
{
    protected AppException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }

    /// <summary>Field-level errors surfaced as ProblemDetails.errors.</summary>
    public virtual IDictionary<string, string[]>? Errors => null;
}

public class BadRequestException : AppException
{
    private readonly IDictionary<string, string[]>? _errors;

    public BadRequestException(string message, IDictionary<string, string[]>? errors = null) : base(message, 400)
    {
        _errors = errors;
    }

    public override IDictionary<string, string[]>? Errors => _errors;
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(message, 401) { }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message, 403) { }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message, 404) { }
}

public class ConflictException : AppException
{
    public ConflictException(string message, IDictionary<string, string[]>? errors = null) : base(message, 409)
    {
        _errors = errors;
    }

    private readonly IDictionary<string, string[]>? _errors;
    public override IDictionary<string, string[]>? Errors => _errors;
}

public class ValidationFailedException : AppException
{
    public ValidationFailedException(IDictionary<string, string[]> errors)
        : base("Validation failed for one or more fields.", 400)
    {
        _errors = errors;
    }

    private readonly IDictionary<string, string[]> _errors;
    public override IDictionary<string, string[]>? Errors => _errors;
}
