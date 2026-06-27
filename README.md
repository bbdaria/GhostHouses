# GhostHouses
Technion, Faculty of Computer Science

2025-2026-Winter Semester - 02340311 - Yearly Project in Software Eng.-Stage A

2025-2026-Spring Semester - 02340312 - Yearly Project in Software Eng.-Stage B

## Team
**Supervisor:** [Daria Bebin](https://github.com/bbdaria)

**Team members:**
- [Kareem Araide](https://github.com/KareemAraide)
- [Benny](https://github.com/StrBenny)
- [Carole Lasmar](https://github.com/carolelasmar)
- [Yara Zeineh](https://github.com/YaraZeineh)
- [bkillercode](https://github.com/bkillercode)
- [L54](https://github.com/L54)

## Project Overview
GhostHouses is a municipal web system for tracking vacant/rehabilitation buildings. It supports structured data entry, import/export workflows, an audit log, and presentation‑ready building cards. The system is built with a .NET backend, React frontend, and PostgreSQL, and is fully containerized for local development.

## Current Capabilities (based on implemented issues)
### Buildings
- Create / edit / view building details with categorized fields.
- Mandatory field validation and conditional rules (e.g., Rehab Status only for relevant classification).
- Advanced filters (including date range for last update).
- Table sorting by clicking column headers.
- Building actions: edit, delete (with safeguards), export to Excel, export building card, and open logs.
- Supports 0–1 image per building (used in building card export).

### Activity Logs
- Full audit trail with per‑field changes.
- Filters by user/date/fields and sorting by header click.
- Log table shows key fields directly (no dropdown rows).
- Logs are immutable (no delete UI).

### Import / Export (Buildings)
- Excel export respects current UI field order and labels.
- Selection‑based export (headers only if nothing selected).
- Export with images as ZIP (Excel + images) or Excel‑only.
- Import with staged validation:
  - Stage 1: fix missing/invalid mandatory fields.
  - Stage 2: resolve conflicts (skip/replace/add‑anyway).
- Duplicate detection (by address and by ID) with explicit resolution.

### Streets
- Dedicated Streets page with add/edit/view.
- Import/export with conflict resolution (StreetId uniqueness).
- Selection‑based export (headers only if none selected).
- “No Street Name” is reserved for buildings only (StreetId = -1).

### Users & Permissions
- User management (create/edit/view) in popups.
- Role model (Viewer / Editor / Admin).
- OTP reset actions.

### Building Cards (PPTX)
- Export single or multiple building cards as PPTX.
- Each selected building becomes a slide in a deck.
- Template‑based with image replacement and aspect‑ratio preservation.
- GIS snapshots are inserted into the card map placeholder when the building can be located.
- The card building is highlighted in red, and nearby buildings that exist in the system database are highlighted in blue.

### GIS
- GIS map page using Haifa municipality map services.
- Buildings can be opened from the Buildings page directly on the GIS map.
- All mapped database buildings are shown on the map when GIS location data is available.
- Users can select an area on the map and view/export the matching system buildings.
- GIS is an active integration, not a mock.

### Admin Template Converter
- Converter page to migrate legacy client templates into the current system format.

## Open / Planned Items (from open issues)
- Real OTP implementation and stronger 2FA enforcement.
- Additional external municipality system integrations beyond GIS, after the municipality provides real API/file contracts.
- Final deployment/handoff on the client Windows Server environment.
- Update building card template (awaiting client template).

## Tech Stack
- **Backend:** ASP.NET Core (.NET 8, C#)
- **Frontend:** React (Vite)
- **Database:** PostgreSQL
- **Testing:** xUnit (`tests/WebServer.Tests`) for backend automated tests, plus frontend production build validation.
- **Containerization:** Docker + Docker Compose
- **CI/CD:** GitHub Actions for issue guard, backend tests, frontend build, and Docker Compose build validation.

## Installation & Running
```bash
git clone https://github.com/bbdaria/GhostHouses.git
cd GhostHouses/project
cp .env.example .env
# Fill .env with the real secrets from the team shared drive / secure handoff mail.
docker compose up -d --build
```

Before running, make sure these local files exist:
- `project/.env` with the real database, pgAdmin, and JWT secrets.
- `project/certs/dev.crt` and `project/certs/dev.key` for HTTPS.

Do not commit `project/.env` or anything under `project/certs/`. They are ignored by git and should be shared only through the team shared drive or secure handoff mail. See `project/HANDOFF_SECRETS.md` for the exact file locations.

On a clean database, the backend creates one initial administrator account: `admin / admin`. Give this account only to the department owner during handoff. They can create the real municipality users and then change or disable the initial admin account.

The deployment server must have outbound internet access so Docker can pull base images and build dependencies during `docker compose up -d --build`.

### Deployment simplicity
Deployment is handled through one top-level Docker Compose file: `project/docker-compose.yml`. From the `project/` folder, one command builds and starts the full system:

```bash
docker compose down -v && docker compose up -d --build
```

This deployment path is intentionally simple and repeatable:
- One command runs the full stack: frontend, backend, PostgreSQL, and pgAdmin.
- Docker Compose builds the project services and pulls required public images when needed.
- PostgreSQL starts with a health check before the backend is used.
- Docker networks are created automatically with the intended isolation: `app-net`, `db-net`, and `admin-net`.
- `down -v` resets the database and pgAdmin volumes, which gives a clean deployment state when needed.
- The only required local files are the documented `project/.env`, `project/certs/dev.crt`, and `project/certs/dev.key`.

### Ports & Networking (local Docker)
- **Frontend (Nginx)**: `https://localhost:443` (host port 443 -> container 443).
- **Backend (ASP.NET Core)**: internal only, `http://backend:8080` (no host port mapping).
- **Database (PostgreSQL)**: internal only, `db:5432`.
- **pgAdmin**: `https://localhost:8443` (host port 8443 -> container 443).
- Networks: `app-net` (frontend ↔ backend), `db-net` (backend ↔ db), `admin-net` (pgAdmin ↔ db).

## Deployment Requirements and Dependencies
This section documents the minimum environment needed to deploy GhostHouses and maps to the deployment grading rubric: deployment UML, software dependencies, and minimal hardware requirements.

The UML diagrams are written in standard UML 2.5.1 style and rendered with PlantUML.

### Deployment UML
The Stage B deployment UML is located here:
- Source: `docs/submissions/stage-b/uml/deployment/deployment_environment.puml`
- Rendered image: `docs/submissions/stage-b/uml/deployment/deployment_environment.png`

The diagram shows the complete Docker-based runtime environment:
- Municipality users access the system only from inside the municipality network, through the frontend over HTTPS on port 443.
- The frontend container serves the React application through Nginx.
- The backend container runs the ASP.NET Core API internally on port 8080 and is not exposed directly outside Docker.
- PostgreSQL runs internally on port 5432 and is not exposed directly outside Docker.
- pgAdmin is exposed separately over HTTPS on port 8443 for IT/database administration only, also inside the municipality network.
- Docker networks isolate traffic by purpose:
  - `app-net`: frontend to backend.
  - `db-net`: backend to PostgreSQL.
  - `admin-net`: pgAdmin to PostgreSQL.
- The deployment server has outbound internet access so Docker can pull public base images during build/deployment.
- The backend has outbound HTTPS access to the public Haifa Municipality GIS / ArcGIS API, which is an active integration.
- OTP is mocked inside the backend for this deployment, so there is no external OTP provider or network dependency shown in the UML.

### Software Dependencies
GhostHouses is deployed through Docker Compose, so the server does not need manual installation of .NET, Node.js, PostgreSQL, or Nginx. Those dependencies are provided by Docker images during build and runtime.

| Dependency | Version / Source | Why it is needed |
| --- | --- | --- |
| Windows Server VM | Municipality-provided server | Target deployment host |
| Docker + Docker Compose | Installed on the server | Builds and runs the full application stack |
| WSL 2 / nested virtualization | Needed if Docker Desktop is used on a Windows Server VM | Required for Linux containers on Docker Desktop |
| PostgreSQL image | `postgres:16` | Application database |
| pgAdmin image | `dpage/pgadmin4:latest` | Database administration UI for IT/DB admins |
| .NET SDK image | `mcr.microsoft.com/dotnet/sdk:8.0` | Builds the ASP.NET Core backend |
| ASP.NET runtime image | `mcr.microsoft.com/dotnet/aspnet:8.0` | Runs the backend container |
| Node image | `node:20` | Builds the React/Vite frontend |
| Nginx image | `nginx:alpine` | Serves the built frontend over HTTPS |
| TLS certificate and key | `project/certs/dev.crt`, `project/certs/dev.key` | Enables HTTPS for frontend and pgAdmin |
| Environment file | `project/.env` | Provides database, pgAdmin, and JWT configuration |
| Outbound HTTPS access | Port 443 | Pulls Docker images during build/deployment and calls the Haifa Municipality GIS API at runtime |

No real external OTP provider is required for this deployment because OTP is currently mocked inside the backend.

### Minimal Hardware Requirements
Recommended deployment VM:

| Resource | Requirement |
| --- | --- |
| CPU | 4 vCPU |
| RAM | 8 GB |
| Storage | 100 GB SSD |
| OS | Windows Server VM |
| Runtime | Docker + Docker Compose |
| Virtualization | Nested virtualization enabled if Docker Desktop / WSL 2 is used |
| Network | Municipality LAN access, inbound HTTPS 443, admin HTTPS 8443, outbound HTTPS 443 |

How we figured these requirements:
- The deployment runs four containers: frontend, backend, PostgreSQL, and pgAdmin.
- Docker Desktop / WSL 2 on a Windows Server VM adds virtualization overhead.
- PostgreSQL needs persistent storage for building data, logs, users, uploaded images, and future growth.
- Building-card exports and GIS snapshots are heavier than normal page views, so 8 GB RAM gives safe headroom.
- The system is for internal municipality users, not high-volume public traffic, so 4 vCPU and 8 GB RAM are enough for the expected workload.
- 100 GB SSD gives room for the repository, Docker images, database volume, pgAdmin volume, logs, uploaded images, generated exports, and growth margin.

## Maintainable Architecture
This section documents the architecture evidence for the maintainability grading rubric.

### UML Class Diagram
The Stage B class UML is located here:
- Source: `docs/submissions/stage-b/uml/class/class_diagram.puml`
- Rendered image: `docs/submissions/stage-b/uml/class/class_diagram.png`

The diagram was rebuilt from the current codebase as a presentation-friendly UML class diagram. It shows the main maintainability decisions without listing every DTO or helper record:
- API layer: controllers, common controller bases, and the separation between authenticated API controllers and simpler controllers.
- Service layer: replaceable contracts for audit, JWT, OTP, and GIS behavior.
- Validation/import layer: shared building and street rules reused by manual editing and Excel import.
- Persistence layer: `AppDbContext` as the EF Core boundary for PostgreSQL entities.
- Domain layer: the main business entities and relationships used by the system.

Why this matters for maintainability:
- The diagram shows the important abstract classes directly, especially `ControllerBase` and `ApiControllerBase`, so shared controller behavior is visible instead of hidden in text.
- It shows interface contracts separately from implementations, which makes it clear where future replacement is possible.
- It shows that business entities are not mixed into controllers or deployment code, they remain in the domain layer.
- It shows the persistence boundary explicitly, so database access is concentrated through `AppDbContext`.
- It shows the active GIS integration and mocked OTP boundary without making either one look like a hard-coded dependency.

### Good Use of Abstract Classes and Design Patterns
The codebase uses a small number of practical abstractions where they reduce coupling or prepare the system for realistic change:

- **Template-style controller base:** `ApiControllerBase` extends ASP.NET `ControllerBase` and centralizes shared authenticated API behavior, especially current-user access. This keeps repeated authentication/user lookup logic out of individual controllers.
- **Dependency Injection:** ASP.NET Core injects services through constructors, so controllers depend on contracts and framework-managed dependencies instead of creating concrete objects directly.
- **Interface-based service replacement:** `IAuditService`, `ITokenService`, `ITwoFactorService`, and `IGisSnapshotService` hide implementation details behind stable contracts. This is important because audit logging, JWT creation, OTP delivery, and GIS access are exactly the areas most likely to change after handoff.
- **Strategy-like integration boundary:** GIS snapshot generation is isolated behind `IGisSnapshotService`. The current implementation uses Haifa ArcGIS, but a future municipality GIS provider can be swapped by replacing the service implementation instead of rewriting controllers or export logic.
- **Adapter boundary for mocked OTP:** OTP is currently mocked in `TwoFactorService`, but the login flow calls `ITwoFactorService`. A real SMS/email provider can be adapted later without changing the controller flow.
- **Centralized validation:** `BuildingRules` and `StreetRules` are shared by API actions and Excel import logic, so mandatory fields and business rules stay consistent across manual editing and bulk import.
- **Persistence boundary:** `AppDbContext` is the single EF Core gateway to PostgreSQL. Controllers and services work through this boundary instead of spreading database access details across unrelated code.
- **High cohesion:** each main area has one clear responsibility: controllers handle HTTP requests and authorization, services handle application behavior, rules handle validation, import helpers handle Excel parsing, `AppDbContext` handles persistence, and domain models represent business data.
- **Low coupling:** controllers depend on service interfaces and stable boundaries instead of concrete external systems. For example, the building workflow calls `IGisSnapshotService` instead of directly depending on ArcGIS details, and the login flow calls `ITwoFactorService` instead of depending on a specific OTP provider.

These choices keep the design simple, but still extensible. The project does not add abstract classes or interfaces for every small helper, only for places where replacement, reuse, or shared behavior is actually needed.

### Robustness to Next Pivot / API Vendor Change
The system is designed so future client or vendor changes can be handled by replacing focused parts of the code instead of rewriting the whole application.

- **GIS provider changes:** the delivered system uses Haifa Municipality ArcGIS because that is the client-approved integration. GIS snapshot creation is isolated behind `IGisSnapshotService`, and the current implementation is `ArcGisSnapshotService`. If the municipality later changes GIS endpoints or moves to another map provider, the replacement work should stay mainly inside the GIS service implementation.
- **Proof-of-pivot branch:** the team also built a separate proof branch that uses OpenStreetMap instead of the client GIS provider to demonstrate scalability and vendor flexibility. It is intentionally kept outside the delivered production branches because the client wants the municipality GIS in the delivered product, but it proves that the map provider can pivot without changing the whole system. The isolated proof lives under `feature/map-provider-flexibility/main` and `feature/map-provider-flexibility/#92-openstreetmap-provider-proof`, and it is not merged into `develop`, `release/*`, or `main`.
- **Municipality scalability:** GIS-based city mapping is a common pattern for municipalities, and the system is not tied directly to Haifa-specific UI logic. The current GIS implementation uses Haifa Municipality ArcGIS, but the integration is isolated behind `IGisSnapshotService`, so another municipality can replace the GIS endpoint or provider with limited code changes. The rest of the system, buildings, streets, users, audit logs, imports, exports, and building cards, is generic enough for municipality asset tracking and would mainly require data-field, validation, and configuration adjustments rather than a full rewrite.
- **OpenStreetMap scalability option:** the proof-of-pivot branch uses OpenStreetMap, which is a worldwide map provider and can support municipalities that do not expose an ArcGIS service. This shows that the system can support both a municipality-owned GIS provider and a global public map provider, depending on what the deployment environment offers.
- **OTP provider changes:** OTP is currently mocked for handoff, but the login flow depends on `ITwoFactorService`, not on a concrete SMS/email provider. A real provider can be added later as a new implementation while keeping the controller flow stable.
- **Additional municipality integrations:** future systems can be added behind new service contracts, following the same pattern as `IGisSnapshotService`, instead of placing external API logic directly inside controllers.
- **Business-rule changes:** mandatory fields, field-level validation, and import validation are centralized in `BuildingRules` and `StreetRules`. If the client changes a rule, the team changes it in one place and both manual editing and Excel import use the same logic.
- **Database/schema changes:** persistence is concentrated through `AppDbContext` and EF Core migrations, so schema changes are handled through the data boundary rather than scattered SQL in UI or controller code.
- **Frontend/backend changes:** the React frontend communicates with the backend through REST API calls. This keeps UI changes separate from backend internals and allows the municipality team to evolve screens without changing the persistence model directly.

This is the main “future proof” point: the project does not assume the current vendor, OTP mechanism, or exact client workflow will stay forever. The architecture keeps likely change points behind interfaces, validation modules, and the EF Core persistence boundary.

### Reset & Rebuild
For a full reset, use the clean deployment command above. It deletes Docker volumes, so database data is removed.

On first run, prepare the local environment file:
```bash
cd project
cp .env.example .env
# Fill .env with the real secrets if this is the first run on this machine.
```

Rebuild without wiping data:
```bash
docker compose down && docker compose up -d --build
```

## Project Structure
- `project/web-server/backend` – ASP.NET Core backend
- `project/web-server/frontend` – React frontend
- `docs/` – project documentation, organized by purpose
- `tests/` – automated tests
- `.github/` – GitHub workflows and templates

Documentation note:
- `docs/technical/` contains current technical/project documentation.
- `docs/data-and-templates/` contains Excel/PPTX templates and sample/reference data.
- `docs/hld/` contains the original HLD material.
- `docs/notes/` contains meeting/client notes.
- `docs/submissions/stage-a/` contains Stage A submission artifacts, rehearsal notes, and UML files.
- `docs/submissions/stage-b/` contains Stage B submission and deployment-grading artifacts.

## Conventions & Workflow
See `docs/CONVENTIONS.md` for:
- Branch naming (`feature/<feature>/#<issue-number>-<slug>`)
- Issue → branch mapping
- Required metadata (milestone, labels, parent User Story)
- Status definitions (Backlog / Current Sprint / Doing / Candidate / Done)
- Approval rules and time tracking

Branch structure used for handoff and grading:
- `main` is reserved for final production/handover history.
- `develop` is the delivered integration branch and should be the GitHub default branch while `main` is intentionally empty.
- `feature/<feature>/main` groups related implementation work.
- `feature/<feature>/#<issue-number>-<slug>` is the branch linked to one implementation issue.
- `release/stage-a/sprint-1-mvp`, `release/stage-a/sprint-2-final`, `release/stage-b/sprint-1-deployment`, `release/stage-b/sprint-2-gis`, and `release/stage-b/sprint-3-final` are release checkpoints from `develop`.
- The normal implementation flow is `develop` -> `feature/<feature>/main` -> `feature/<feature>/#<issue>-<slug>` -> `feature/<feature>/main` -> `develop`.
- The OpenStreetMap provider proof stays isolated under `feature/map-provider-flexibility/*` because it demonstrates scalability without changing the client-approved GIS delivery.

## CI/CD
GitHub Actions is used for both project-process validation and code validation.

### 3.2.3 Automated Testing
The automated testing workflow is defined in `.github/workflows/ci.yml`.

It runs on pushes to `develop`, `feature/**`, and `release/**`, on pull requests into `develop`, and manually through `workflow_dispatch`.

The CI workflow checks:
- **Backend build and tests:** restores, builds, and runs `tests/WebServer.Tests` with .NET 8 and xUnit.
- **Frontend build:** installs dependencies with `npm ci` and runs the Vite production build.
- **Docker Compose build readiness:** validates the Compose configuration with CI-only dummy secrets, then builds the backend and frontend Docker images.

This gives a clear commit-to-delivery gate: implementation branches must be buildable and testable before work is merged through the Git workflow into `develop` and then captured by release checkpoint branches.

The issue-guard workflow is defined in `.github/workflows/issue-guard.yml`. It runs on issue events, daily, and manually to comment when GitHub issue metadata is missing or inconsistent. Issue guard checks project management quality, while CI checks code and deployment readiness.

## Local TLS Certificates
The frontend and pgAdmin use local HTTPS. Self‑signed certs live in `project/certs/` (ignored by git).  
If missing, get them from the team shared drive / secure handoff mail, or generate new local certificates and place them at `project/certs/dev.crt` and `project/certs/dev.key`.

---
Maintained by the GhostHouses team.
