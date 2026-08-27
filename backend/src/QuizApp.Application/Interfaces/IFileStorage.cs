namespace QuizApp.Application.Interfaces;

/// <summary>
/// Stores uploaded files (avatars) and returns the public relative URL.
/// </summary>
public interface IFileStorage
{
    Task<string> SaveAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken = default);

    void Delete(string? relativeUrl);
}
