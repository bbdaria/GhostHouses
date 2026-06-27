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
- **Testing:** xUnit (WebServer.Tests) for backend unit tests (run locally).
- **Containerization:** Docker + Docker Compose
- **CI/CD:** GitHub Actions (issue guard comment-only workflow)

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

### Deployment UML
The Stage B deployment UML is located here:
- Source: `docs/submissions/stage-b/uml/deployment/deployment_environment.puml`
- Rendered image: `docs/submissions/stage-b/uml/deployment/deployment_environment.png`

The diagram shows the complete Docker-based runtime environment:
- Municipality users access only the frontend over HTTPS on port 443.
- The frontend container serves the React application through Nginx.
- The backend container runs the ASP.NET Core API internally on port 8080 and is not exposed directly outside Docker.
- PostgreSQL runs internally on port 5432 and is not exposed directly outside Docker.
- pgAdmin is exposed separately over HTTPS on port 8443 for IT/database administration only.
- Docker networks isolate traffic by purpose:
  - `app-net`: frontend to backend.
  - `db-net`: backend to PostgreSQL.
  - `admin-net`: pgAdmin to PostgreSQL.

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
- Branch naming (`Issues/#<issue-number>-<slug>`)
- Issue → branch mapping
- Required metadata (milestone, labels, parent User Story)
- Status definitions (Backlog / Current Sprint / Doing / Candidate / Done)
- Approval rules and time tracking

## CI/CD
GitHub Actions runs the issue‑guard workflow on issue events and daily to comment on missing required metadata. Build/test pipelines are run locally by the team.

## Local TLS Certificates
The frontend and pgAdmin use local HTTPS. Self‑signed certs live in `project/certs/` (ignored by git).  
If missing, get them from the team shared drive / secure handoff mail, or generate new local certificates and place them at `project/certs/dev.crt` and `project/certs/dev.key`.

---
Maintained by the GhostHouses team.
