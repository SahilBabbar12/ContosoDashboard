using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public sealed class DocumentUploadRequest
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Other";
    public string? Description { get; set; }
    public int? ProjectId { get; set; }
    public string? Tags { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string ContentType { get; set; } = "application/octet-stream";
}

public sealed class DocumentUploadResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Document? Document { get; set; }
}

public class DocumentService
{
    private const long MaxBytes = 25 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg", ".gif", ".txt"
    };

    private readonly ApplicationDbContext _dbContext;
    private readonly IFileStorageService _storageService;
    private readonly IMalwareScanService _malwareScanService;

    public DocumentService(ApplicationDbContext dbContext, IFileStorageService storageService, IMalwareScanService malwareScanService)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _malwareScanService = malwareScanService;
    }

    public DocumentUploadResult ValidateUploadRequest(DocumentUploadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Fail("Title is required.");

        if (request.Title.Length > 255)
            return Fail("Title must be 255 characters or fewer.");

        if (string.IsNullOrWhiteSpace(request.FileName))
            return Fail("A file is required.");

        var extension = Path.GetExtension(request.FileName);
        if (!AllowedExtensions.Contains(extension))
            return Fail("Unsupported file type. Please upload a supported business document such as PDF, Word, Excel, PowerPoint, image, or text.");

        if (request.FileSizeBytes <= 0 || request.FileSizeBytes > MaxBytes)
            return Fail("The selected file is larger than the 25 MB limit. Please choose a file that is 25 MB or smaller.");

        if (!Enum.TryParse<DocumentCategory>(request.Category, ignoreCase: true, out var category))
            return Fail("Invalid document category.");

        return new DocumentUploadResult { Success = true, Message = "Validation passed." };
    }

    public async Task<DocumentUploadResult> UploadAsync(DocumentUploadRequest request, Stream stream, int uploaderUserId, CancellationToken cancellationToken = default)
    {
        var validation = ValidateUploadRequest(request);
        if (!validation.Success)
        {
            return validation;
        }

        var uploadBuffer = new MemoryStream();
        await stream.CopyToAsync(uploadBuffer, cancellationToken);
        uploadBuffer.Position = 0;

        var scanStream = new MemoryStream(uploadBuffer.ToArray(), writable: false);
        var scan = await _malwareScanService.ScanAsync(scanStream, request.FileName, cancellationToken);
        if (!scan.IsClean)
        {
            var activity = new DocumentActivityLog
            {
                Action = DocumentActivityAction.Reject,
                Details = $"Upload rejected: {scan.Reason}",
                CreatedDate = DateTime.UtcNow,
                UserId = uploaderUserId,
                DocumentId = 0
            };

            return Fail($"Upload rejected by scan service: {scan.Reason}");
        }

        var safeName = Path.GetFileName(request.FileName);
        var storageStream = new MemoryStream(uploadBuffer.ToArray(), writable: false);
        var storagePath = await _storageService.UploadAsync(storageStream, safeName, "documents", cancellationToken);

        var category = Enum.Parse<DocumentCategory>(request.Category, ignoreCase: true);
        var document = new Document
        {
            Title = request.Title.Trim(),
            Description = request.Description,
            Category = category,
            UploaderUserId = uploaderUserId,
            ProjectId = request.ProjectId,
            FileName = request.FileName,
            StoragePath = storagePath,
            ContentType = request.ContentType,
            FileSizeBytes = request.FileSizeBytes,
            Tags = request.Tags,
            UploadedDate = DateTime.UtcNow,
            IsActive = true
        };

        _dbContext.Documents.Add(document);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var activityLog = new DocumentActivityLog
        {
            DocumentId = document.DocumentId,
            UserId = uploaderUserId,
            Action = DocumentActivityAction.Upload,
            Details = "Upload accepted and stored.",
            CreatedDate = DateTime.UtcNow
        };

        _dbContext.DocumentActivityLogs.Add(activityLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DocumentUploadResult
        {
            Success = true,
            Message = "Document uploaded successfully.",
            Document = document
        };
    }

    public async Task<List<Document>> GetDocumentsForUserAsync(int userId)
    {
        return await _dbContext.Documents
            .Include(d => d.Uploader)
            .Include(d => d.Project)
            .Where(d => d.UploaderUserId == userId || d.Project!.ProjectMembers.Any(pm => pm.UserId == userId))
            .ToListAsync();
    }

    public static DocumentUploadResult Fail(string message)
    {
        return new DocumentUploadResult { Success = false, Message = message };
    }
}
