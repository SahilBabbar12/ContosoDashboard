# Quickstart

## Goal

Validate that the document upload and management workflow is wired through the repository’s trained service and page conventions without requiring external cloud services.

## Prerequisites

- Repository root contains the `.NET` web project and the `ContosoDashboard` training application.
- A user is authenticated using the mock authentication provider already represented in the app.
- The local document storage directory is created outside `wwwroot` and visible to the service layer.

## Validation Scenarios

### 1. Happy-path upload

1. Sign in as an authenticated user.
2. Open a project page or personal documents area.
3. Select a supported file (for example PDF or PNG) under 25 MB.
4. Enter required metadata: title and category.
5. Submit the upload.

Expected outcome:

- The UI reports upload progress and success feedback.
- The document record is created with upload metadata and the activity log contains an upload record.
- The file is stored under a safe, GUID-based path outside `wwwroot`.

### 2. Rejection path

1. Attempt to upload an unsupported file or a file larger than 25 MB.
2. Submit the document.

Expected outcome:

- The UI presents a clear rejection message.
- No metadata record is stored.
- The upload activity log shows the rejected attempt or logs the validation error without creating the file.

### 3. Search and access control

1. Upload a document with a tag or description.
2. Search by title, description, tag, uploader, or project.
3. Attempt access from an unauthorized account or a user without project membership.

Expected outcome:

- Only authorized documents appear in search results.
- Unauthorized document access fails through the service or page authorization boundary.

### 4. Download and preview

1. From document browse or project view, select a document available to the user.
2. Choose preview for a PDF or image, or choose download.

Expected outcome:

- A preview loads in the browser for common file types.
- Download permission is enforced through the same service abstraction.

## Validation Evidence

A passing implementation should show:

- document metadata persisted
- a secure local storage path persisted
- activity log entries for upload and access flows
- rejection messages for invalid file types and oversized files
- role-aware visibility checks reflected in search and browse views
