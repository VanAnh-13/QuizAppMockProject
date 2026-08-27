using Microsoft.Extensions.Configuration;
using QuizApp.Application.Interfaces;

namespace QuizApp.Infrastructure.Storage;

/// <summary>
/// Local-disk file storage for avatars. Files land under
/// {FileStorage:BasePath}/avatars and are served at /avatars by the API.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/webp", "image/gif"
    };

    private static readonly Dictionary<string, string> ExtensionByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif"
    };

    private readonly string _avatarsDirectory;

    public LocalFileStorage(IConfiguration configuration)
    {
        var basePath = configuration["FileStorage:BasePath"] ?? "data";
        _avatarsDirectory = Path.GetFullPath(Path.Combine(basePath, "avatars"));
        Directory.CreateDirectory(_avatarsDirectory);
    }

    public async Task<string> SaveAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken = default)
    {
        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new Application.Common.Exceptions.BadRequestException(
                "Unsupported file type. Please upload a PNG, JPEG, WEBP or GIF image.");
        }

        var extension = ExtensionByContentType[contentType];
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_avatarsDirectory, storedName);

        await using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
        {
            await content.CopyToAsync(stream, cancellationToken);
        }

        return $"/avatars/{storedName}";
    }

    public void Delete(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl))
        {
            return;
        }

        var name = Path.GetFileName(relativeUrl);
        var fullPath = Path.Combine(_avatarsDirectory, name);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
