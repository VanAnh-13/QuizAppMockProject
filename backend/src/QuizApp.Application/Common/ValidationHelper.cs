using FluentValidation;
using QuizApp.Application.Common.Exceptions;

namespace QuizApp.Application.Common;

public static class ValidationHelper
{
    /// <summary>Runs a FluentValidation validator and throws <see cref="ValidationFailedException"/> on failure.</summary>
    public static async Task ValidateAsync<T>(IValidator<T>? validator, T instance, CancellationToken ct = default)
    {
        if (validator is null)
        {
            return;
        }

        var result = await validator.ValidateAsync(instance, ct);
        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors
            .GroupBy(e => char.ToLowerInvariant(e.PropertyName[0]) + e.PropertyName[1..])
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        throw new ValidationFailedException(errors);
    }
}
