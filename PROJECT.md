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
**Realtime**: Supabase Realtime (`postgres_changes`) — enabled on `ActivityLogs` and `Notifications` tables  
**Auth**: JWT (access + refresh tokens), roles stored per-user in DB

---

## API Architecture

```
Domain/          ← Entities, Interfaces (Repos + Services), Constants, Enums — no dependencies
Application/     ← Use Cases (Handlers), DTOs — depends on Domain only
Infrastructure/  ← EF Core (AppDbContext), Repository impls, Services
Presentation/    ← 19 Controllers, JWT Middleware, DI Registration
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

## Domain Entities (25)

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
| **Notification** | **`Guid`** | per-user notifications with type, priority, read state, optional relatedEntity |

---

## API Controllers (19)

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
| **Notifications** | `/api/notifications` | GET paged+filtered, PUT read, PUT read-all, DELETE |

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
| **135** | **Activity Logging + Notification System — full stack** | `feature/135-activity-logs-api` | 🔄 In Progress |

---

## Feature #135 — Activity Logging System

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

### Tests Added (24)
| File | Tests |
|------|-------|
| `Domain.Tests/ActivityLogs/ActivityLogTests.cs` | 9 |
| `Application.Tests/ActivityLogs/GetActivityLogsHandlerTests.cs` | 9 |
| `Infrastructure.Tests/Services/ActivityLogServiceTests.cs` | 6 |

---

## Feature #135 — Notification System (same branch)

### Notification triggers (who gets notified and when)

| Event | Recipient | Type | Priority |
|-------|-----------|------|----------|
| Staff/Dentist creates LeaveRequest | **All Owners** | Schedule | Medium |
| Admin/Owner approves LeaveRequest | Requester (Staff/Dentist) | Schedule | Medium |
| Admin/Owner rejects LeaveRequest | Requester (Staff/Dentist) | Schedule | High |
| Patient books Appointment | **Dentist** (assigned) | Appointment | High |
| Patient books Appointment | **All Staff** | Appointment | High |
| Staff confirms Appointment | Dentist | Appointment | Medium |
| Patient/Staff cancels Appointment | Dentist | Appointment | High |
| Patient checks in (Appointment) | Dentist | Appointment | High |

### API — New Files
- `Domain/Entities/Notification.cs` — Guid PK, UserId, Type, Priority, Title, Body, IsRead, ReadAt, RelatedEntityType?, RelatedEntityId?
- `Domain/Interfaces/Services/INotificationService.cs` — `CreateAsync`, `CreateForMultipleUsersAsync`, `MarkAsReadAsync`, `MarkAllAsReadAsync`, `DeleteAsync`
- `Domain/Constants/NotificationType.cs` — System, Account, Schedule, Service, Security, Reminder, Appointment
- `Domain/Constants/NotificationPriority.cs` — High, Medium, Low
- `Infrastructure/Services/NotificationService.cs` — fire-and-forget pattern; all methods wrapped in try/catch
- `Infrastructure/Repositories/NotificationRepository.cs` — paged query with type/priority/isRead/search filters; `unreadCount` in response
- `Application/UseCases/Notifications/` — GetNotificationsHandler, MarkNotificationReadHandler, MarkAllNotificationsReadHandler, DeleteNotificationHandler
- `Presentation/Controllers/NotificationsController.cs` — GET/PUT/DELETE `/api/notifications`

### API — Modified handlers (notification injection)
- `CreateLeaveRequestHandler` — notifies **Owner** role (was Admin); `IUserRepository.GetUserIdsByRoleAsync("Owner")`
- `ApproveLeaveRequestHandler` — notifies requester (Schedule/Medium)
- `RejectLeaveRequestHandler` — notifies requester (Schedule/High)
- `CreateAppointmentHandler` — notifies dentist (Appointment/High) + **all Staff** (Appointment/High)
- `UpdateAppointmentStatusHandler.ConfirmAsync` — notifies dentist (Appointment/Medium)
- `UpdateAppointmentStatusHandler.CancelAsync` — notifies dentist (Appointment/High)
- `UpdateAppointmentStatusHandler.CheckInAsync` — notifies dentist (Appointment/High)
- `IAppointmentRepository` — added `GetDentistUserIdAsync(Guid dentistId)` to bridge DentistId → UserId

### Frontend — Notification pages (4 roles)
| Page | Path | Sidebar entry |
|------|------|--------------|
| Admin | `app/admin/notifications/page.tsx` | Added to `AdminSidebar` |
| Dentist | `app/dentist/notifications/page.tsx` | Added to `DentistSidebar` |
| Staff | `app/staff/notifications/page.tsx` | Already in `StaffSidebar` |
| Owner | `app/owner/notifications/page.tsx` | Already in `OwnerSidebar` |

**Common page features** (all 4 roles):
- Filter tabs: all / unread / type-specific (role-dependent)
- Group notifications by date
- Optimistic mark-read + delete (refetch on error)
- Loading spinner + empty state
- "· Xem chi tiết →" label + clickable `<Link>` when `relatedEntityType` is set

**Role-specific navigation on click**:
| Role | relatedEntityType | Navigates to |
|------|------------------|-------------|
| Owner | `LeaveRequest` | `/owner/leaves/{id}` |
| Staff | `Appointment` | `/staff/appointments` |

### Frontend — NotificationBell component
- `components/shared/NotificationBell.tsx` — dropdown with last 5 notifications
- Supabase Realtime: subscribes to INSERT on `Notifications` table → auto-refresh
- `useRef` pattern to avoid stale closure in Realtime callback
- Props: `href` — "Xem tất cả" link destination (role-specific)
- Embedded in: `DentistPageHeader`, `StaffPageHeader`, `ProfilePageContent`, all admin/owner inline headers
- `admin/leaves/page.tsx` and `admin/leaves/[id]/page.tsx` — replaced static bell button

### Frontend — Sidebar updates
| Sidebar | Change |
|---------|--------|
| `AdminSidebar` | Added "Thông báo" link → `/admin/notifications` |
| `DentistSidebar` | Added "Thông báo" entry to `NAV_ITEMS` → `/dentist/notifications` |
| `StaffSidebar` | Already had "Thông báo" |
| `OwnerSidebar` | Already had "Thông báo" |

### Tests Added (notification — current session)
| File | Tests | Notes |
|------|-------|-------|
| `Application.Tests/LeaveRequests/CreateLeaveRequestHandlerTests.cs` | 7 | Fixed constructor (added `INotificationService`, `IUserRepository`); added 2 notification tests |
| `Application.Tests/Appointments/UpdateAppointmentStatusHandlerTests.cs` | existing+3 | Fixed constructor (added `INotificationService`); added dentist-notify tests for Confirm/Cancel/CheckIn |
| `Application.Tests/Appointments/CreateAppointmentHandlerTests.cs` | 7 | New file; tests patient creation, slot conflict, dentist notify, staff notify |

---

## Test Coverage

| Project | Files | Tests |
|---------|-------|-------|
| Domain.Tests | ~20 | ~80 |
| Application.Tests | ~48 | ~197 |
| Infrastructure.Tests | ~23 | ~62 |
| **Total** | **~91** | **~339** |

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
| NotificationService swallows exceptions | Same rationale — notification failure must not break appointment/leave business flows |
| `CreateNotificationRequest` parameter object | Avoids SonarQube S107 (max 7 constructor params) on handlers that also inject ActivityLog + CurrentUser |
| `GetDentistUserIdAsync` on IAppointmentRepository | `Appointment.DentistId` is a Dentist profile ID, not a UserId — needs a join to resolve; kept in repo layer |
| Leave notifications → Owner (not Admin) | Owner is the role responsible for HR decisions; Admin is technical/system role |
| Appointment notifications → Staff | Staff (receptionists) manage scheduling and need to know about new bookings immediately |
| Constants in `Domain/Constants/` | String sentinel values for `switch`/comparison — distinct from CLR enums in `Domain/Enums/` |
| `useRef` for page in realtime callback | Stale closure: callback captures initial state; ref always holds the current value |
| Supabase Realtime on INSERT only | Sufficient for audit log and notifications; UPDATE/DELETE not applicable to append-only patterns |
| Primary constructor DI (C# 12) | Used throughout entire API; cleaner than separate field + constructor body |
| `ICurrentUserService` | Abstracts `HttpContext` user claims; injected into handlers needing current user (userId, role, email) |

---

## File Quick-Reference

```
apps/api/src/
  Domain/
    Entities/               ← 25 entities (incl. ActivityLog, Notification)
    Interfaces/
      Repositories/         ← 17 interfaces (IActivityLogRepository, INotificationRepository, ...)
      Services/             ← IActivityLogService, INotificationService, IJwtService, IEmailService, ICurrentUserService
    Constants/              ← ActivityAction/Module/Status, NotificationType, NotificationPriority (string values)
    Enums/                  ← EmploymentType, SalaryUnit, ... (CLR enums)
  Application/
    UseCases/               ← 90+ handlers, one per use case
    DTOs/                   ← request/response records
  Infrastructure/
    Persistence/
      AppDbContext.cs
      Repositories/         ← EF Core implementations
    Services/               ← ActivityLogService, NotificationService, JwtService, EmailService
  Presentation/
    Controllers/            ← 19 thin controllers
    Middleware/             ← JWT auth
    DependencyInjection/    ← service registration
tests/
  Domain.Tests/
  Application.Tests/
  Infrastructure.Tests/

apps/admin_website/src/
  app/
    admin/
      notifications/page.tsx     ← real API, stats cards, pagination, type filters
      activity-logs/page.tsx     ← Realtime + real API
      leaves/page.tsx            ← NotificationBell (replaced static button)
      leaves/[id]/page.tsx       ← NotificationBell (replaced static button)
    dentist/notifications/page.tsx
    staff/notifications/page.tsx ← tab "Đặt lịch", links to /staff/appointments
    owner/notifications/page.tsx ← links LeaveRequest → /owner/leaves/{id}
  components/shared/
    NotificationBell.tsx         ← dropdown, Supabase Realtime, role-aware href
    AdminSidebar.tsx             ← added "Thông báo" nav item
    DentistSidebar.tsx           ← added "Thông báo" to NAV_ITEMS
    StaffSidebar.tsx             ← already had "Thông báo"
    OwnerSidebar.tsx             ← already had "Thông báo"
    ProfilePageContent.tsx       ← added NotificationBell + notificationHref prop
    DentistPageHeader.tsx        ← has NotificationBell built in
    StaffPageHeader.tsx          ← has NotificationBell built in
```
