# GhostHouses
Yearly Project (2340311) @ Technion (2026)

## Project Overview
GhostHouses is a web-based municipal system for tracking vacant/rehabilitation buildings, built as part of the yearly Software Engineering project. It provides a structured workflow for managing building data, auditing changes, importing/exporting datasets, and producing presentation-ready building cards.

## Key Features
- **Buildings**
  - Rich building card (view/edit) with categorized fields
  - Advanced filters (including last update date range)
  - Table sorting by header click
  - Export to Excel (headers only if no selection)
  - Export building cards to PPTX (single or multiple slides)
  - Image support per building (0–1 image)
- **Activity Logs**
  - Full audit trail with per‑field changes
  - Filtering and date range search
  - User column + improved formatting
- **Import / Export**
  - Buildings import from Excel or ZIP (Excel + images)
  - Staged validation: missing fields → conflicts handling
  - Duplicate handling (ID + address rules)
  - Streets import/export with conflict resolution
- **Streets**
  - Dedicated streets page
  - Import/export and conflict resolution
  - “No Street Name” support for buildings (StreetId = -1 reserved)
- **Users & Permissions**
  - User management with roles
  - OTP / password reset
  - Popup‑based add/edit/view
- **Template Converter (Admin)**
  - Convert legacy client templates into current system templates

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
- `docs/` – project documentation and submissions
- `tests/` – automated tests
- `.github/` – GitHub workflows and templates

## Building Card Export (PPTX)
- Uses a PowerPoint template stored in the backend.
- Each selected building is exported as a slide.
- Images preserve aspect ratio and are letterboxed to fit the template image box.

## Import / Export Notes
- **Buildings Export:** Excel, or ZIP (Excel + images) if images are included.
- **Buildings Import:** validates mandatory fields first, then resolves conflicts (skip/replace/add‑anyway).
- **Streets Import:** resolves conflicts by StreetId, supports skip/replace.
- **No Street Name:** buildings can use a reserved StreetId `-1` (not part of streets import).

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
