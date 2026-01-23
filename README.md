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
- Deletion logs supported and protected (no delete button).

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
- OTP reset and password reset actions.

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
- **Testing:** xUnit (WebServer.Tests)
- **Containerization:** Docker + Docker Compose
- **CI/CD:** GitHub Actions

## Installation & Running
```bash
git clone https://github.com/bbdaria/GhostHouses.git
cd GhostHouses/project
docker compose up -d --build
```

### Quick rebuild/run
Reset data and rebuild:
```bash
cd project
docker compose down -v && docker compose up -d --build
```

Rebuild without wiping data:
```bash
docker compose down && docker compose up -d --build
```

## Project Structure
- `project/web-server/backend` – ASP.NET Core backend
- `project/web-server/frontend` – React frontend
- `docs/` – project documentation and submission files
- `tests/` – automated tests
- `.github/` – GitHub workflows and templates

Documentation note: Stage A submission artifacts (docx + UML) live under `docs/Stage A submission/`.

## Conventions & Workflow
See `docs/CONVENTIONS.md` for:
- Branch naming (`Issues/#<issue-number>-<slug>`)
- Issue → branch mapping
- Required metadata (milestone, labels, parent User Story)
- Status definitions (Backlog / Current Sprint / Doing / Candidate / Done)
- Approval rules and time tracking

## CI/CD
GitHub Actions builds the project, runs tests, and validates PRs.

---
Maintained by the GhostHouses team.
