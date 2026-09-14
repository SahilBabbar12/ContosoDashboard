using System.Security.Cryptography;
using System.Text;

namespace ContosoDashboard.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly string _storageRoot;
    private readonly string _storageRootAbsolute;

    public LocalFileStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
        _storageRoot = Path.Combine(_environment.ContentRootPath, "AppData", "uploads");
        _storageRootAbsolute = Path.GetFullPath(_storageRoot);

        Directory.CreateDirectory(_storageRoot);
    }

    public async Task<string> UploadAsync(Stream stream, string safeFileName, string folder = "uploads", CancellationToken cancellationToken = default)
    {
        var safeFolder = NormalizeFolder(folder);
        var folderPath = Path.Combine(_storageRoot, safeFolder);
        Directory.CreateDirectory(folderPath);

        var safeName = Path.GetFileName(safeFileName);
        var extension = Path.GetExtension(safeName);
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg", ".gif", ".txt"
        };

        if (!allowed.Contains(extension))
        {
            throw new InvalidOperationException("Unsupported file type for secure local storage.");
        }

        var guid = Guid.NewGuid().ToString("N");
        var targetName = $"{guid}{extension}";
        var targetPath = Path.Combine(folderPath, targetName);

        await using var output = File.Create(targetPath);
        await stream.CopyToAsync(output, cancellationToken);

        return Path.GetRelativePath(_storageRoot, targetPath).Replace('\\', '/');
    }

    public Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var normalized = storagePath?.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar) ?? string.Empty;
        var targetPath = Path.GetFullPath(Path.Combine(_storageRoot, normalized));
        if (!targetPath.StartsWith(_storageRootAbsolute, StringComparison.OrdinalIgnoreCase) || !File.Exists(targetPath)) return Task.FromResult(false);

        File.Delete(targetPath);
        return Task.FromResult(true);
    }

    public Task<Stream> DownloadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var normalized = storagePath?.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar) ?? string.Empty;
        var targetPath = Path.GetFullPath(Path.Combine(_storageRoot, normalized));
        if (!targetPath.StartsWith(_storageRootAbsolute, StringComparison.OrdinalIgnoreCase) || !File.Exists(targetPath)) throw new FileNotFoundException("Document file not found.", targetPath);

        return Task.FromResult<Stream>(File.OpenRead(targetPath));
    }

    public Task<string> GetUrlAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"/documents/{storagePath}");
    }

    private static string NormalizeFolder(string folder)
    {
        var safe = Path.GetFileName(folder.Trim());
        if (string.IsNullOrWhiteSpace(safe))
        {
            return "documents";
        }

        var invalid = Path.GetInvalidFileNameChars().Concat(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
        if (invalid.Any(ch => safe.Contains(ch)))
        {
            return "documents";
        }

        return safe;
    }
}
