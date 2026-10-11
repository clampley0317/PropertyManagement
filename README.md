# Troy Property Management

A full-stack ASP.NET Core MVC application for a property management company that accepts rental applications for its apartments. Property managers maintain properties and units and review applications. Applicants browse available units, complete a rental application, and submit it for review. An approved application issues a twelve-month lease.

Built for the Troy Web Consulting .NET Developer technical assessment.

## Tech stack

- **.NET 10** / ASP.NET Core MVC with Razor views, partial views and view components (no SPA framework)
- **Entity Framework Core 10** with code-first migrations on **SQL Server**
- **ASP.NET Core Identity** for users and roles
- **Bogus** for seed data
- **Bootstrap 5** and plain JavaScript (`fetch`) for modals and partial page refreshes
- **xUnit** for unit tests

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server, SQL Server Express or LocalDB

### 1. Configure the connection string

The default connection string in `appsettings.json` targets a local default SQL Server instance using Windows authentication:

```
Server=localhost;Database=PropertyManagement;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

If your instance is different, override it without editing the file, for example with user secrets:

```powershell
dotnet user-secrets init   # once, adds a UserSecretsId to the project

# SQL Server Express
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost\SQLEXPRESS;Database=PropertyManagement;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"

# LocalDB
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=PropertyManagement;Trusted_Connection=True;MultipleActiveResultSets=true"
```

The `ConnectionStrings__DefaultConnection` environment variable works too.

### 2. Run the application

```powershell
dotnet run --launch-profile https
```

Then browse to https://localhost:7068.

**No manual database setup is needed.** On startup the application creates the database, applies all migrations, and seeds it. Seeding is idempotent: each step checks for existing data, so restarting the app never duplicates records.

### 3. Run the tests

```powershell
dotnet test
```

### Demo accounts

All seeded accounts use the password `Password123!`. In the Development environment the home page also lists them.

| Role             | Email                    |
| ---------------- | ------------------------ |
| Property Manager | `manager@example.com`    |
| Applicant        | `applicant1@example.com` |
| Applicant        | `applicant2@example.com` |

You can also register a new account and choose the Applicant or Property Manager role.

### Seeded data

- **Unit types:** Apartment, Townhouse, Condo, and Loft. Loft is **inactive**, so you can see the inactive-lookup rule.
- **Properties:** 8 properties with 5–10 units each.
- **Applications:** one in **every status** (Draft, Submitted, Returned, Approved, Denied, Withdrawn), each with residence history and a realistic status history. The Draft has its first section saved but not its second.
- **Leases:** the Approved application has an active twelve-month lease, so its unit does not appear as available.

### Resetting the database

```powershell
dotnet tool install --global dotnet-ef   # once
dotnet ef database drop --force
dotnet run
```

## Using the application

### As a property manager

- **Properties** (`/Manager`): add, edit and remove properties through modals. **Manage Units** opens a property's units, which you also add, edit and remove through modals. After each save the modal closes and only the table refreshes.
- **Applications** (`/Applications`): every application, filtered by status and property.
- **Reviewing:** open a Submitted application and click **Review**. Choose Approve, Return or Deny; a comment is required for Return and Deny. Approving creates a twelve-month lease. The status history panel shows who changed what, when, and why.

### As an applicant

- **Available Units** (`/Units`): units with no active lease, filterable by property. **Apply** starts a Draft application. Applying again for the same unit reopens the existing open application.
- **The application** is a single page that shows one section at a time: Applicant Information, Residence History, and Summary. **Save & Continue** validates and saves the current section; **Back** returns without saving. Residences are added, edited and removed through a modal. **Submit** is available from the Summary once both sections are saved.
- **My Applications** (`/Applications`): your own applications, filtered by status and property. A Returned application can be corrected and resubmitted. A Draft, Submitted or Returned application can be withdrawn.

## How the requirements are met

| Requirement                                                                                | Where                                                                                                                                                                                                                   |
| ------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Controllers, view models, Razor views                                                      | `Controllers/`, `ViewModels/`, `Views/`                                                                                                                                                                                 |
| Partial views                                                                              | `_PropertyForm`, `_UnitForm`, `_ResidenceForm`, `_ReviewForm`, the application section partials, `_Alerts`, `_StatusBadge`                                                                                              |
| View components                                                                            | `PropertyTable`, `UnitTable`, `ApplicationReviewPanel` (`ViewComponents/`)                                                                                                                                              |
| Modals from partials; re-render on validation failure; refresh part of the page on success | Property, unit, residence and review modals. Failed validation returns the same partial with errors. On success the server returns JSON, the modal closes, and the affected view component or section is fetched again. |
| Identity users and roles; role choice at sign-up                                           | `Areas/Identity/Pages/Account/Register` overrides only the Register page from the Identity UI                                                                                                                           |
| Permissions in controllers and UI                                                          | `[Authorize(Roles = ...)]` on controllers and actions; navigation and buttons render by role and status                                                                                                                 |
| Migrate and seed idempotently on start, using Bogus                                        | `Program.cs` → `Data/DbSeeder.cs`                                                                                                                                                                                       |
| Unit type lookup with active/inactive values, enforced on the server                       | `ManagerController.ValidateUnitAsync` and `GetUnitTypeOptionsAsync`                                                                                                                                                     |
| Unit availability ("lease covers today")                                                   | `Data/Queries/LeaseQueries.cs` (`ActiveOn`, `AvailableOn`)                                                                                                                                                              |
| Single-page application: one view model, one form, one action, button-driven               | `ApplicationsController.Edit` (POST) with `command` = `continue`, `back` or `submit`; `ApplicationWizardViewModel`                                                                                                      |
| Same partial renders editable or read-only from a server-side decision                     | `_ApplicantInfoSection` and `_ResidenceHistorySection`, reused read-only by `_Summary`                                                                                                                                  |
| Active-lease check at submit and at approval                                               | `ApplicationsController.SubmitAsync` and `Review`                                                                                                                                                                       |
| Review modal with Approve, Return or Deny and a required comment                           | `ReviewApplicationViewModel`, `_ReviewForm`, `ApplicationsController.Review`                                                                                                                                            |
| Status history (who, when, comment) for managers                                           | `ApplicationReviewPanel` view component                                                                                                                                                                                 |
| Application list filtered in the database; applicants see only their own                   | `Data/Queries/ApplicationQueries.cs` (`VisibleTo`, `FilterBy`), `ApplicationsController.Index`                                                                                                                          |
| Unit tests for business logic                                                              | `PropertyManagement.Tests/`                                                                                                                                                                                             |

## Design decisions

**Business rules live in one place each.**

- The application workflow is a single transition table in `Domain/ApplicationRules.cs`. "Terminal" status is derived from it rather than kept as a separate list.
- Status changes go only through `RentalApplication.ChangeStatus`, which enforces the table and always writes a history row.
- Availability (`AvailableOn`) and ownership (`VisibleTo`) are `IQueryable` extensions. They are reused by the browse page, the submit and approval checks, the application list, the detail page and the home page.

**Query rules run in SQL but are tested without a database.** Because the rules are `IQueryable` extensions, EF Core translates them into the SQL `WHERE` clause. The unit tests run the same code against in-memory lists through `AsQueryable()`.

**Lease dates.**

- A lease is active when `StartDate <= today < EndDate`. The end date is exclusive, so on the end date the unit is available again.
- An approved lease starts on the approval date. Allowing a future start date would mean an approved unit still shows as available until then, and the approval check would need overlap logic rather than the simple "covers today" rule.

**Two kinds of concurrency control, for two kinds of conflict.**

- **Two approvals for the same unit** are different rows, so there is no shared row to version. The review action takes an `UPDLOCK` on the unit's row inside a transaction. A second manager approving for the same unit waits, then sees the first lease and is rejected.
- **Two writers on the same application**, for example an applicant withdrawing while a manager approves, are caught by a `rowversion` column. A save based on stale data affects no rows and is rejected with a "reload and try again" message instead of overwriting.

**Validation.**

- Entities define column sizes only. A Draft legitimately has empty fields, so form rules live on view models.
- The wizard validates only the posted section, by checking `ModelState` entries for that section's prefix.
- Rules that span fields, such as move-out after move-in or a comment required for Return and Deny, use `IValidatableObject` and attach each error to the field it belongs to.
- Changing the residence list clears that section's saved flag, so the applicant must confirm it again before submitting.

**Security details.**

- Display-only view model properties such as `IsEditable` and `Status` are `[BindNever]`. The server recomputes them on every request.
- Residence actions are scoped to their application, so a tampered `residenceId` cannot reach another applicant's data.
- Another user's application returns 404 rather than 403, so its existence isn't revealed.
- Endpoints called with `fetch` return a plain 403 status rather than redirecting to the login page.
- The demo credentials on the home page are shown only in Development.
- Empty error responses (403, 404, 409) are re-executed into a friendly page that keeps the original status code. JSON error responses used by `fetch` are left unchanged.

**Deleting never destroys history.** A property can't be deleted while it has units, and a unit can't be deleted once it has applications or leases. The manager gets a message explaining why. Foreign keys use `Restrict` as a second line of defence.

**Post/Redirect/Get.** Every successful full-page POST redirects to a GET, so refreshing never re-submits a form.

**Testable time.** `TimeProvider` is injected wherever "today" matters, so date rules are tested against fixed dates.

## Project structure

```
Areas/Identity/       Register page override (role selection)
Constants/            Role names
Controllers/          Home, Manager (properties and units), Units (browse), Applications
Data/                 DbContext, seeder, Queries/ (reusable IQueryable rules)
Domain/               ApplicationRules: workflow and submit rules
Enums/                ApplicationStatus, ApplicationSection, ReviewOutcome
Migrations/           EF Core code-first migrations
Models/               Entities
ViewComponents/       PropertyTable, UnitTable, ApplicationReviewPanel
ViewModels/           Form, page and row view models
Views/                Razor views and partials
PropertyManagement.Tests/   xUnit tests
```

## Known limitations

- **Bonus items are not implemented.** The `rowversion` column covers conflicts within a single request. Rejecting a save from a form that was opened before someone else saved (bonus #5) would mean sending the version through the form.
- **The concurrency handling has no automated test.** It depends on SQL Server generating `rowversion` values and on row locking, which in-memory tests can't reproduce. It was checked manually by pausing one request in the debugger while a second request changed the same application.
- **Times are stored and shown in UTC.**
- **Each application has a single applicant.**

## Use of AI

AI assistance was used throughout development, and I directed, reviewed and tested all of it:

- **Claude Opus 5.5** (`claude-opus-5-5`, Anthropic), through the Claude Code extension for VS Code, was used as a guide.

I reviewed and ran each change before committing it. I made the design decisions above, can explain them, and am responsible for the code.
