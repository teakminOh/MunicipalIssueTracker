# Municipal Issue Tracker

A web application for registering and managing municipal infrastructure issues — potholes, broken street lights, waste problems, greenery maintenance, and other city maintenance incidents.

Built as a realistic portfolio project demonstrating **.NET 8**, **Blazor**, **Radzen**, **EF Core**, **SQL reporting**, and **Leaflet map integration** — aligned with municipal digitalization roles.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **Frontend** | Blazor Server (Interactive), Radzen Blazor Components |
| **Backend** | .NET 8, ASP.NET Core |
| **Database** | SQLite (via EF Core), easily swappable to SQL Server |
| **Maps** | Leaflet.js with OpenStreetMap tiles |
| **Auth** | Cookie authentication with role-based authorization |
| **Testing** | xUnit, EF Core InMemory provider |
| **CI/CD** | Azure DevOps YAML pipeline |

## Architecture

```
┌─────────────────────────────────────┐
│  MunicipalIssueTracker.Web          │  Blazor Server + Radzen UI
│  (Pages, Services, JS Interop)      │  Cookie Auth, Minimal API
├─────────────────────────────────────┤
│  MunicipalIssueTracker.Domain       │  Entities, Enums, Interfaces
│  (Zero dependencies)                │
├─────────────────────────────────────┤
│  MunicipalIssueTracker.Infrastructure│ EF Core, DbContext, Migrations
│  (Data access, File storage)        │  LocalFileStorage / IFileStorage
└─────────────────────────────────────┘
```

Clean Architecture with pragmatic 3-layer separation:
- **Domain** — entities, enums, and contracts. No external dependencies.
- **Infrastructure** — EF Core DbContext, migrations, repositories, file storage.
- **Web** — Blazor pages, Radzen components, services, authentication, JS interop.

## Features

### Core
- **Issue CRUD** — Create, edit, view, and manage municipal issues
- **Radzen DataGrid** — Sortable, pageable, filterable issue list
- **Status workflow** — Reported → Confirmed → In Progress → Resolved → Closed
- **Assignment** — Assign issues to operators
- **Comments** — Add comments to issues with audit trail
- **Audit logging** — Track all changes with JSON details

### Map / GIS
- **Leaflet map** — Interactive map with issue markers
- **Color-coded markers** — By priority (Critical=red, High=orange, Medium=blue, Low=grey)
- **Popup navigation** — Click markers to view issue details
- **Bounding-box query** — Viewport-based filtering support
- **OpenStreetMap tiles** — Free, with proper attribution

### Reporting / SQL
- **Dashboard** — Summary cards (total, open, resolved, critical)
- **District × Status report** — JOIN + GROUP BY with chart visualization
- **Category × Priority report** — Multi-dimension grouping
- **Latest comment per issue** — Correlated subquery demonstration

### Authentication
- Cookie-based authentication with roles:
  - **Admin** — Full access
  - **Operator** — Can manage and update issues
  - **Viewer** — Read-only access

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git

### Run Locally

```bash
git clone <repository-url>
cd MunicipalIsuueTracker

# Restore and build
dotnet build

# Run the web application
dotnet run --project src/MunicipalIssueTracker.Web

# Or with hot reload
dotnet watch --project src/MunicipalIssueTracker.Web
```

The app will start at `https://localhost:5001` (or the port shown in console).

The database is automatically created and seeded on first run.

### Demo Credentials

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@municipality.se | Admin123! |
| Operator | erik@municipality.se | Operator123! |
| Operator | maria@municipality.se | Operator123! |
| Viewer | anna@municipality.se | Viewer123! |

### Run Tests

```bash
dotnet test
```

## Project Structure

```
MunicipalIssueTracker/
├── src/
│   ├── MunicipalIssueTracker.Domain/          # Entities, Enums, Interfaces
│   ├── MunicipalIssueTracker.Infrastructure/  # EF Core, Migrations, Storage
│   └── MunicipalIssueTracker.Web/             # Blazor Server App
│       ├── Components/Pages/                  # Blazor pages
│       ├── Services/                          # Business logic
│       └── wwwroot/js/                        # Leaflet map interop
├── tests/
│   └── MunicipalIssueTracker.Tests/           # xUnit tests
├── tools/
│   └── MunicipalIssueTracker.WinFormsImporter/ # WinForms CSV import tool
│       ├── MainForm.cs                        # UI + CSV parsing + API call
│       ├── sample-issues.csv                  # Sample import data
│       └── .csproj                            # .NET 8 (retargetable to 4.7.2)
├── azure-pipelines.yml                        # CI/CD pipeline
└── README.md
```

## Data Model

The domain model includes:
- **Issues** — Core entity with title, description, coordinates, priority, status
- **Users** — With roles (Admin, Operator, Viewer)
- **Districts** — Municipal districts (Stockholm-based demo data)
- **Categories** — Issue types (Pothole, Street Light, Waste, etc.)
- **Statuses** — Workflow states with sort order
- **Comments** — Issue discussion thread
- **Attachments** — File attachment support via IFileStorage abstraction
- **AuditLogs** — Change tracking with JSON details

## SQL Competency Demonstration

The project explicitly demonstrates SQL skills beyond ORM usage:

1. **JOIN + GROUP BY** — Issues by district and status with aggregate counts
2. **Multi-dimension grouping** — Issues by category and priority
3. **Correlated subquery** — Latest comment per issue
4. **Bounding-box spatial query** — Coordinate-based map filtering
5. **EF Core LINQ** — Standard CRUD with navigation property inclusion

Raw SQL queries are intentionally visible in `ReportingService.cs`.

## Development Workflow

- **Git** — Feature branch workflow
- **Commits** — Conventional commit messages
- **PR-ready** — Clean code review structure
- **CI Pipeline** — Azure DevOps Server compatible YAML
- **Separation of concerns** — Domain, Infrastructure, Web layers

## Extensibility Points

- `IFileStorage` interface — Currently `LocalFileStorage`, easily replaced with Azure Blob Storage
- SQLite → SQL Server — Change connection string in `appsettings.json`
- Additional reporting — Add queries to `ReportingService`

## WinForms CSV Importer

A companion Windows Forms tool for bulk-importing issues from CSV files.

Located in `tools/MunicipalIssueTracker.WinFormsImporter/`.

**Features:**
- Browse and load CSV files
- Preview parsed issues in a DataGridView
- Send issues to the web app via REST API (`POST /api/issues/import`)
- Handles quoted CSV fields

**CSV Format** (see `sample-issues.csv`):
```
Title,Description,Category,District,Lat,Lng,Address,Priority
```

**Running on Windows:**
```bash
dotnet run --project tools/MunicipalIssueTracker.WinFormsImporter
```

> **Note:** The project targets .NET 8 (windows) for development convenience. It is designed to be easily retargeted to .NET Framework 4.7.2 for legacy Windows environments — see comments in the `.csproj` file.

## Import API

The web app exposes a Minimal API endpoint for external integrations:

```
POST /api/issues/import
Content-Type: application/json

[
  {
    "title": "Pothole on Main St",
    "description": "Large pothole near crossing",
    "category": "Pothole",
    "district": "Centrum",
    "lat": 59.33,
    "lng": 18.07,
    "address": "Main St 10",
    "priority": "High"
  }
]
```

---

Built with .NET 8 | Blazor Server | Radzen | EF Core | Leaflet
