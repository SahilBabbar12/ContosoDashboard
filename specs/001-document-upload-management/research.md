# Research

## Decision: Use an application-owned document storage service abstraction

The feature will be implemented through the existing Blazor Server repository shape: `Models`, `Data`, `Services`, `Pages`, and `Shared`. The document management workflow will add a secure document storage abstraction with a local filesystem implementation in the training codebase and a placeholder malware scanning abstraction.

### Rationale

The stakeholder requirements explicitly require:

- file storage outside `wwwroot`
- secure, role-aware file access through an abstraction
- a malware and virus risk scanning abstraction
- local file storage in the training implementation
- generation of unique, safe file paths before database writes

The repository already follows a service-oriented design in `ContosoDashboard/Services/` and page-driven UI workflows. That makes a service abstraction the best fit for a training implementation without changing the rest of the UI structure.

### Alternatives considered

- Direct file writes from the page layer: rejected because it violates repository conventions and bypasses access control.
- Cloud-only or Azure-specific storage from the start: rejected because the repository is an offline training app and the requirement explicitly requires a local-training storage implementation.
- Malware scanning as a hard dependency on a concrete scanner product: rejected because the spec clarified that the requirement is for a secure-storage abstraction with a placeholder scanning contract.

## Decision: Add document metadata and permissions as first-class entities

The data model should recognize `Document`, `DocumentShare`, `DocumentActivityLog`, and the project ownership relationship represented by `Project` and `User` models.

### Rationale

The spec requires searchable metadata, category and project association, sharing, notification, audit logging, and permission-aware authorization. Those concerns are already represented in the repository’s model-service-page pattern.

### Alternatives considered

- Treating documents as loose files only: rejected because the required document metadata, search, audit, and sharing rules need a persisted entity.
- Overloading `Project` or `TaskItem` records with document file payloads: rejected because it weakens service boundaries and makes metadata and permissions difficult to manage consistently.

## Decision: Keep the contract layer service-focused

The design will add contracts where interface boundaries are meaningful:

- `IFileStorageService` for upload/delete/download/url generation.
- `IMalwareScanService` for pre-storage risk screening.
- A `DocumentUploadRequest` DTO or page-level model that can be validated before storage.

### Rationale

The feature already states that the repository should preserve abstraction and future migration possibilities. The contract should describe the expected service behavior without demanding the cloud vendor implementation.

### Alternatives considered

- Defining a full controller or endpoint contract: rejected because the current repository uses Razor pages and service composable logic rather than a standalone API layer.
- Inlining storage calls in page code: rejected because this would create bypasses around role-aware enforcement and file path security.

## Decision: Validate all uploads through a normal upload chain

The implementation should use an upload chain that maps to the spec’s safety guidance:

1. Select and validate file extension and size.
2. Generate a safe storage path with a GUID-based filename outside of `wwwroot`.
3. Run a malware scan abstraction before commit.
4. Persist document metadata and audit log details.
5. Store the file securely and return a success/error message with progress and feedback.

### Rationale

This ordering satisfies the feature’s file path safety requirement, supports auditing, and provides a reliable place for authorization and failure handling.

### Alternatives considered

- Save database record first then file: rejected because it creates orphan records and duplicate file path errors.
- Use the user-supplied filename directly: rejected because it allows path traversal and name collision issues.

## Decision: Keep the first implementation internal and portable

The default implementation will be local file storage and local security abstractions using the interface names already described in the stakeholder requirements.

### Rationale

This is consistent with the repository’s training environment and requirements for a portable, secure, offline-friendly storage pattern.

### Alternatives considered

- Azure Blob Storage integration now: rejected due to infrastructure assumptions and repository design constraints.
- Database BLOB storage: rejected due to security and file serving requirements that insist on storage outside the web-accessible directory.
