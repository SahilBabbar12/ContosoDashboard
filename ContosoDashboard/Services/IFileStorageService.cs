using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream stream, string safeFileName, string folder = "uploads", CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string storagePath, CancellationToken cancellationToken = default);
    Task<string> GetUrlAsync(string storagePath, CancellationToken cancellationToken = default);
}
