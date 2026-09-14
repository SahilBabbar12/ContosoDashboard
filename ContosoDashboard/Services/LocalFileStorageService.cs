using System.Security.Cryptography;
using System.Text;

namespace ContosoDashboard.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly string _storageRoot;

    public LocalFileStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
        _storageRoot = Path.Combine(_environment.ContentRootPath, "AppData", "uploads");

        Directory.CreateDirectory(_storageRoot);
    }

    public async Task<string> UploadAsync(Stream stream, string safeFileName, string folder = "uploads", CancellationToken cancellationToken = default)
    {
        var safeFolder = Path.GetInvalidFileNameChars().Any(c => folder.Contains(c)) ? "uploads" : folder;
        var folderPath = Path.Combine(_storageRoot, safeFolder);
        Directory.CreateDirectory(folderPath);

        var extension = Path.GetExtension(safeFileName);
        var guid = Guid.NewGuid().ToString("N");
        var targetName = $"{guid}{extension}";
        var targetPath = Path.Combine(folderPath, targetName);

        await using var output = File.Create(targetPath);
        await stream.CopyToAsync(output, cancellationToken);

        return Path.GetRelativePath(_storageRoot, targetPath).Replace('\\', '/');
    }

    public Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var targetPath = Path.Combine(_storageRoot, storagePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(targetPath)) return Task.FromResult(false);

        File.Delete(targetPath);
        return Task.FromResult(true);
    }

    public Task<Stream> DownloadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var targetPath = Path.Combine(_storageRoot, storagePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(targetPath)) throw new FileNotFoundException("Document file not found.", targetPath);

        return Task.FromResult<Stream>(File.OpenRead(targetPath));
    }

    public Task<string> GetUrlAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"/documents/{storagePath}");
    }
}
