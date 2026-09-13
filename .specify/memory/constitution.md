<!--
Sync Impact Report
Version change: 0.0.0 → 1.0.0
List of modified principles: placeholder scaffold → ContosoDashboard principles
Added sections: Additional Constraints, Development Workflow
Removed sections: none
Follow-up TODOs:
- TODO(RATIFICATION_DATE): original adoption date is not recorded in the repository; set from project history if available.
-->

# ContosoDashboard Constitution

## Core Principles

### I. Security and User Isolation
All user-facing behavior must protect authenticated identity, enforce role-aware access, and prevent data leakage between project members. The application must keep mock authentication, claims-based authorization, and service-level checks aligned with the training scenario and user privacy expectations.

### II. Project and Task Clarity
Every workflow must be understandable from a user perspective and tied to clear project, task, notification, and profile artifacts. Controllers, services, and Razor pages must expose consistent names and state transitions so the training dashboard remains straightforward and traceable.

### III. Test-First Delivery
Before implementation, requirements must be expressed in a testable form, a failing verification must exist, and the final change must be shown to pass that proof. No feature or bug fix may be accepted without evidence that the behavior was validated.

### IV. Integration Integrity
The data model, business services, pages, and UI must stay connected across the repository. Changes to a model, service contract, or page should be reviewed together with downstream usage so data-binding, navigation, authorization, and notifications remain consistent.

### V. Simplicity and Maintainability
The codebase must stay simple, explicit, and explainable for a training context. Prefer obvious service boundaries, reusable patterns, and small changes over clever abstractions that obscure business intent.

## Additional Constraints

The project is an ASP.NET Core Blazor Server training application built around mock authentication, role-aware authorization, and sample data services. It must remain suitable for offline learning and avoid introducing production-only infrastructure, external identity dependencies, or cloud service assumptions into the repository.

The repository must preserve the documented separation of concerns across Models, Data, Services, Pages, and Shared UI resources. Security controls, user isolation, and authorization checks must remain visible and reviewable in the codebase.

## Development Workflow

Every change must be introduced through an identified requirement or scenario and must be reflected consistently in the code, tests, and user-facing behavior. When a new feature is added, the implementation must keep a clear data flow through the service layer and the page or component that presents the result.

Review and validation must confirm that authentication and authorization remain consistent with the repository’s mock security model, that tasks and projects retain clear ownership and visibility boundaries, and that no UI or service path silently bypasses the existing access conventions.

## Governance

This Constitution governs the ContosoDashboard repository and supersedes any informal practices that conflict with it. Amendments require a documented change reason, a version update, and a review of the principles affected by the update.

All pull requests and repository reviews must verify that the current constitution remains satisfied by the work. Principle changes and process changes must be explained in a way that preserves alignment between the project documentation, implementation, and evidence of verification.

**Version**: 1.0.0 | **Ratified**: TODO(RATIFICATION_DATE): original adoption date is not recorded in the repository; set from project history if available. | **Last Amended**: 2026-09-13
