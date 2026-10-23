using Microsoft.Extensions.Options;
using Sigloc.Application.Contracts;

namespace Sigloc.Infrastructure.Storage;

public sealed class FileStorageSettings
{
    public const string SectionName = "FileStorage";

    /// <summary>Root folder for stored files; relative paths are resolved against the content root.</summary>
    public string RootPath { get; set; } = "App_Data/uploads";
}

/// <summary>
/// Stores files on the local disk below <see cref="FileStorageSettings.RootPath"/>. Keys are
/// "{prefix}/{guid}{ext}"; any key resolving outside the root is rejected.
/// </summary>
public sealed class LocalDiskFileStorage(IOptions<FileStorageSettings> options) : IFileStorage
{
    private readonly string _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.Value.RootPath));

    public async Task<string> SaveAsync(string prefix, Stream content, string extension, CancellationToken ct = default)
    {
        var key = $"{prefix.Trim('/')}/{Guid.NewGuid():N}{extension}";
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp";
        try
        {
            await using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                await content.CopyToAsync(file, ct);
            File.Move(temporary, path);
        }
        catch
        {
            File.Delete(temporary);
            throw;
        }
        return key;
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken ct = default)
    {
        var path = Resolve(key);
        return Task.FromResult<Stream?>(File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true)
            : null);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        File.Delete(Resolve(key));
        return Task.CompletedTask;
    }

    private string Resolve(string key)
    {
        var path = Path.GetFullPath(Path.Combine(_root, key));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException("Invalid storage key.", nameof(key));
        return path;
    }
}
