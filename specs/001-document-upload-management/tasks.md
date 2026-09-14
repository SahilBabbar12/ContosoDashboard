# Tasks: Document Upload and Management

**Input**: Design documents from `/specs/001-document-upload-management/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Tests are optional because the feature specification does not explicitly request automated test tasks.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the document feature scaffold and align it with the repository’s existing services and pages.

- [X] T001 [P] Create a document storage directory and metadata folder conventions in `ContosoDashboard/AppData/uploads/`
- [X] T002 [P] Register the local file storage and malware scan abstractions in `ContosoDashboard/Program.cs`
- [X] T003 [P] Extend `ContosoDashboard/Data/ApplicationDbContext.cs` for Document, DocumentShare, and DocumentActivityLog entity mapping

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Provide the base abstractions and models that all document stories depend on.

**Checkpoint**: Foundation ready - document upload, browse, search, and sharing services can now begin in parallel.

- [X] T004 Add the `Document` entity shape and required metadata fields in `ContosoDashboard/Models/Document.cs`
- [X] T005 [P] Add the `DocumentShare` relationship model in `ContosoDashboard/Models/DocumentShare.cs`
- [X] T006 [P] Add the `DocumentActivityLog` audit model in `ContosoDashboard/Models/DocumentActivityLog.cs`
- [X] T007 [P] Add the `IFileStorageService` interface contract in `ContosoDashboard/Services/IFileStorageService.cs`
- [X] T008 [P] Add the `IMalwareScanService` interface contract in `ContosoDashboard/Services/IMalwareScanService.cs`
- [X] T009 Implement `LocalFileStorageService` in `ContosoDashboard/Services/LocalFileStorageService.cs`
- [X] T010 Implement `PlaceholderMalwareScanService` in `ContosoDashboard/Services/PlaceholderMalwareScanService.cs`
- [X] T011 Create a `DocumentService` validation and orchestration layer in `ContosoDashboard/Services/DocumentService.cs`

---

## Phase 3: User Story 1 - Upload and organize work documents (Priority: P1) 🎯 MVP

**Goal**: Let an authenticated user upload a supported document with required metadata and have it stored securely with searchable metadata.

**Independent Test**: A user can upload a supported file with title, category, and optional project metadata and see that the document is saved with searchable metadata and a clear success message.

### Implementation for User Story 1

- [X] T012 [P] [US1] Add upload request validation and the allowed file-type and 25 MB checks in `ContosoDashboard/Services/DocumentService.cs`
- [X] T013 [P] [US1] Add GUID-safe, non-web path generation and upload persistence in `ContosoDashboard/Services/LocalFileStorageService.cs`
- [X] T014 [US1] Add the upload success, error, and progress message handling in `ContosoDashboard/Pages/Documents.razor`
- [X] T015 [US1] Create the document upload page and metadata form in `ContosoDashboard/Pages/Documents.razor`
- [X] T016 [US1] Capture activity audit records for upload and rejection events in `ContosoDashboard/Services/DocumentService.cs`

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently.

---

## Phase 4: User Story 2 - Browse, search, and access the right project documents (Priority: P2)

**Goal**: Let authorized users browse, filter, and search document metadata across personal and project document views.

**Independent Test**: A user can search by document title, description, tags, uploader, or project and view only documents they are authorized to access.

### Implementation for User Story 2

- [ ] T017 [P] [US2] Add repository query helpers for document listing, project view, and personal documents in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T018 [P] [US2] Implement filter, sort, and search metadata support in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T019 [US2] Add project and personal document browse UI in `ContosoDashboard/Pages/Projects.razor`
- [ ] T020 [US2] Add document download and preview authorization checks in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T021 [US2] Surface document previews and download actions in `ContosoDashboard/Pages/Documents.razor`

**Checkpoint**: At this point, User Stories 1 and 2 should both work independently.

---

## Phase 5: User Story 3 - Share, manage, and audit document access (Priority: P3)

**Goal**: Let document owners and project managers share, replace, delete, and audit document access while keeping the document lifecycle explicit.

**Independent Test**: A user with permission can share a document with another user or team, manage metadata, replace the file, and confirm deletion while administrators can review document activity.

### Implementation for User Story 3

- [ ] T022 [P] [US3] Add document metadata edit, file replacement, and delete workflow hooks in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T023 [P] [US3] Add `DocumentShare` creation and revocation permission processing in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T024 [US3] Add shared-document and recipient notification integration in `ContosoDashboard/Services/NotificationService.cs`
- [ ] T025 [US3] Add audit logging for document upload, download, share, edit, replace, and delete events in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T026 [US3] Add an administrator-facing document activity reporting surface in `ContosoDashboard/Pages/Notifications.razor`

**Checkpoint**: All user stories should now be independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Connect the new document workflow into the broader dashboard and protect the implementation.

- [ ] T027 [P] Add a recent document widget and document count card integration in `ContosoDashboard/Pages/Index.razor`
- [ ] T028 [P] Add document task attachment hooks in `ContosoDashboard/Pages/Tasks.razor`
- [ ] T029 Security hardening pass across `ContosoDashboard/Services/DocumentService.cs`, `ContosoDashboard/Services/LocalFileStorageService.cs`, and `ContosoDashboard/Pages/Documents.razor`
- [ ] T030 [P] Run the quickstart validation scenario in `specs/001-document-upload-management/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - blocks all user stories
- **User Stories (Phase 3+)**: Depend on Foundational completion and stay independently testable
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) and is the MVP slice
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) and relies on document listing/search abstractions
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) and depends on document permissions and audit service integration

### Parallel Opportunities

- `T001`, `T002`, `T003` can run in parallel because they touch file and configuration scaffolding.
- `T004`, `T005`, `T006` can run in parallel because they describe independent model files under `Models/`.
- `T007`, `T008`, `T009`, `T010`, `T011` can run in parallel when service signatures and folders remain consistent.
- `T012`, `T013`, `T014`, `T015`, `T016` can be parallelized once the foundational service and contract interfaces exist.
- `T017`, `T018`, `T019`, `T020`, `T021` can be parallelized across browse, search, and download flows.
- `T022`, `T023`, `T024`, `T025`, `T026` can be parallelized with careful dependency order around user permissions.

---

## Parallel Example: User Story 1

```bash
# File-level validation and storage modeling are independent once the service contract is defined:
Task: "Add upload request validation and the allowed file-type and 25 MB checks in ContosoDashboard/Services/DocumentService.cs"
Task: "Add GUID-safe, non-web path generation and upload persistence in ContosoDashboard/Services/LocalFileStorageService.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Validate that the upload path, metadata validation, and reject-on-file-rule workflow work independently

### Incremental Delivery

1. Complete Setup + Foundational
2. Add User Story 1
3. Add User Story 2
4. Add User Story 3
5. Complete the Polish and Cross-Cutting integration work

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together.
2. Once the foundation is ready:
   - Developer A: User Story 1
   - Developer B: User Story 2
   - Developer C: User Story 3
3. Polish work remains available to all after stories are complete.

---

## Notes

- `[P]` tasks represent different files or independent service concerns that do not block one another.
- In the final implementation, every story task should carry a story label and include an exact repository file path.
- The smoke path should stay aligned with the secure storage abstraction and local file storage plan.
