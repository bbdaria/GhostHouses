# GhostHouses
Yearly Project (2340311) @ Technion (2026)

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

### Admin Template Converter
- Converter page to migrate legacy client templates into the current system format.

## Open / Planned Items (from open issues)
- Real OTP implementation and stronger 2FA enforcement.
- External municipality system sync (data integration).
- Deployment on client Windows Server environment.
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
docker compose up -d --build
```

### Ports & Networking (local Docker)
- **Frontend (Nginx)**: `https://localhost:443` (host port 443 -> container 443).
- **Backend (ASP.NET Core)**: internal only, `http://backend:8080` (no host port mapping).
- **Database (PostgreSQL)**: internal only, `db:5432`.
- **pgAdmin**: `https://localhost:8443` (host port 8443 -> container 443).
- Networks: `app-net` (frontend ↔ backend), `db-net` (backend ↔ db), `admin-net` (pgAdmin ↔ db).

### Reset & Rebuild
Main command (run from `project/`):
```bash
cd project
docker compose down -v && docker compose up -d --build
```

Rebuild without wiping data:
```bash
docker compose down && docker compose up -d --build
```

## Offline / No‑Internet Setup
If you need to run without internet access, use the pre‑pulled image bundle in:
`project/offline-images/ghosthouses-images.tar`

Commands:
```bash
cd project
docker load -i offline-images/ghosthouses-images.tar
docker compose down -v
docker compose up -d --build --pull=never
```
If the images were already built once, you can just run:
```bash
cd project
docker compose up -d
```
See `project/offline-images/OFFLINE_IMAGES_README.docx` for details.

## Project Structure
- `project/web-server/backend` – ASP.NET Core backend
- `project/web-server/frontend` – React frontend
- `docs/` – project documentation and submission files
- `tests/` – automated tests
- `.github/` – GitHub workflows and templates

Documentation note: Stage A submission artifacts (docx + UML) live under `docs/Stage A submission/`.
Internal technical notes live under `docs/internal/`.

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
If missing, generate them locally (see `project/certs/README_Certs.docx` for the exact command).

---
Maintained by the GhostHouses team.
