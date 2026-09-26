# CreditWorks Vehicle Manager

A small ASP.NET Core MVC application for managing vehicles and their weight categories. The vehicle category is calculated from the current category ranges whenever the list is loaded.

## Requirements

- .NET 10 SDK
- Microsoft SQL Server 2019 or later, SQL Server Express, or LocalDB
- EF Core CLI (`dotnet tool install --global dotnet-ef --version 10.0.0`) for migration commands

## Configure and run

The checked-in default connection string uses Windows LocalDB and contains no password or secret. To use another SQL Server instance, override `ConnectionStrings:CreditWorks` with configuration or an environment variable. For example, in PowerShell:

```powershell
$env:ConnectionStrings__CreditWorks = "Server=localhost\SQLEXPRESS;Database=CreditWorks;Trusted_Connection=True;TrustServerCertificate=True"
```

For a SQL login, keep credentials out of source control and provide the connection string through a secret store or an environment variable. Then, from the repository root:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.0
dotnet restore CreditWorks.slnx
dotnet ef database update --project CreditWorks.Web --startup-project CreditWorks.Web
dotnet build CreditWorks.slnx
dotnet run --project CreditWorks.Web
```

The application also applies pending migrations on startup and inserts the initial seed data when the corresponding tables are empty. The explicit `database update` command makes database setup visible and repeatable for reviewers.

To use SQL Server Express, adjust only the `Server` value, such as `localhost\\SQLEXPRESS`. For a local default SQL Server instance, use `Server=localhost`. SQL Server must be running and accessible to the Windows account used to run the application.

## Run tests

```powershell
dotnet test CreditWorks.slnx
```

The tests use EF Core's in-memory provider for fast, isolated application behavior checks; the application itself uses SQL Server. To generate a deployment SQL script after restoring the CLI tool:

```powershell
dotnet ef migrations script --idempotent --project CreditWorks.Web --startup-project CreditWorks.Web --output database.sql
```

## Solution structure

```text
CreditWorks.slnx
CreditWorks.Web/       ASP.NET Core MVC app, EF Core SQL Server persistence, migrations and Razor UI
CreditWorks.Tests/     xUnit business behavior tests
docs/                  Requirements traceability notes
```

Controllers handle HTTP requests and form responses. ViewModels shape and validate posted form data. `VehicleService` and `CategoryService` implement application behavior. `CategoryRangeValidator` contains the range invariants and category lookup logic. `CreditWorksDbContext` configures the SQL Server schema and relationships.

## Data model and decisions

- `Manufacturer` is a lookup table seeded with Mazda, Mercedes, Honda, Ferrari and Toyota. Vehicles reference it with a foreign key; manufacturers are not repeated as hard-coded strings in the form or vehicle table.
- `Vehicle` stores owner name, manufacturer foreign key, manufacture year and `decimal(10,2)` kilogram weight.
- `VehicleCategory` stores its unique name, supported icon key, minimum weight and nullable maximum weight, also as `decimal(10,2)`.
- A vehicle does not store a category foreign key. Its category is derived from its current weight and the current category rows, so category edits immediately change the category displayed for existing vehicles without changing a vehicle's weight.
- Initial ranges are Light `[0, 500)`, Medium `[500, 2500)`, and Heavy `[2500, +∞)`. A lower bound is inclusive; an upper bound is exclusive. Therefore exactly 500.00 kg is Medium and exactly 2500.00 kg is Heavy.
- The first range must start at zero; each finite maximum must equal the next minimum; exactly the final category must be open-ended. Category operations preserve continuity by splitting an existing range on create, moving the adjacent boundary together on edit, and extending the adjacent range on delete. The last category cannot be deleted if it is the only category remaining.
- Category icons are safe, predefined vehicle symbols (`car`, `van`, `truck`, `bus`, and `motorcycle`). This keeps user input from becoming arbitrary markup while still allowing categories to choose a graphic.
- Category ranges and weights allow at most two decimal places. Vehicle weights must be positive. Manufacture years are accepted from 1886 through the current server-local year.

## Validation, security and errors

Razor forms use unobtrusive client validation for immediate feedback. Controllers and services validate inputs again on the server, including manufacturer IDs, manufacture year, weight precision, icon keys and the complete category configuration. State-changing form actions require anti-forgery tokens. EF Core parameterizes database operations; Razor encodes displayed values. Production exceptions are routed to the generic error page and details remain in application logs. No authentication is included because the assignment does not require it.

## Tests and known limitations

The xUnit suite checks valid/invalid range configurations, boundary values, category create/edit/delete behavior, existing vehicle reclassification, vehicle validation and ascending/descending sorting. In-memory provider tests verify application behavior; they do not replace a deployment check against the target SQL Server version.

This is a single-user coding-assignment application. It has no authentication, category change audit history, manufacturer administration screen or concurrency token. A production deployment would add those based on actual access and audit requirements, configure centralized logging/monitoring, use a managed secret store, and consider concurrency handling for simultaneous category edits.
