# Data Model

## Core Entities

### Document

Represents a stored work-related file associated with a user, categorizable metadata, and optional project context.

| Field | Type | Constraints | Notes |
|---|---|---|---|
| Id | Guid | Required | Primary key. |
| Title | string | Required, max 200 | User-visible searchable title. |
| Description | string | Optional, max 2000 | Searchable plain-text description. |
| Category | enum/string | Required | Valid values include `Project Documents`, `Team Resources`, `Personal Files`, `Reports`, `Presentations`, `Other`. |
| Tags | string[]/string | Optional | Stored and indexed as searchable metadata. |
| ProjectId | Guid? | Optional | Links document to a project when available. |
| UploaderId | Guid | Required | Links to authenticated user identity. |
| UploadDateUtc | DateTime | Required | Audit timestamp. |
| FileName | string | Required | Stored safe file identifier, never originating from user-supplied file name. |
| FileSizeBytes | long | Required | Enforced against 25 MB limit. |
| MimeType | string | Required, max 255 | Example `application/pdf`. |
| StoragePath | string | Required | Secure, non-web-accessible path. |
| FileHash | string? | Optional | Useful for tamper and duplicate validation. |
| SecurityScanStatus | enum/string | Required | `Pending`, `Clean`, `Rejected`, `Quarantined` etc. |
| IsDeleted | bool | Required | Soft-delete or archival flag. |

Relationships:

- `Document` belongs to an `Uploader` (`User`).
- `Document` may belong to a `Project`.
- `Document` may have many `DocumentShare` permissions.
- `Document` may generate many `DocumentActivityLog` records.

Validation rules:

- Title and category must be provided before upload is accepted.
- File extension and size must be validated before database commit.
- File storage path must be generated using a GUID and safe extension handling.
- MIME type should be recorded from content, not untrusted client input.

### DocumentShare

Represents a permission grant between a document and a user or team.

| Field | Type | Constraints | Notes |
|---|---|---|---|
| Id | Guid | Required | Primary key. |
| DocumentId | Guid | Required | Target document. |
| RecipientUserId | Guid? | Optional | User share recipient. |
| RecipientTeamId | Guid? | Optional | Team share recipient. |
| PermissionLevel | enum/string | Required | `View`, `Download`, `Edit`, `Manage`. |
| CreatedByUserId | Guid | Required | Owner or manager creating the grant. |
| CreatedUtc | DateTime | Required | Audit timestamp. |
| IsActive | bool | Required | Allows revocation. |

Relationships:

- `DocumentShare` belongs to a `Document`.
- `DocumentShare` references user or team recipients.
- Creation and revocation should emit activity log records.

### DocumentActivityLog

Represents an administrative audit record for document access and management changes.

| Field | Type | Constraints | Notes |
|---|---|---|---|
| Id | Guid | Required | Primary key. |
| DocumentId | Guid | Required | Related document. |
| ActorUserId | Guid | Required | Actor initiating the action. |
| ActionType | enum/string | Required | Upload, Download, Delete, Share, Replace, Edit, Preview. |
| ActionUtc | DateTime | Required | Audit timestamp. |
| Details | string | Optional, max 2000 | Plain-language summary or JSON-safe details. |
| IpAddress | string? | Optional | Training implementation may omit. |

Relationships:

- `DocumentActivityLog` belongs to a `Document`.
- `DocumentActivityLog` links to `User` actor identity.

### FileScanResult

Represents the security scan status created by the scan abstraction. This is not a UI domain model but a service contract object.

| Field | Type | Constraints | Notes |
|---|---|---|---|
| IsClean | bool | Required | True when accepted. |
| Reason | string? | Optional | Scan rejection explanation. |
| ScanId | string? | Optional | Traceable external scan identifier. |

## State and Workflow

The document lifecycle should be stateful through the following three phases:

1. Draft uploaded metadata, file validation, and secure scan pending.
2. Accepted file stored through `IFileStorageService` and connected to audit and document metadata.
3. Shareable, downloadable, replaceable, or deletable according to permission records.

The application should record one or more `DocumentActivityLog` records for every document action.
