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
GhostHouses is a municipal web system for tracking vacant and rehabilitation buildings for Haifa Municipality. It supports structured building data, import/export workflows, audit logs, GIS map integration, and presentation-ready building cards. The system is built with a .NET backend, React frontend, PostgreSQL database, and Docker Compose deployment.

## Current Capabilities
### Buildings
- Create, edit, delete, and view building details with categorized fields.
- Mandatory field validation and conditional business rules.
- Advanced filters, date filters, and sortable table columns.
- Building actions for Excel export, building-card export, GIS map opening, and logs.
- Supports one image per building for details and building-card export.

### Activity Logs
- Immutable audit trail for building and user actions.
- Per-field change tracking with old/new values.
- Filters by date, user, and field data.

### Import / Export
- Excel import with staged validation and conflict resolution.
- Excel export based on the current UI field order and selected rows.
- Building-card PPTX export for one or multiple buildings.
- GIS snapshots are inserted into building cards when the building can be located.

### GIS
- GIS map page using Haifa Municipality map services.
- Buildings can be opened directly on the GIS map from the Buildings page.
- All database buildings with GIS location data are shown on the map.
- Users can select an area on the map and view/export matching system buildings.
- GIS is an active integration, not a mock.

### Users & Permissions
- User management for Viewer, Editor, and Admin roles.
- Login flow with mocked OTP boundary ready for a real provider after handoff.

## Installation & Running
```bash
git clone https://github.com/bbdaria/GhostHouses.git
cd GhostHouses/project
cp .env.example .env
# Fill .env with the real secrets from the team shared drive / secure handoff mail.
docker compose up -d --build
```

For a full clean reset:
```bash
cd project
docker compose down -v && docker compose up -d --build
```

Before running, make sure these local files exist:
- `project/.env` with the real database, pgAdmin, and JWT secrets.
- `project/certs/dev.crt` and `project/certs/dev.key` for HTTPS.

Do not commit `project/.env` or anything under `project/certs/`. They are ignored by git and should be shared only through the team shared drive or secure handoff mail. See `project/HANDOFF_SECRETS.md` for exact file locations.

On a clean database, the backend creates one initial administrator account: `admin / admin`. Give this account only to the department owner during handoff. They can create the real municipality users and then change or disable the initial admin account.

## Project Delivery

### 1. Presentation and Poster
#### 1.1 Poster
Poster files are stored under `docs/submissions/stage-b/poster/`.

- Required template: `docs/submissions/stage-b/poster/Yearly Poster Pattern 97x67.pptx`
- Project poster working copy: `docs/submissions/stage-b/poster/GhostHouses_Poster.pptx`

#### 1.2 Presentation
Presentation source files are stored under `docs/submissions/stage-b/presentation/`.

- Beamer source: `docs/submissions/stage-b/presentation/StageB_Presentation.tex`
- Rendered PDF: `docs/submissions/stage-b/presentation/StageB_Presentation.pdf`

##### 1.2.1 User Stories to Use Cases to Issues to Sprints
The diagram shows how our main actors connect to permissions, use cases, GitHub User Story issues, and the sprint branches where the work was delivered. It is written in UML 2.5.1 style and rendered with PlantUML.

- Source: `docs/submissions/stage-b/uml/use-cases/use_cases_to_sprints.puml`
- Rendered image: `docs/submissions/stage-b/uml/use-cases/use_cases_to_sprints.png`

The diagram is intentionally presentation-level: it keeps the main flow readable as actors -> permissions -> use cases -> User Story issues -> sprint branches. Detailed implementation sub-issues, time-tracked comments, and branch history remain traceable in GitHub.

##### 1.2.2 Architecture - What Is Abstract, Where We Committed to Concrete, and Why
We use the class UML to explain the main architecture decisions: what we kept abstract, what we implemented concretely, and why. It is written in UML 2.5.1 style and rendered with PlantUML.

- Source: `docs/submissions/stage-b/uml/class/class_diagram.puml`
- Rendered image: `docs/submissions/stage-b/uml/class/class_diagram.png`

What is abstract:
- Controller reuse is abstracted through `ApiControllerBase`, which centralizes shared authenticated API behavior.
- Replaceable service contracts isolate important extension points: `ITokenService`, `ITwoFactorService`, `IAuditService`, and `IGisSnapshotService`.
- GIS snapshot generation is behind `IGisSnapshotService`, so a different GIS provider can replace ArcGIS without changing controllers or building-card export flow.
- OTP is behind `ITwoFactorService`, so the mocked OTP delivery can be replaced by a real SMS/email provider after handoff.
- Building and street validation are centralized in `BuildingRules` and `StreetRules`, so manual editing and Excel import follow the same business rules.

Where we committed to concrete technology:
- PostgreSQL is the concrete database, accessed through EF Core and `AppDbContext`.
- ASP.NET Core is the backend API framework.
- React/Vite is the frontend implementation.
- ArcGIS is the active GIS provider for the municipality map and building-card snapshots.
- Docker Compose is the deployment unit for frontend, backend, PostgreSQL, and pgAdmin.

Why this split fits the project:
- Stable project decisions, such as PostgreSQL, ASP.NET Core, React, and Docker Compose, are concrete because they define the runtime system.
- Riskier or more likely-to-change boundaries, such as OTP delivery, GIS provider, audit behavior, and token generation, are kept behind interfaces so they can be replaced without rewriting the full application.

##### 1.2.3 Customer-Side Deployment Woes and How We Overcame / Did Our Best
We use the deployment UML to explain how GhostHouses is intended to run inside the municipality environment, what blocked the deployment, and what we prepared to move it forward. It is written in UML 2.5.1 style and rendered with PlantUML.

- Source: `docs/submissions/stage-b/uml/deployment/deployment_environment.puml`
- Rendered image: `docs/submissions/stage-b/uml/deployment/deployment_environment.png`
- Deployment User Story: [Issue #94](https://github.com/bbdaria/GhostHouses/issues/94)

What the deployment UML shows:
- Municipality users access the system only from inside the municipality private network over HTTPS 443.
- pgAdmin is separated for IT/database administration over HTTPS 8443.
- The backend and PostgreSQL are internal Docker services with no direct external port mapping.
- Docker networks isolate frontend/backend traffic, backend/database traffic, and pgAdmin/database traffic.
- The backend has outbound HTTPS access to the Haifa Municipality GIS / ArcGIS API.
- OTP remains mocked inside the backend for handoff, so there is no external OTP provider dependency.

Main deployment blocker:
- The target environment is a municipality Windows Server VM.
- Running the delivered Docker Compose stack on that VM depends on Docker/WSL support, which may require nested virtualization approval from municipality security/IT.

How we handled it:
- Prepared a one-command Docker Compose deployment from `project/`.
- Documented required ports, software dependencies, hardware expectations, secrets, and certificates.
- Prepared alternatives with the supervisor in case nested virtualization is not approved.
- Continued written communication with the client side, dev team, IT/security contacts, and supervisors to move deployment forward professionally.

### 2. Deployment
#### 2.1 Deployment Simplicity
Deployment is handled through one top-level Docker Compose file: `project/docker-compose.yml`.

From the `project/` folder, one command builds and starts the full system:
```bash
docker compose down -v && docker compose up -d --build
```

This deployment path is intentionally simple and repeatable:
- One command runs the full stack: frontend, backend, PostgreSQL, and pgAdmin.
- Docker Compose builds the project services and pulls required public images when needed.
- PostgreSQL starts with a health check before the backend is used.
- Docker networks are created automatically with the intended isolation: `app-net`, `db-net`, and `admin-net`.
- `down -v` resets the database and pgAdmin volumes, which gives a clean deployment state when needed.
- Required local files are documented: `project/.env`, `project/certs/dev.crt`, and `project/certs/dev.key`.

Ports and Docker networking:
- **Frontend (Nginx):** `https://localhost:443`, host port 443 to container 443.
- **Backend (ASP.NET Core):** internal only, `http://backend:8080`, no host port mapping.
- **Database (PostgreSQL):** internal only, `db:5432`, no host port mapping.
- **pgAdmin:** `https://localhost:8443`, host port 8443 to container 443.
- **Networks:** `app-net` for frontend/backend, `db-net` for backend/database, and `admin-net` for pgAdmin/database.

#### 2.2 Minimal Requirements and Dependencies Analysis
This section documents the minimum environment needed to deploy GhostHouses and links each requirement to the runtime design.

##### 2.2.1 Deployment UML
The Stage B deployment UML is written in UML 2.5.1 style and rendered with PlantUML.

- Source: `docs/submissions/stage-b/uml/deployment/deployment_environment.puml`
- Rendered image: `docs/submissions/stage-b/uml/deployment/deployment_environment.png`

The diagram shows:
- Municipality users access the system only from inside the municipality private network through HTTPS on port 443.
- The frontend container serves the React application through Nginx.
- The backend container runs the ASP.NET Core API internally on port 8080 and is not exposed directly outside Docker.
- PostgreSQL runs internally on port 5432 and is not exposed directly outside Docker.
- pgAdmin is exposed separately over HTTPS on port 8443 for IT/database administration.
- Docker networks isolate traffic by purpose: `app-net`, `db-net`, and `admin-net`.
- The backend has outbound HTTPS access to the public Haifa Municipality GIS / ArcGIS API.
- OTP is mocked inside the backend for this delivery, so there is no external OTP provider dependency.

##### 2.2.2 Software Dependencies
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

##### 2.2.3 Minimal Hardware Requirements and How We Figured Them
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

#### 2.3 Customer Satisfaction
##### 2.3.1 Customer Questionnaire
A Hebrew customer questionnaire was used to collect the client's final feedback, and the signed response is stored as the customer satisfaction evidence.

Filled response evidence: `docs/submissions/stage-b/customer-satisfaction/GhostHouses_Customer_Questionnaire_Response.pdf`

##### 2.3.2 Successful Deployment on Customer Side
Customer-side deployment is tracked in the deployment User Story: [Issue #94](https://github.com/bbdaria/GhostHouses/issues/94).

##### 2.3.3 Exhausting Communication With the Customer Toward Possible Deployment, in Writing
The written communication trail for deployment is documented in email correspondence with the client side, the municipality dev team, municipality IT/security contacts, and the project supervisors CC'd. This evidence covers the deployment requirements, server preparation, security approval process, nested-virtualization discussion, and our attempts to keep the deployment moving professionally.

#### 2.4 Maintainable Architecture
##### 2.4.1 UML Class Diagram
The Stage B class UML is written in UML 2.5.1 style and rendered with PlantUML.

- Source: `docs/submissions/stage-b/uml/class/class_diagram.puml`
- Rendered image: `docs/submissions/stage-b/uml/class/class_diagram.png`

The diagram shows:
- API layer: controllers, common controller bases, and the separation between authenticated API controllers and simpler controllers.
- Service layer: replaceable contracts for audit, JWT, OTP, and GIS behavior.
- Validation/import layer: shared building and street rules reused by manual editing and Excel import.
- Persistence layer: `AppDbContext` as the EF Core boundary for PostgreSQL entities.
- Domain layer: the main business entities and relationships used by the system.

##### 2.4.2 Good Use of Abstract Classes and Design Patterns
The codebase uses practical abstractions only where they reduce coupling or prepare the system for realistic change:

- **Template-style controller base:** `ApiControllerBase` extends ASP.NET `ControllerBase` and centralizes shared authenticated API behavior.
- **Dependency Injection:** ASP.NET Core injects services through constructors, so controllers depend on contracts and framework-managed dependencies.
- **Interface-based service replacement:** `IAuditService`, `ITokenService`, `ITwoFactorService`, and `IGisSnapshotService` hide implementation details behind stable contracts.
- **Strategy-like integration boundary:** GIS snapshot generation is isolated behind `IGisSnapshotService`, currently implemented by `ArcGisSnapshotService`.
- **Adapter boundary for mocked OTP:** login depends on `ITwoFactorService`, so a real SMS/email provider can be added later without rewriting controller flow.
- **Centralized validation:** `BuildingRules` and `StreetRules` are shared by API actions and Excel import logic.
- **Persistence boundary:** `AppDbContext` is the single EF Core gateway to PostgreSQL.
- **High cohesion:** controllers handle HTTP, services handle application behavior, rules handle validation, import helpers handle Excel parsing, and models represent business data.
- **Low coupling:** controllers depend on service interfaces and stable boundaries instead of concrete external systems.

##### 2.4.3 Demonstrate Robustness to Next Pivot / API Vendor Change, Future Proof
The system is designed to be future proof: likely client, vendor, and deployment changes can be handled by replacing focused parts of the code instead of rewriting the whole application.

- **GIS provider changes:** the delivered system uses Haifa Municipality ArcGIS because that is the client-approved integration. GIS map and snapshot behavior is isolated behind focused GIS modules and `IGisSnapshotService`, with the current backend implementation in `ArcGisSnapshotService`. If the municipality changes GIS endpoints or moves to another provider, most of the replacement work should stay inside the GIS boundary instead of spreading through controllers, exports, or the database model.
- **OpenStreetMap proof of pivot:** a separate proof branch demonstrates that the map provider can be replaced with OpenStreetMap: `feature/map-provider-flexibility/main` and `feature/map-provider-flexibility/#92-openstreetmap-provider-proof`. It is intentionally not merged into `develop`, `release/*`, or `main` because the client-approved delivery uses Haifa Municipality GIS, but it proves vendor flexibility.
- **Municipality scalability:** GIS-based city mapping is common for municipalities. The system is not hard-coded to Haifa-only UI logic, so another municipality could replace the GIS endpoint/provider and adjust field definitions without a full rewrite.
- **OTP provider changes:** OTP is mocked for this handoff, but the login flow depends on `ITwoFactorService`. A real SMS/email provider can be added later by replacing that service implementation.
- **Additional external systems:** future municipality integrations can follow the same service-contract pattern instead of placing external API logic directly inside controllers.
- **Business-rule changes:** mandatory fields and validation logic are centralized in `BuildingRules` and `StreetRules`, so manual editing and Excel import stay consistent when rules change.
- **Database/schema changes:** persistence is concentrated through `AppDbContext` and EF Core migrations, so schema evolution happens at the data boundary instead of through scattered SQL.
- **Frontend/backend separation:** the React frontend communicates with the backend through REST APIs, so UI changes and backend persistence changes can evolve separately.

### 3. Project Management
#### 3.1 Agility
##### 3.1.1 MVP Hop - Continuous Version Releases
Release checkpoints show continuous project growth:
- `release/stage-a/sprint-1-mvp`: first working MVP / client-demoable version.
- `release/stage-a/sprint-2-final`: final Stage A checkpoint.
- `release/stage-b/sprint-1-deployment`: deployment-focused Stage B checkpoint.
- `release/stage-b/sprint-2-gis`: GIS-focused Stage B checkpoint.
- `release/stage-b/sprint-3-final`: intentionally empty for now and will be updated when all remaining issues are closed and the final handoff state is ready.

##### 3.1.2 Backlog Management - What Is on the Horizon
Open/planned work is tracked through GitHub Issues and the GhostHouses project board.

Current known horizon:
- Real OTP implementation and stronger 2FA enforcement.
- Additional external municipality system integrations beyond GIS, if the municipality provides real API/file contracts.
- Final deployment/handoff completion on the customer server.
- Final customer feedback/questionnaire evidence.

#### 3.2 Correct Use of GitHub
##### 3.2.1 Branch Management
The branch workflow is documented in `docs/CONVENTIONS.md`.

Current branch model:
- `main` is reserved for final production/handover history.
- `develop` is the delivered integration branch.
- `feature/<feature>/main` groups related implementation work.
- `feature/<feature>/#<issue-number>-<slug>` is linked to exactly one implementation issue.
- `release/stage-a/...` and `release/stage-b/...` branches are release checkpoints from `develop`.
- Normal implementation flow: `develop` -> `feature/<feature>/main` -> `feature/<feature>/#<issue>-<slug>` -> `feature/<feature>/main` -> `develop`.

##### 3.2.2 Issue Management
Issue conventions are documented in `docs/CONVENTIONS.md`.

The repository uses:
- User Story issues with acceptance criteria.
- Non-User-Story implementation issues with `Summary`, `Scope`, and `Notes`.
- Required labels, milestones, project status, parent User Story links, and Development branch links.
- Closing/progress comments with `Time spent:`.
- `issue-guard.yml` to comment when issue metadata is missing or inconsistent.

##### 3.2.3 Automated Tests
The automated testing workflow is defined in `.github/workflows/ci.yml`.

It runs on pushes to `develop`, `feature/**`, and `release/**`, on pull requests into `develop`, and manually through `workflow_dispatch`.

The CI workflow checks:
- **Backend build and tests:** restores, builds, and runs `tests/WebServer.Tests` with .NET 8 and xUnit.
- **Frontend build:** installs dependencies with `npm ci` and runs the Vite production build.
- **Docker Compose build readiness:** validates the Compose configuration with CI-only dummy secrets, then builds the backend and frontend Docker images.

The issue-guard workflow is defined in `.github/workflows/issue-guard.yml`. It checks GitHub/project-management metadata, while CI checks code and deployment readiness.

##### 3.2.4 Readme.md
This README is structured as both a handoff guide and a grading evidence map. It includes:
- Project overview and team information.
- Current capabilities.
- Installation and clean deployment commands.
- Deployment requirements, UML links, dependencies, and hardware sizing.
- Architecture maintainability evidence.
- GitHub workflow evidence.
- CI/test evidence.
- License evidence.

##### 3.2.5 License
This repository is licensed under the Creative Commons Attribution 4.0 International license (`CC-BY-4.0`).

Full license text: `LICENSE.md`.

## Project Structure
- `project/web-server/backend`: ASP.NET Core backend.
- `project/web-server/frontend`: React frontend.
- `project/docker-compose.yml`: top-level Docker Compose deployment.
- `project/db-server`: PostgreSQL and pgAdmin compose configuration.
- `docs/`: project documentation.
- `docs/submissions/stage-a/`: Stage A submission artifacts.
- `docs/submissions/stage-b/`: Stage B grading evidence, UML files, and future customer evidence.
- `tests/`: automated backend tests.
- `.github/workflows/`: GitHub Actions workflows.

---
Maintained by the GhostHouses team.
