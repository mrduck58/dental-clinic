# DentalClinic — Project Overview
> Fast-load context doc. Read this before starting a Claude session.
> Updated: 2026-06-30 | Active branch: `feature/135-activity-logs-api`

---

## Apps & Stack

| App | Path | Technology | Notes |
|-----|------|------------|-------|
| REST API | `apps/api/` | .NET 9, ASP.NET Core, EF Core, Npgsql/PostgreSQL | Clean Architecture; primary backend |
| Admin Dashboard | `apps/admin_website/` | Next.js 14, TypeScript, Tailwind CSS, Supabase client | 70+ pages; Admin/Owner/Dentist/Staff |
| Clinic Website | `apps/clinic_website/` | Next.js, TypeScript | Patient-facing; ~10 pages |
| Mobile App | `apps/mobile/` | Flutter | Patient app; ~10 features |

**Database**: PostgreSQL via Supabase (connection string in `appsettings.json`)  
**Realtime**: Supabase Realtime (`postgres_changes`) — enabled on `ActivityLogs` table  
**Auth**: JWT (access + refresh tokens), roles stored per-user in DB

---

## API Architecture

```
Domain/          ← Entities, Interfaces (Repos + Services), Constants, Enums — no dependencies
Application/     ← Use Cases (Handlers), DTOs — depends on Domain only
Infrastructure/  ← EF Core (AppDbContext), Repository impls, ActivityLogService, JwtService, EmailService
Presentation/    ← 18 Controllers, JWT Middleware, DI Registration
tests/           ← Domain.Tests / Application.Tests / Infrastructure.Tests
```

**Patterns**:
- One handler per use case: `Application/UseCases/<Module>/<Action>Handler.cs`
- Repository interfaces in `Domain/Interfaces/Repositories/`, impls in `Infrastructure/Persistence/Repositories/`
- Service interfaces in `Domain/Interfaces/Services/`
- String constants (not CLR enums) in `Domain/Constants/` — used with `switch`/comparison on strings
- CLR enums in `Domain/Enums/`
- Primary constructor DI (C# 12) used throughout
- Thin controllers — all logic in handlers
- `EF.Functions.ILike` for PostgreSQL case-insensitive search (not `ToLower().Contains()`)

---

## Domain Entities (24)

| Entity | PK Type | Notes |
|--------|---------|-------|
| User / Account | `Guid` | 5 roles: Admin, Owner, Dentist, Staff, Patient |
| Appointment | `Guid` | links Dentist + Patient + Service + Room |
| Service | `Guid` | dental services catalog |
| Post | `Guid` | blog / news |
| Schedule | `Guid` | dentist working hours |
| Room | `Guid` | treatment rooms |
| Medicine | `Guid` | medicines catalog |
| Inventory / Stock | `Guid` | stock-in/out transactions (#126) |
| Leave | `Guid` | staff leave requests + approvals |
| Invoice | `Guid` | billing + payment |
| Feedback | `Guid` | patient feedback on services |
| Promotion | `Guid` | discount promotions |
| **ActivityLog** | **`int`** | audit trail — int PK intentional: sequential, insert-ordered, avoids UUID fragmentation |

---

## API Controllers (18)

| Controller | Route | Key Operations |
|-----------|-------|---------------|
| Auth | `/api/auth` | login, refresh-token, logout |
| Accounts | `/api/accounts` | CRUD, role-based, profile fill |
| Appointments | `/api/appointments` | CRUD, filter by role, status transitions |
| Services | `/api/services` | CRUD |
| Posts | `/api/posts` | CRUD, filter by serviceId |
| Schedules | `/api/schedules` | CRUD, dentist working hours |
| Rooms | `/api/rooms` | CRUD |
| Medicines | `/api/medicines` | CRUD |
| Inventory | `/api/inventory` | stock-in, stock-out, transaction history |
| Leave | `/api/leave` | request, approve, reject |
| Invoices | `/api/invoices` | CRUD, payment recording |
| Feedback | `/api/feedback` | CRUD |
| Promotions | `/api/promotions` | CRUD |
| **ActivityLogs** | `/api/activity-logs` | GET paged+filtered; params: action, module, status, search, startDate, endDate, page, pageSize |

---

## Features Done (by Issue)

| # | Feature | Branch | Status |
|---|---------|--------|--------|
| — | Auth (JWT + refresh tokens) | — | ✅ Merged |
| — | Account management (5 roles) | — | ✅ Merged |
| — | Appointments, Services, Posts, Schedule, Room | — | ✅ Merged |
| — | Medicine management | — | ✅ Merged |
| — | Leave management | — | ✅ Merged |
| — | Invoice + Payment | — | ✅ Merged |
| — | Feedback, Promotions | — | ✅ Merged |
| 126 | Stock In / Stock Out (Inventory) | `feat/126-Stock-In-Stock-Out` | ✅ Merged |
| **135** | **Activity Logging System — full stack** | `feature/135-activity-logs-api` | 🔄 In Progress |

---

## Feature #135 — Activity Logging System (current)

### API — New Files
- `Domain/Entities/ActivityLog.cs` — entity with int PK, factory method `Create(...)`
- `Domain/Constants/ActivityAction.cs` — login, create, edit, delete, export, view, approve, reject, cancel, payment
- `Domain/Constants/ActivityModule.cs` — account, appointment, service, post, schedule, room, medicine, inventory, leave, invoice, feedback, promotion, system
- `Domain/Constants/ActivityStatus.cs` — success, failed, warning
- `Infrastructure/Services/ActivityLogService.cs` — fire-and-forget; swallows exceptions; logs Warning on failure
- `Infrastructure/Repositories/ActivityLogRepository.cs` — `AddAsync` + `GetPagedAsync` with `ILike` search
- `Application/UseCases/ActivityLogs/GetActivityLogsHandler.cs` — paged query; `endDate` normalization
- `Presentation/Controllers/ActivityLogsController.cs` — GET `/api/activity-logs`

### API — Modified (25 handlers)
All existing handlers performing user-visible actions inject `IActivityLogService` and call `LogAsync(...)`.  
Covered: login, account CRUD, appointment transitions, medicine, inventory, leave, invoice, post, schedule, room.

### Frontend — admin_website
- `app/admin/activity-logs/page.tsx` — full rewrite from mock data to real API
  - Supabase Realtime subscription: `postgres_changes` INSERT on `ActivityLogs` table
  - `useRef(currentPage)` pattern to avoid stale closure in realtime callback
  - New-record banner: "X hoạt động mới — xem ngay" button
  - CSV export with current filter state (pageSize: 10000)
  - Realtime badge: animate-ping green dot + "Realtime" label in header

### Tests Added (24 new)
| File | Tests |
|------|-------|
| `Domain.Tests/ActivityLogs/ActivityLogTests.cs` | 9 |
| `Application.Tests/ActivityLogs/GetActivityLogsHandlerTests.cs` | 9 |
| `Infrastructure.Tests/Services/ActivityLogServiceTests.cs` | 6 |

### Commits (branch: `feature/135-activity-logs-api`)
```
6ec6e1d refactor(api):#135-clean up activity log code structure
abb880b test(api, admin):#135-fix broken tests and add NUnit tests for activity logging
b51dae4 feat(api, admin):#135-implement activity logging system end-to-end
```

---

## Test Coverage

| Project | Files | Tests |
|---------|-------|-------|
| Domain.Tests | ~20 | ~80 |
| Application.Tests | ~45 | ~180 |
| Infrastructure.Tests | ~23 | ~62 |
| **Total** | **88** | **322** |

**Stack**: NUnit 4 + NSubstitute + FluentAssertions  
**Run**: `dotnet test` from `apps/api/`  
**Rule**: All must pass before merge. Infrastructure tests are integration-style — no DB mocks.

---

## Key Technical Decisions

| Decision | Rationale |
|----------|-----------|
| `int` PK on ActivityLog | Sequential, insert-ordered; no UUID index fragmentation on append-only audit table |
| `EF.Functions.ILike` for search | Native PostgreSQL case-insensitive; faster than `ToLower().Contains()` (which forces full-scan) |
| `endDate` normalization | If `TimeOfDay == TimeSpan.Zero` (date-only input), extend to `AddDays(1).AddTicks(-1)` — full day included |
| ActivityLogService swallows exceptions | Log failure must never break the main business flow; logs a Warning so engineers see dropped logs |
| Constants in `Domain/Constants/` | String sentinel values for `switch`/comparison — distinct from CLR enums in `Domain/Enums/` |
| `useRef` for page in realtime callback | Stale closure: callback captures initial `currentPage` state; ref always holds the current value |
| Supabase Realtime on INSERT only | Sufficient for audit log; UPDATE/DELETE not applicable to append-only log |
| Primary constructor DI (C# 12) | Used throughout entire API; cleaner than separate field + constructor body |
| `ICurrentUserService` | Abstracts `HttpContext` user claims; injected into handlers needing current user (userId, role, email) |

---

## File Quick-Reference

```
apps/api/src/
  Domain/
    Entities/               ← 24 entities
    Interfaces/
      Repositories/         ← 16 interfaces (IActivityLogRepository, IAppointmentRepository, ...)
      Services/             ← IActivityLogService, IJwtService, IEmailService, ICurrentUserService
    Constants/              ← ActivityAction, ActivityModule, ActivityStatus (string values)
    Enums/                  ← EmploymentType, SalaryUnit, ... (CLR enums)
  Application/
    UseCases/               ← 83 handlers, one per use case
    DTOs/                   ← request/response records
  Infrastructure/
    Persistence/
      AppDbContext.cs
      Repositories/         ← EF Core implementations
    Services/               ← ActivityLogService, JwtService, EmailService
  Presentation/
    Controllers/            ← 18 thin controllers
    Middleware/             ← JWT auth
    DependencyInjection/    ← service registration
tests/
  Domain.Tests/
  Application.Tests/
  Infrastructure.Tests/

apps/admin_website/src/app/admin/
  activity-logs/page.tsx    ← Realtime + real API (current work)
  dashboard/
  appointments/
  accounts/
  inventory/
  ... (70+ pages total)
```
