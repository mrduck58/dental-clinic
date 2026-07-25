<!--
Sync Impact Report
==================
Version change: (template) → 1.0.0 (initial ratification)
Modified principles: n/a — all placeholders filled for the first time
Added sections:
  - Core Principles (I–V)
  - Technology Stack & Constraints
  - Development Workflow & Quality Gates
  - Governance
Removed sections: none
Templates requiring updates:
  - .specify/templates/plan-template.md ✅ aligned (Constitution Check gate is populated
    per-feature from this document; no structural change needed)
  - .specify/templates/spec-template.md ✅ aligned (mandatory sections unchanged)
  - .specify/templates/tasks-template.md ✅ aligned (tests remain opt-in per Principle V,
    which requires tests for API business logic but does not mandate TDD everywhere)
Follow-up TODOs: none
-->

# Dental Clinic System Constitution

## Core Principles

### I. Clean Architecture & the Dependency Rule

The backend API (`apps/api`) MUST follow Clean Architecture with the dependency flow
`Presentation → Infrastructure → Application → Domain`. Dependencies always point inward;
an inner layer MUST NOT reference an outer layer.

- **Domain** MUST contain only entities, enums, value objects, domain exceptions, and
  interfaces. It MUST NOT depend on any external framework or library beyond base system
  libraries.
- **Application** MUST express business behavior as use cases (MediatR command/query
  handlers) with DTOs, mappings, and FluentValidation validators. It MUST depend only on
  Domain.
- **Infrastructure** MUST implement interfaces declared in Application or Domain (EF Core
  context, repositories, mail, payment, AI wrappers) and is the only layer performing real
  I/O.
- **Presentation** MUST only receive HTTP requests and dispatch to Application via MediatR;
  controllers MUST NOT contain business logic.

Rationale: isolating business rules from frameworks keeps the core testable and lets
infrastructure (database, payment, AI providers) change without rewriting business logic.

### II. Feature-Isolated Clients

Client applications MUST keep features modular and self-contained.

- The Flutter app (`apps/mobile_app`) MUST follow Feature-First + Clean Architecture: each
  feature owns its `data/`, `domain/`, and `presentation/` folders. Features MUST NOT
  reference each other's presentation layer; any cross-feature communication goes through
  the domain layer.
- The Next.js apps (`apps/clinic_website`, `apps/admin_website`) MUST keep routing in
  `src/app/`, reusable components in `src/components/` (`ui/` and `shared/`), API clients
  in `src/lib/`, global state in `src/stores/`, and shared types in `src/types/`.
- Public marketing pages MUST prefer Server Components for SEO; the admin portal is
  organized by operational modules (`/appointments`, `/patients`, `/inventory`,
  `/invoices`, `/dentists`).

Rationale: four teams/apps share one monorepo; isolation keeps them independently
buildable, testable, and reviewable.

### III. API-First Contracts

All client–server communication MUST go through the documented REST API behind the Nginx
reverse proxy. Endpoints, payloads, and status codes MUST be specified in
`docs/api-endpoints.md` before or alongside implementation. Breaking changes to an existing
endpoint contract MUST be flagged in the PR description and coordinated with every consumer
(web, admin, mobile). Request DTOs MUST be validated (FluentValidation) at the Application
layer; clients MUST NOT rely on undocumented behavior.

Rationale: three independent clients consume one API; an explicit, documented contract is
the only thing keeping them compatible.

### IV. Security & Access Control (NON-NEGOTIABLE)

- Authentication MUST use JWT: short-lived access tokens with refresh tokens stored in
  HttpOnly cookies (web) or secure storage (mobile).
- Authorization MUST use RBAC with the roles `Admin`, `Dentist`, `Receptionist`, `Patient`,
  enforced at the Presentation layer via `[Authorize(Roles = ...)]` attributes. Every
  non-public endpoint MUST declare its allowed roles.
- Secrets (connection strings, JWT secrets, payment and AI keys) MUST live in environment
  configuration (`.env`, never committed); hardcoded credentials are a blocking review
  failure.
- Passwords MUST be stored hashed (BCrypt); patient medical data MUST never appear in logs
  or error responses.

Rationale: the system stores patient health records and processes payments; a single leak
is a legal and trust catastrophe, so security checks are not skippable.

### V. Test & Review Gates

- Business logic in the API's Domain and Application layers MUST have unit tests
  (`apps/api/tests/Domain.Tests`, `Application.Tests`, `Infrastructure.Tests`); a PR that
  adds or changes a use case without covering tests MUST justify why.
- All existing tests MUST pass before merge. Feature-level tests beyond that are opt-in per
  feature specification.
- Every PR MUST be reviewed by at least one other team member against the checklist in
  `docs/git-workflow.md`: behavioral correctness, architectural compliance (Principles I–II),
  security (Principle IV), and code cleanliness (no leftover files or debug artifacts).

Rationale: a small team on a shared integration branch needs cheap, enforced gates rather
than heavyweight process to keep `develop` releasable.

## Technology Stack & Constraints

The stack is fixed per subsystem; introducing a new framework or replacing a pinned one
requires a constitution amendment.

- **Backend**: .NET 9, EF Core, MediatR, FluentValidation, BCrypt. Database is SQL Server
  or PostgreSQL via EF Core migrations (schema rules in `docs/database.md`).
- **Web**: Next.js 15/16 (App Router), React 19, Tailwind CSS v4, TypeScript,
  Zustand/React Context for state.
- **Mobile**: Flutter 3.22+ (stable), GoRouter, Riverpod/BLoC for state.
- **Infra**: Docker Compose for local orchestration, Nginx as reverse proxy
  (`localhost/` → clinic site, `localhost/admin` → admin portal, `localhost/api` → API).
- **External services**: Gemini AI (patient Q&A), Momo/Stripe (payments) — accessed only
  through Infrastructure-layer wrappers.
- Environment setup MUST work from `.env.example` + `docker compose up --build`; a change
  that breaks this quickstart is a regression.

## Development Workflow & Quality Gates

- **Branching**: trunk is `main` (production, PR-only — direct commits are forbidden);
  `develop` is the integration branch; work happens on `feature/*`, `bugfix/*` (from
  `develop`), and `hotfix/*` (from `main`). Branch names follow
  `feature/ten-tinh-nang` or `feature/<issue-id>-<Name>`.
- **Commits**: Conventional Commits (`<type>(<scope>): <description>`) with types
  `feat|fix|docs|style|refactor|test|chore`.
- **PR process**: sync with the target branch and resolve conflicts locally before opening
  a PR; describe the changes and link related issues/tasks; obtain at least one approval
  (Principle V) before merging.
- **Issues first**: new features and bug reports SHOULD be discussed in an Issue before a
  PR is opened, so scope is agreed before code review.
- **Documentation**: changes that alter architecture, database schema, API contracts, or
  workflow MUST update the corresponding file under `docs/` in the same PR.

## Governance

This constitution supersedes ad-hoc practices; where other documents conflict with it, the
constitution wins and the conflicting document MUST be updated.

- **Amendments**: proposed via PR modifying this file, describing the change, its
  motivation, and any migration required. Amendment PRs require review approval like any
  other PR and MUST update dependent templates in `.specify/templates/` in the same change.
- **Versioning**: semantic versioning of this document — MAJOR for removing or redefining
  a principle in a backward-incompatible way, MINOR for adding a principle or materially
  expanding guidance, PATCH for clarifications and wording fixes. The version line below
  MUST be updated with every amendment.
- **Compliance**: every PR review MUST verify compliance with Principles I–V (the review
  checklist in `docs/git-workflow.md` operationalizes this). The `/speckit-plan`
  Constitution Check gate MUST evaluate feature plans against these principles, and any
  violation MUST be justified in the plan's Complexity Tracking table or resolved before
  implementation.

**Version**: 1.0.0 | **Ratified**: 2026-07-14 | **Last Amended**: 2026-07-14
