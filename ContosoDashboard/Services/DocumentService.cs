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

public sealed class DocumentShareRequest
{
    public int RecipientUserId { get; set; }
    public DocumentShareAccess AccessLevel { get; set; } = DocumentShareAccess.Read;
}

public sealed class DocumentAuditLog
{
    public int DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
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
    private readonly INotificationService _notificationService;

    public DocumentService(ApplicationDbContext dbContext, IFileStorageService storageService, IMalwareScanService malwareScanService, INotificationService notificationService)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _malwareScanService = malwareScanService;
        _notificationService = notificationService;
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
            await _dbContext.DocumentActivityLogs.AddAsync(new DocumentActivityLog
            {
                DocumentId = 0,
                UserId = uploaderUserId,
                Action = DocumentActivityAction.Reject,
                Details = $"Upload rejected during validation: {validation.Message}",
                CreatedDate = DateTime.UtcNow
            }, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

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
        return await SearchDocumentsAsync(userId, null, null, null, "UploadedDateDesc");
    }

    public async Task<List<Document>> GetProjectDocumentsAsync(int projectId, int requestingUserId)
    {
        var project = await _dbContext.Projects
            .Include(p => p.ProjectMembers)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);

        if (project is null)
        {
            return new List<Document>();
        }

        var allowed = project.ProjectManagerId == requestingUserId ||
            project.ProjectMembers.Any(pm => pm.UserId == requestingUserId);

        if (!allowed)
        {
            return new List<Document>();
        }

        return await _dbContext.Documents
            .Include(d => d.Uploader)
            .Include(d => d.Project)
            .Where(d => d.ProjectId == projectId && d.IsActive)
            .OrderByDescending(d => d.UploadedDate)
            .ToListAsync();
    }

    public async Task<List<Document>> GetPersonalDocumentsAsync(int userId)
    {
        return await _dbContext.Documents
            .Include(d => d.Uploader)
            .Include(d => d.Project)
            .Where(d => d.UploaderUserId == userId && d.IsActive && d.ProjectId == null)
            .OrderByDescending(d => d.UploadedDate)
            .ToListAsync();
    }

    public async Task<Document?> GetDocumentByIdAsync(int documentId)
    {
        return await _dbContext.Documents
            .Include(d => d.Uploader)
            .Include(d => d.Project)
            .Include(d => d.Shares)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && d.IsActive);
    }

    public async Task<List<Document>> SearchDocumentsAsync(int userId, string? term, int? projectId, string? category, string sort = "UploadedDateDesc")
    {
        var query = _dbContext.Documents
            .Include(d => d.Uploader)
            .Include(d => d.Project)
            .Include(d => d.Shares)
            .Where(d => d.IsActive &&
                (d.UploaderUserId == userId ||
                 d.Project != null && (d.Project.ProjectManagerId == userId || d.Project.ProjectMembers.Any(pm => pm.UserId == userId)) ||
                 d.Shares.Any(s => s.UserId == userId)));

        if (!string.IsNullOrWhiteSpace(term))
        {
            var cleanTerm = term.Trim();
            query = query.Where(d =>
                d.Title.Contains(cleanTerm) ||
                (d.Description != null && d.Description.Contains(cleanTerm)) ||
                (d.Tags != null && d.Tags.Contains(cleanTerm)) ||
                (d.Uploader.DisplayName != null && d.Uploader.DisplayName.Contains(cleanTerm)) ||
                (d.Project != null && d.Project.Name.Contains(cleanTerm)));
        }

        if (projectId.HasValue)
        {
            query = query.Where(d => d.ProjectId == projectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<DocumentCategory>(category, ignoreCase: true, out var parsedCategory))
        {
            query = query.Where(d => d.Category == parsedCategory);
        }

        query = sort?.ToLowerInvariant() switch
        {
            "title" => query.OrderBy(d => d.Title),
            "category" => query.OrderBy(d => d.Category).ThenByDescending(d => d.UploadedDate),
            "uploader" => query.OrderBy(d => d.Uploader.DisplayName).ThenByDescending(d => d.UploadedDate),
            "project" => query.OrderBy(d => d.Project != null ? d.Project.Name : string.Empty).ThenByDescending(d => d.UploadedDate),
            "uploadeddatedesc" => query.OrderByDescending(d => d.UploadedDate),
            _ => query.OrderByDescending(d => d.UploadedDate)
        };

        return await query.ToListAsync();
    }

    public async Task<bool> CanDownloadDocumentAsync(int documentId, int requestingUserId)
    {
        return await CanAccessDocumentAsync(documentId, requestingUserId);
    }

    public async Task<bool> CanPreviewDocumentAsync(int documentId, int requestingUserId)
    {
        return await CanAccessDocumentAsync(documentId, requestingUserId);
    }

    public async Task<bool> CanAccessDocumentAsync(int documentId, int requestingUserId)
    {
        var document = await _dbContext.Documents
            .Include(d => d.Project)
            .ThenInclude(p => p!.ProjectMembers)
            .Include(d => d.Shares)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && d.IsActive);

        if (document is null)
        {
            return false;
        }

        if (document.UploaderUserId == requestingUserId)
        {
            return true;
        }

        if (document.Project is not null &&
            (document.Project.ProjectManagerId == requestingUserId ||
             document.Project.ProjectMembers.Any(pm => pm.UserId == requestingUserId)))
        {
            return true;
        }

        if (document.Shares.Any(s => s.UserId == requestingUserId))
        {
            return true;
        }

        return false;
    }

    public async Task<List<DocumentActivityLog>> GetDocumentActivityLogsAsync(int documentId)
    {
        return await _dbContext.DocumentActivityLogs
            .Include(d => d.User)
            .Where(d => d.DocumentId == documentId)
            .OrderByDescending(d => d.CreatedDate)
            .ToListAsync();
    }

    public async Task<List<DocumentActivityLog>> GetAllDocumentActivityLogsAsync(int requestingUserId)
    {
        var currentUser = await _dbContext.Users.FindAsync(requestingUserId);
        if (currentUser is null || currentUser.Role != UserRole.Administrator)
        {
            return new List<DocumentActivityLog>();
        }

        return await _dbContext.DocumentActivityLogs
            .Include(d => d.User)
            .Include(d => d.Document)
            .OrderByDescending(d => d.CreatedDate)
            .Take(200)
            .ToListAsync();
    }

    public async Task<bool> ShareDocumentAsync(int documentId, int requestingUserId, DocumentShareRequest shareRequest)
    {
        var document = await _dbContext.Documents
            .Include(d => d.Project)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && d.IsActive);

        if (document is null)
        {
            return false;
        }

        var canManage = document.UploaderUserId == requestingUserId ||
            (document.Project != null && document.Project.ProjectManagerId == requestingUserId);

        if (!canManage)
        {
            return false;
        }

        var recipientExists = await _dbContext.Users
            .AnyAsync(u => u.UserId == shareRequest.RecipientUserId);

        if (!recipientExists)
        {
            return false;
        }

        var existing = await _dbContext.DocumentShares
            .FirstOrDefaultAsync(s => s.DocumentId == documentId && s.UserId == shareRequest.RecipientUserId);

        if (existing is not null)
        {
            existing.AccessLevel = shareRequest.AccessLevel;
            existing.SharedDate = DateTime.UtcNow;
            existing.RevokedDate = null;
        }
        else
        {
            _dbContext.DocumentShares.Add(new DocumentShare
            {
                DocumentId = documentId,
                UserId = shareRequest.RecipientUserId,
                AccessLevel = shareRequest.AccessLevel,
                SharedDate = DateTime.UtcNow
            });
        }

        _dbContext.DocumentActivityLogs.Add(new DocumentActivityLog
        {
            DocumentId = documentId,
            UserId = requestingUserId,
            Action = DocumentActivityAction.Share,
            Details = $"Document shared with user {shareRequest.RecipientUserId} at {shareRequest.AccessLevel} level.",
            CreatedDate = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();

        await _notificationService.CreateDocumentNotificationAsync(
            shareRequest.RecipientUserId,
            "Document shared",
            $"You received access to '{document.Title}' with {shareRequest.AccessLevel} access.",
            NotificationType.DocumentShare,
            NotificationPriority.Important);

        return true;
    }

    public async Task<bool> RevokeDocumentShareAsync(int documentId, int requestingUserId, int recipientUserId)
    {
        var document = await _dbContext.Documents
            .Include(d => d.Project)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && d.IsActive);

        if (document is null)
        {
            return false;
        }

        var canManage = document.UploaderUserId == requestingUserId ||
            (document.Project != null && document.Project.ProjectManagerId == requestingUserId);

        if (!canManage)
        {
            return false;
        }

        var share = await _dbContext.DocumentShares
            .FirstOrDefaultAsync(s => s.DocumentId == documentId && s.UserId == recipientUserId && s.RevokedDate == null);

        if (share is null)
        {
            return false;
        }

        share.RevokedDate = DateTime.UtcNow;

        _dbContext.DocumentActivityLogs.Add(new DocumentActivityLog
        {
            DocumentId = documentId,
            UserId = requestingUserId,
            Action = DocumentActivityAction.Share,
            Details = $"Document share revoked for user {recipientUserId}.",
            CreatedDate = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateDocumentAsync(int documentId, int requestingUserId, string title, string? description, string? tags, string category, int? projectId)
    {
        var document = await _dbContext.Documents
            .Include(d => d.Project)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && d.IsActive);

        if (document is null)
        {
            return false;
        }

        var canManage = document.UploaderUserId == requestingUserId ||
            (document.Project != null && document.Project.ProjectManagerId == requestingUserId);

        if (!canManage)
        {
            return false;
        }

        if (!Enum.TryParse<DocumentCategory>(category, ignoreCase: true, out var parsedCategory))
        {
            return false;
        }

        document.Title = string.IsNullOrWhiteSpace(title) ? document.Title : title.Trim();
        document.Description = description;
        document.Tags = tags;
        document.Category = parsedCategory;
        document.ProjectId = projectId;
        document.UpdatedDate = DateTime.UtcNow;

        _dbContext.DocumentActivityLogs.Add(new DocumentActivityLog
        {
            DocumentId = documentId,
            UserId = requestingUserId,
            Action = DocumentActivityAction.Replace,
            Details = "Document metadata edited.",
            CreatedDate = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReplaceDocumentFileAsync(int documentId, int requestingUserId, Stream stream, string fileName, string contentType, long fileSizeBytes)
    {
        var document = await _dbContext.Documents
            .Include(d => d.Project)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && d.IsActive);

        if (document is null)
        {
            return false;
        }

        var canManage = document.UploaderUserId == requestingUserId ||
            (document.Project != null && document.Project.ProjectManagerId == requestingUserId);

        if (!canManage)
        {
            return false;
        }

        var extension = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return false;
        }

        if (fileSizeBytes <= 0 || fileSizeBytes > MaxBytes)
        {
            return false;
        }

        var safeName = Path.GetFileName(fileName);
        var uploadBuffer = new MemoryStream();
        await stream.CopyToAsync(uploadBuffer);
        uploadBuffer.Position = 0;

        var storagePath = await _storageService.UploadAsync(uploadBuffer, safeName, "documents");

        document.StoragePath = storagePath;
        document.FileName = safeName;
        document.ContentType = contentType;
        document.FileSizeBytes = fileSizeBytes;
        document.UpdatedDate = DateTime.UtcNow;

        _dbContext.DocumentActivityLogs.Add(new DocumentActivityLog
        {
            DocumentId = documentId,
            UserId = requestingUserId,
            Action = DocumentActivityAction.Replace,
            Details = "Document file replaced.",
            CreatedDate = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, int requestingUserId)
    {
        var document = await _dbContext.Documents
            .Include(d => d.Project)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && d.IsActive);

        if (document is null)
        {
            return false;
        }

        var canManage = document.UploaderUserId == requestingUserId ||
            (document.Project != null && document.Project.ProjectManagerId == requestingUserId);

        if (!canManage)
        {
            return false;
        }

        document.IsActive = false;
        document.UpdatedDate = DateTime.UtcNow;

        _dbContext.DocumentActivityLogs.Add(new DocumentActivityLog
        {
            DocumentId = documentId,
            UserId = requestingUserId,
            Action = DocumentActivityAction.Delete,
            Details = "Document marked inactive by owner or project manager.",
            CreatedDate = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();
        await _storageService.DeleteAsync(document.StoragePath);
        return true;
    }

    public async Task<Stream> DownloadDocumentAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        if (!await CanDownloadDocumentAsync(documentId, requestingUserId))
        {
            throw new UnauthorizedAccessException("You are not authorized to download this document.");
        }

        var document = await _dbContext.Documents
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && d.IsActive, cancellationToken);

        if (document is null)
        {
            throw new FileNotFoundException("Document file not found.");
        }

        var stream = await _storageService.DownloadAsync(document.StoragePath, cancellationToken);

        _dbContext.DocumentActivityLogs.Add(new DocumentActivityLog
        {
            DocumentId = documentId,
            UserId = requestingUserId,
            Action = DocumentActivityAction.Download,
            Details = $"Document downloaded by user {requestingUserId}.",
            CreatedDate = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();
        return stream;
    }

    public static DocumentUploadResult Fail(string message)
    {
        return new DocumentUploadResult { Success = false, Message = message };
    }
}
