# File Storage Service Contract

## Interface

```csharp
public interface IFileStorageService
{
    Task<string> UploadAsync(Stream content, string safeFileName, string logicalFolder, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string storagePath, CancellationToken cancellationToken = default);
    Task<string> GetUrlAsync(string storagePath, CancellationToken cancellationToken = default);
}
```

## Responsibilities

- Store files outside the web-accessible directory.
- Build secure, portable paths using GUID-based filenames and safe extension handling.
- Hide cloud or local provider differences behind the same service signature.
- Support upload, delete, download, and URL generation operations without changing the page or business-layer contract.

## Local Implementation Expectations

The training-implementation provider should:

- write files using `System.IO.File` under a controlled application data directory such as `AppData/uploads`
- generate a non-user-controlled file path before storing metadata
- reject path traversal or extension escape attempts
- preserve a predictable storage relationship between `userId`, `projectId`, and a safe document identifier

## Security and Audit Constraints

- Access to download and preview flows must remain permission-aware.
- File writes must occur before metadata persistence where possible.
- Service errors should flow up to a user-visible error message and a corresponding activity log entry.
