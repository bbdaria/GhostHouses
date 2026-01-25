# Stage A Rehearsal Notes (Long-Form)

Purpose: long answers for rehearsal + a self-contained onboarding guide for someone new to GhostHouses.
Use this as a spoken script or as a teaching document.

---

## 0) Executive Summary (2–3 minutes)

GhostHouses is a municipal web system for tracking vacant and rehabilitation buildings. We built a .NET 8 backend, a React frontend, and a PostgreSQL database, all containerized with Docker. The system now supports structured building data, robust import/export workflows, immutable activity logs, and presentation-ready building cards (PPTX). We also added a template converter so legacy client Excel data can be converted into our schema. The architecture is organized around controllers, services, and data/models with a shared field metadata layer (FieldSpec) so Add/Edit/Import/Export logic stays consistent.

Operationally we hardened the deployment layout: the frontend is served over HTTPS (Nginx on port 443), the backend is internal-only (port 8080), the database is internal (5432), and pgAdmin is exposed separately over HTTPS (8443). These are isolated by Docker networks (`app-net`, `db-net`, `admin-net`) so only the intended services can talk to each other. Local TLS certs live under `project/certs/` and are ignored by git.

Stage A focused on completing the core workflows (buildings, streets, logs, import/export, cards) and hardening governance and documentation: issue conventions, automated issue checks (issue guard runs on events and daily), and updated README + submission docs. Remaining backlog is mostly external integration and deployment on the client’s Windows Server.

Current status (explicit for the rubric’s “Current …” item):
- Stage A is complete and stable, running on a temporary HTTPS host.
- Remaining Stage B work depends on client IT access and external integrations.

Plain-language summary:
- We built a working system with clean data rules, reliable logs, and import/export tools.
- We tightened operational setup (HTTPS frontend, internal backend/db, separate admin access).
- The only big missing pieces are deployment on the client server and external integrations.

---

## 1) User Stories: HLD changes, what’s implemented, what’s planned

What is a User Story (plain language):
- A short, numbered requirement that describes a real user need (e.g., “Export buildings to Excel”).

### What changed from HLD
- HLD had **16 User Stories**, we now have **17**.
- **Added after HLD:** US‑17 “Export buildings table to Excel for backup/restore” (Issue #76).
- **Removed:** US‑14 “Delete log entries” (client wanted immutable logs, Issue #73 closed).
- Several stories expanded (e.g., import/export now includes staged validation and conflict resolution, building cards export multi-slide decks).

### Current User Story status (as used in the submission doc)
- **US‑1** Clear role model (Viewer/Editor/Admin) — Implemented (Issue #20 closed).
- **US‑2** 2FA login — Backlog (OTP mock exists, real OTP integration pending).
- **US‑3** User permissions control — Backlog (role gates exist, final admin controls pending).
- **US‑4** View building details — Implemented.
- **US‑5** Search & filter buildings — Implemented.
- **US‑6** View building card with photos — Implemented.
- **US‑7** Export building card for presentations — Backlog (core export exists, template update pending).
- **US‑8** Add new buildings — Implemented.
- **US‑9** Update existing buildings — Implemented.
- **US‑10** Remove buildings with safeguards — Implemented.
- **US‑11** View building change log — Implemented.
- **US‑12** Search & filter logs — Implemented.
- **US‑13** Add/edit log entries — Canceled (logs are auto‑generated and immutable).
- **US‑14** Delete log entries — Canceled (client requested immutable logs).
- **US‑15** Full audit trail — Implemented.
- **US‑16** External system sync — Planned (Stage B).
- **US‑17** Export buildings table to Excel for backup/restore — Implemented (added after HLD).

What to say out loud:
- “We aligned with the client’s actual usage: immutable logs, export/import workflows, and a stronger audit trail. The only removed story is log deletion, we added Excel export to match real operational needs.”

Plain-language summary:
- User Stories are our official checklist, Stage A core workflows are done, and the remaining open items belong to Stage B (auth integration, template update, external systems).

Rubric Q&A (user stories):
- Question: Are user stories well defined and formulated?  
  Answer: Yes, each User Story is a single, focused requirement with a clear title and scope, and every non‑story issue is linked back to a parent User Story.
- Question: Do we have acceptance criteria?  
  Answer: Yes, each User Story now starts with `## Acceptance Criteria` and has checkbox items, open stories keep at least one unchecked item, closed Done stories have all items checked.
- Question: Do we know our actors?  
  Answer: Yes, actors are explicit in the use‑case diagram and permissions, Viewer, Editor, Admin (plus IT/DB Admin for pgAdmin).
- Question: Do we understand inter‑dependencies?  
  Answer: Yes, we track dependencies, OTP depends on the client’s OTP provider, external sync depends on municipal APIs, deployment depends on client IT access, those are moved to Stage B.
- Question: Are there changes from the HLD?  
  Answer: Yes, we documented additions and removals, change is expected and acceptable, we added Excel export, strengthened import, removed log deletion, and added a template converter.
- Question: Do we estimate remaining difficulty?  
  Answer: Yes, remaining items are medium to hard due to external dependencies, OTP integration, external APIs, and client Windows Server deployment.

Rubric Q&A (design and class diagram):
- Question: Is the design feasible, maintainable, and extensible?  
  Answer: Yes, it is layered and modular, Controllers are thin, rules live in one place, import and export are isolated utilities, adding external integrations can be done by adding adapters without rewriting controllers.
- Question: Are there loop dependencies?  
  Answer: No, the flow is one‑way, UI → Controllers → Services or Rules → Data, models are shared, there are no circular dependencies in the main path.
- Question: Are interfaces used where extension makes sense?  
  Answer: We keep clear boundaries and service style classes, external sync and auth are isolated so adding interfaces or adapters later is straightforward without changing business rules.
- Question: Is responsibility encapsulated correctly?  
  Answer: Yes, DTOs isolate the API shape, rules are centralized, persistence is in the data layer, and the UI does not know database details.

Rubric Q&A (GitHub and repo quality):
- Question: Do they have a clear repo and README?  
  Answer: Yes, README explains how to run the stack and where the backend, frontend, and docs live, it includes deployment and local run instructions.
- Question: Do they have a good branching system?  
  Answer: Yes, we use main for stable releases, dev for ongoing work, and per‑issue branches following a fixed naming convention.

Rubric Q&A (testing and deployment workflow):
- Question: Do they have a workflow from commit to deployment?  
  Answer: Yes, we work per issue, review and test manually, then merge to dev, then to main for stable releases, deployment is Docker‑based, and daily issue‑guard checks enforce process rules.  
  Important note: deployment to the client Windows Server is delayed by client IT availability, so we use a temporary host for testing, this is a process constraint, not a design issue.

Rubric Q&A (PoC):
- Question: Did they prove their PoC from the HLD?  
  Answer: Yes, the HLD PoC focused on Excel ingestion, we built a staged importer, validation rules, and a template converter, which goes beyond a demo and proves the concept in production‑style flow.  
  Important note: even if the HLD PoC was limited, we validated the core ideas during Stage A.

Rubric Q&A (risk assessment):
- Question: What is the major risk and how is it mitigated?  
  Answer: The major risk is external dependency, deployment on the client Windows Server and integration with municipal systems, we mitigate with a temporary hosted environment, clear network separation, and staged integration plans.  
  The full risk table is in point 7 and includes past risks and current risks.

Rubric Q&A (current backlog):
- Question: Is the backlog too short or unusual?  
  Answer: No, the open backlog contains Stage B items driven by external dependencies and planned features, it is visible in the GitHub Project board and does not contain filler issues.

---

## 2) Design / Architecture (Class Diagram + Implementation Story)

Plain-language summary:
- The system has clear layers: the UI sends requests, the backend applies rules, and the database stores the data.
- We keep rules and field definitions in one place so all screens behave the same.

### Layer definitions (technical + plain language)
**DTOs (Data Transfer Objects):** Small request/response shapes used by controllers so we don’t expose database entities directly.  
Plain language: we send only the fields we need, not the full database record.

**Controllers:** API entry points that receive HTTP requests, validate inputs, call services, and return DTOs.  
Plain language: the “front desk” that routes each request to the right place.

**Services:** Business logic layer (validation rules, import/export workflows, logging, integrations like OTP/external sync).  
Plain language: the system’s “rules engine.”

**Data layer:** EF Core (Entity Framework Core) `DbContext`, migrations, and import utilities. Uses LINQ (C#’s built‑in query syntax) instead of raw SQL.  
Plain language: the part that saves/loads data and handles structured file imports.

**Models:** Domain entities (Building, Street, User, Log) plus metadata like `FieldSpecAttribute` (field descriptors such as category, label, select‑table source, logging).  
Plain language: definitions of the main objects and how the UI should show them. “Metadata” here means extra descriptions about fields (category, label, select‑table source, logging).

### High-level architecture
**Backend:** ASP.NET Core (.NET 8)  
- Controllers expose HTTP endpoints.  
- Services encapsulate business logic and background jobs.  
- Data layer (EF Core + LINQ) handles persistence and schema.  
- Shared metadata and rules enforce consistent validation and field definitions.  

**Frontend:** React (Vite)  
- Pages: Buildings, Logs, Streets, Users, Settings, Template Converter.  
- Reusable modals for add/edit/view for consistent UX.  
- API client layer in `frontend/src/api/client.js`.  

**Database:** PostgreSQL  
- EF Core migrations define schema.  
- Tables: Buildings, Streets, Users, BuildingLogs, AuditEntries, ExternalSystemSnapshots.  

### UML legend (arrows and colors)
- `A --> B` means **A uses/depends on B** (A calls B).  
- `A <|-- B` means **B inherits from A** (B is a specialized type of A).  
- `A \"1\" --> \"*\" B` means **one A relates to many B** (one‑to‑many).  
- Incoming arrows **into** a box mean others depend on it.  
- Outgoing arrows **from** a box mean it depends on others.  
- Colors: **Green = implemented**, **Yellow = planned**.  

### UML element descriptions (what each box is)
**Controllers**  
- `ApiControllerBase`: shared base class (common behavior for all controllers).  
- `AuthController`: login + OTP flow, uses TokenService, TwoFactorService, DbContext.  
- `BuildingsController`: building CRUD (Create, Read, Update, Delete), logs, import/export, building cards.  
- `LogsController`: log queries and filtering.  
- `StreetsController`: street CRUD (Create, Read, Update, Delete) + import/export.  
- `UsersController`: user CRUD (Create, Read, Update, Delete) + OTP reset.  
- `SelectTablesController`: serves select‑table options for dropdowns.  
- `HealthController`: health endpoint for monitoring.  

**Services**  
- `AuditService`: writes audit entries (who did what, when).  
- `BuildingRules`: building validation rules (required fields, rehab logic, select values).  
- `StreetRules`: street validation rules (ID rules, required name).  
- `TokenService`: issues JWTs (JSON Web Tokens), signed login tokens sent with each request.  
- `TwoFactorService`: issues/validates OTP codes (currently mocked).  
- `ExternalDataService` (mocked, integration planned): fetches external data snapshots.  
- `ExternalSyncWorker` (mocked, integration planned): background job to refresh external data.  

**Data**  
- `AppDbContext`: EF Core (Entity Framework Core) gateway to the database.  
- `SelectTables`: registry of dropdown options.  
- `BuildingsExcelImporter`: parses Excel/ZIP and validates buildings.  
- `StreetsExcelImporter`: parses Excel and validates streets.  

**Models**  
- `Building`: main entity with all building fields + metadata.  
- `Street`: street entity (StreetId key).  
- `BuildingLog`: immutable change log entries (no delete UI, logs are audit‑grade).  
- `AuditEntry`: generic audit events.  
- `ExternalSystemSnapshot` (mocked, integration planned): external sync payloads.  
- `AppUser`: user entity (roles, OTP state).  

### Key backend classes and relationships (talk track)
- **`AppDbContext`** is the EF Core (Entity Framework Core) hub, exposes `DbSet<Building>`, `DbSet<Street>`, `DbSet<AppUser>`, `DbSet<BuildingLog>`, `DbSet<AuditEntry>`, `DbSet<ExternalSystemSnapshot>`.
- **`Building`** is the central entity. It has a FK (foreign key) to `Street` via `StreetCode`, meaning each building references a specific street. It also uses `FieldSpecAttribute` metadata on each property to describe category/label/select table/event log inclusion.
- **`Street`** is keyed by `StreetId`. It has a collection of `Buildings`.
- **`BuildingLog`** stores change events, logs are immutable (no delete).
- **`AuditEntry`** stores generic audit events across entities.
- **`FieldSpecAttribute`** is metadata used by UI + import/export + logs to keep field definitions consistent.

**Services:**
- **`BuildingRules`** is the single source of truth for building validation: required fields, type parsing, rehab status conditional logic, select-table resolution.
- **`StreetRules`** handles street validation and reserved IDs (`-1` reserved for “ללא שם רחוב” but only in buildings context).
- **`AuditService`** records audit entries (who did what, when, with what changes).
- **`TokenService`** issues JWT tokens (JSON Web Tokens) — signed login tokens the client sends on each request so the server can identify the user.
- **`TwoFactorService`** issues/verifies OTP codes (currently logged, external integration pending).
- **`ExternalDataService` + `ExternalSyncWorker`** mock external system snapshots and a scheduled sync (planned integration).

**Controllers:**
- **`BuildingsController`** orchestrates building CRUD (Create, Read, Update, Delete), logs, import/export, building card export.
- **`LogsController`** queries BuildingLog and supports filtering/sorting.
- **`StreetsController`** CRUD (Create, Read, Update, Delete) + import/export for streets.
- **`UsersController`** user CRUD (Create, Read, Update, Delete) and OTP reset.
- **`AuthController`** login/OTP flow.
- **`SelectTablesController`** exposes dropdown/select options.

What to say:
- “We built a consistent rule and metadata layer so Add/Edit/Import/Export all use the same validation rules. That’s why we introduced `BuildingRules` and `StreetRules` and `FieldSpecAttribute`.”

---

## 3) GitHub / Repository Readiness

The repo is structured for handoff and long-term maintenance:
- Clear directory layout (`backend/`, `frontend/`, `docs/`, `.github/`).
- `docs/CONVENTIONS.md` defines branch naming, issue metadata, statuses, and approval rules.
- `issue-guard.yml` enforces metadata rules through automated comments (missing labels, parent story, project status, and branch linkage).
- README includes a real feature list, setup steps, and pointers to documentation.
- Stage A submission artifacts are committed under `docs/Stage A submission/`.
- Issue guard runs on issue events and daily (scheduled) so drift is caught automatically.

What to say:
- “We can hand this repo to another team and they can understand structure, run the system, and follow our issue conventions immediately.”

Plain-language summary:
- The project is organized and documented so new developers can continue work without guessing.

---

## 4) CI/CD and Deployment Strategy

Plain-language summary:
- We have a repeatable build flow (Docker + GitHub), but real deployment is still waiting for the client’s server.

### Current state
- Dockerized development and build process.
- GitHub Actions exists for build/test workflow.
- Temporary client testing environment: hosted on a team member’s PC kept online, with router port‑forwarding (443 → 443) and a **free** No‑IP dynamic DNS address (a service that gives a stable hostname even when the public IP changes). The client currently uses `https://ghosthouses.myddns.me/`.
- Current port layout: Frontend HTTPS 443 (public), backend internal 8080 (not exposed), PostgreSQL 5432 (internal), pgAdmin HTTPS 8443 (admin‑only).
- Network isolation: `app-net` (frontend↔backend), `db-net` (backend↔db), `admin-net` (pgAdmin↔db).
- For a visual network breakdown, see `docs/Stage A submission/uml/network_split.puml`.

### Planned deployment flow
- Dev branch for ongoing work, merge to main for stable releases.
- Build pipeline produces container images.
- Deployment target is **client Windows Server**, which requires coordination with client IT (access, domain, HTTPS, credentials).

What to say:
- “What stayed the same from HLD: GitHub‑based workflow (dev for ongoing work, main for stable releases), Dockerized stack, and the planned CI/CD path. What changed: we can’t deploy to the client’s Windows Server yet, so we run a temporary host (team PC + port‑forwarding + free No‑IP). Port‑forwarding routes outside traffic to our internal host, No‑IP provides a stable DNS name. It’s worse for long‑term reliability, but better for short‑term progress because it keeps client testing active.”

Plain-language summary:
- We can build and run the system reliably, but we still need client IT to host it officially.

---

## 5) POC (from HLD) and how we validated it

Plain-language summary:
- We proved the system can ingest Excel data safely by enforcing the same rules everywhere.

**HLD POC:** Prove that the system can ingest Excel data and map it to the DB reliably.

**What we built (and why it’s stronger than the HLD POC):**
- **Single source of truth** for Buildings and Streets rules across add/edit/import/export.  
  Paths: `project/web-server/backend/Services/BuildingRules.cs`, `project/web-server/backend/Services/StreetRules.cs`,  
  shared metadata: `project/web-server/backend/Models/FieldSpecAttribute.cs`, select options: `project/web-server/backend/Data/SelectTables.cs`.
- **Importers** use the same rules and metadata, so behavior is consistent everywhere.  
  Paths: `project/web-server/backend/Data/BuildingsExcelImporter.cs`, `project/web-server/backend/Data/StreetsExcelImporter.cs`.
- **Immutable logs** (audit‑grade), so history cannot be deleted or altered.
- **New strict template** is now the only accepted format. Any non‑template file is rejected.
- **Temporary converter page** for legacy client Excel files (maps columns only, no validation).  
  Frontend: `project/web-server/frontend/src/pages/TemplateConverterPage.jsx`  
  Backend endpoints: `project/web-server/backend/Controllers/BuildingsController.cs` and `.../StreetsController.cs` (`convert-template`).
- **Validation happens on import** of the converted file, so the client is forced to comply with the new rules.
- We already referenced these classes in point 2 (class diagram), so this POC explanation is consistent with the architecture story.

**Outcome:**
- The import pipeline now validates Hebrew data, enforces required fields and select‑table values, resolves conflicts, and prevents inconsistent data.  
- This is better than the HLD POC because it is not just a demo—it’s the production‑grade rule system.

---

## 6) Current Main Risk (non‑technical or technical)

Plain-language summary:
- The biggest risk is not technical code, it is external dependencies (client servers and external APIs).

**Primary risk:** Deployment + external integrations require the client’s Windows Server and external APIs, which we do not control.

**Mitigation plan:**
- Engage IT early for environment access and HTTPS/domain setup.
- Use mock adapters now (ExternalDataService) and swap with real adapters when API contracts are known.
- Prepare a pilot deployment with a minimal dataset.

---

## 7) Risk Management Table (current state)

Plain-language summary:
- We identified major risks and already lowered them with backups, mocks, and validation.

| Project Risk Severity | Likelihood of Risk | Mitigation | Severity After Mitigation | Likelihood After Mitigation |
| --- | --- | --- | --- | --- |
| High | Medium | Define deployment plan with client IT, test pilot deployment | Medium | Low |
| High | Medium | Define external API contracts, build mocks/adapters | Medium | Low |
| Medium | High | Integrate OTP with client provider, maintain fallback | Medium | Low |
| Medium | High | Staged import validation, conflict resolution | Low | Medium |
| Medium | Medium | Docs, conventions, issue guard, structured repo | Low | Low |

What to say per row:
- Deployment risk: our biggest dependency is the client’s Windows Server + IT access, we mitigate with a pilot plan and temporary hosting, which lowers likelihood.
- External APIs risk: contracts are not finalized, we built mocks/adapters so integration can proceed once APIs are ready (GIS, Water, Electricity, Tax/Arnona, CRM‑106).
- OTP risk: current OTP is mocked, mitigation is provider integration plus a fallback so auth doesn’t block usage.
- Data import risk: real municipal data is messy, staged validation + conflict resolution reduces severity even if likelihood stays medium.
- Process risk: handoff/maintenance risk is reduced by clear documentation and automated checks (README + CONVENTIONS + issue-guard workflow enforce consistent branches, labels, parent stories, and status rules).

---

## 8) Current Backlog (open items)

Plain-language summary:
- All Stage A items are closed, everything left is Stage B because it depends on client IT or external systems.

All Stage A milestone issues and Current Sprint items are closed (Done or Canceled).  
All remaining open items are in **Stage B**.

For a clearer visual view, use the GitHub Project board backlog.

Stage B backlog highlights (open):
- #4 Real OTP integration
- #5 External data sources integration
- #78 Deployment on client Windows Server
- #82 Building card template update (awaiting client)

Open User Stories tied to backlog:
- US‑2 2FA login
- US‑3 Permissions
- US‑7 Building card export (template update)
- US‑16 External system sync
Why Stage B (not Stage A / current sprint): these depend on external factors or inputs that are not available yet (client IT deployment access, external municipal APIs, OTP provider integration, and a new building card template). We cannot complete them without those dependencies, so they were moved to Stage B.

---

## 9) UML: Use Case (Implemented / Planned / Backlog)
Use case diagram is color‑coded as requested:  
- Implemented = Green  
- Planned = Yellow  
- Backlog only = Red  
- Canceled/removed = Gray  
Files: `docs/Stage A submission/uml/use_case.puml` and `docs/Stage A submission/uml/use_case.png`.

What to say:
- “We marked every use case by status so it’s clear what was delivered in Stage A, what is planned for Stage B, and what is only backlog.”
- “We also extended the original HLD diagram with the Stage‑A additions (labeled ‘Added’), without removing any original HLD items.”
- “Mocks are explicitly labeled (e.g., Manage users mock, Reset OTP mock, internal servers mock) to distinguish temporary implementations from final production behavior.”

Plain-language summary:
- This picture shows who can do what in the system, and which features are done vs planned.

---

## 10) UML: Class Diagram
Class diagram matches the implementation (see point 2).  
Files: `docs/Stage A submission/uml/class_diagram.puml` and `docs/Stage A submission/uml/class_diagram.png`.

What to say:
- “This class diagram is implementation‑accurate, not just conceptual HLD. Planned components are marked separately.”

Plain-language summary:
- This diagram is the “blueprint” of the code: the main classes and how they connect.

---

## 11) UML: Deployment (Desired / Target)
Desired deployment shows the intended client Windows Server environment with HTTPS and CI/CD pipeline.  
User → Frontend (HTTPS) → Backend → PostgreSQL DB, plus Auth Server and other municipal services inside the municipality network.  
pgAdmin is for municipality IT/DB admins only (not end users). It’s the database admin console used to monitor the DB, run queries, manage backups, and verify data health. We need it for maintenance and troubleshooting, not for day‑to‑day users.  
This matches the HLD desired deployment diagram (same flow, updated to PostgreSQL in our stack).
Containers: frontend + backend run in Docker containers on the web server, PostgreSQL + pgAdmin run in Docker containers on the database server.
React/Vite: React is the frontend UI library, Vite is the build/dev tool that bundles the frontend quickly.  
ASP.NET Core: Microsoft’s backend web framework for building APIs and handling business logic.
EF Core: .NET’s Object‑Relational Mapper (ORM) that lets us work with the DB using C# classes instead of raw SQL.  
LINQ: a C# query language used to filter/sort/project data in code, EF Core translates LINQ queries into SQL.
File: `docs/Stage A submission/uml/deployment_desired.puml` / `.png`.

Detailed network view and ports:
- See `docs/Stage A submission/uml/network_split.puml` for the concrete Docker network split and ports.
- Networks: app‑net (frontend ↔ backend), db‑net (backend ↔ PostgreSQL), admin‑net (pgAdmin ↔ PostgreSQL).
- Ports: Frontend HTTPS 443 (public), Backend API 8080 (internal), PostgreSQL 5432 (internal), pgAdmin HTTPS 8443 (admin only).
- Why we split networks: reduce reachability, enforce least‑privilege, and keep admin tools isolated from the app path.

HLD comparison:
- HLD already showed the same logical flow (Frontend → Backend → DB + Auth/Services).
- Now we add Docker networks and explicit ports so deployment is precise and security‑aware.

What to say:
- “This is the target deployment once client IT provides access and infrastructure.”

Plain-language summary:
- This shows the final hosting setup we want on the client’s Windows Server.

---

## 12) UML: Deployment (Current / Existing)
Current deployment reflects the temporary hosting on a team PC. The client reaches it over HTTPS on port 443 via No‑IP DNS (`https://ghosthouses.myddns.me`) and router port‑forwarding (external 443 → internal 443 on the frontend).  
HTTPS is the encrypted version of HTTP, we already use it in the temporary host, and it will remain in the official Windows Server deployment.  
No‑IP provides a stable DNS name (DNS maps a name to an IP address) while the public IP changes.  
Port‑forwarding is the router rule that maps external traffic to an internal machine/port.  
Ports: Frontend HTTPS 443 (host‑mapped), Backend API 8080 (internal only), PostgreSQL 5432 (internal), pgAdmin HTTPS 8443 (IT/admin only).  
Network split mirrors the desired setup: app‑net (frontend ↔ backend), db‑net (backend ↔ DB), admin‑net (pgAdmin ↔ DB).  
The server to network membership table is shown in `docs/Stage A submission/uml/network_split.puml` to make the network split explicit.  
pgAdmin is a web UI for database administration (monitoring, queries, backups), it’s used by IT/DB admins, not end users.
File: `docs/Stage A submission/uml/deployment_current.puml` / `.png`.

What to say:
- “This diagram shows the temporary environment we use today while we wait for the official Windows Server deployment.”

Plain-language summary:
- This shows how we are running the system today on a temporary team PC.

---

# Deep Dive: How the System is Built (for onboarding)

## Backend structure
This is the server side that enforces rules, talks to the database, and exposes the API used by the UI.

**Controllers**
- `BuildingsController`: CRUD (Create, Read, Update, Delete), import/export, logs, building card export.
- `LogsController`: query logs with filters and sorting.
- `StreetsController`: CRUD (Create, Read, Update, Delete) and import/export.
- `UsersController`: user CRUD (Create, Read, Update, Delete), OTP reset.
- `AuthController`: login and OTP validation.
- `SelectTablesController`: serves select‑table options used by drop‑downs.

**Services**
- `BuildingRules`: centralized validation, select‑table mapping, rehab‑status logic.
- `StreetRules`: centralized street validation (unique ID, name required).
- `AuditService`: writes audit entries used in reports and log views.
- `TokenService` and `TwoFactorService`: authentication and OTP logic.
- `ExternalDataService` and `ExternalSyncWorker`: mocked external sync snapshots, planned for real integration.

**Data / Importers**
- `BuildingsExcelImporter`: parses Excel or ZIP (Excel + images), stage‑1 fixes missing fields, stage‑2 resolves duplicates.
- `StreetsExcelImporter`: parses streets Excel with conflict resolution and ID checks.
- `SelectTables`: single source of select‑table values used by UI and validation.

**Models**
- `Building`: main entity, uses `FieldSpecAttribute` to drive labels, categories, and rules.
- `Street`: street entity with `StreetId` key.
- `BuildingLog`: immutable activity log records for audit history.
- `AuditEntry`: general audit events outside the building log.
- `ExternalSystemSnapshot`: saved external payloads from municipal services.
- `AppUser` and `UserRole`: authentication and authorization.

## Frontend structure
This is the browser UI that users interact with, it calls the backend API.

**Pages**
- `BuildingsPage`: filters, table, add/edit/view modals, import/export, card export.
- `LogsPage`: logs table and filters.
- `StreetsPage`: add/edit/view modals, import/export selection.
- `UsersListPage`: add/edit/view modals, OTP reset.
- `TemplateConverterPage`: legacy Excel to new template conversion.

**Components**
- `BuildingModal`: shared modal used for building view and edit.
- `AppLayout`, `RequireAuth`, `RoleGate`: layout and access control.

## How a change flows through the system
1) User edits a building in the UI and clicks “Save.” The UI first checks obvious field issues (required fields, basic formats) so the user gets immediate feedback.
2) Frontend builds a DTO (data transfer object) that contains the edited fields and sends it to the API endpoint in `BuildingsController`.
3) `BuildingsController` loads the current building from the DB and runs server‑side validation using `BuildingRules` plus the field metadata (`FieldSpecAttribute`). This ensures the rules are enforced even if the client is bypassed.
4) If validation passes, the controller applies the changes and calls `AppDbContext.SaveChanges()`, which updates the PostgreSQL database.
5) The controller then creates a `BuildingLog` entry that records what changed (old value vs new value) and stores it via the `AuditService` / log pipeline, keeping an immutable history.
6) When the Logs page is opened, it reads from the log table, so it always shows the exact changes that were saved. Exports (Excel/PPTX) also pull from the latest persisted data, not the UI state.

---

# Presentation Tips (what to emphasize)

- We replaced ad‑hoc, inconsistent rules with **centralized validation** (BuildingRules and StreetRules), so add, edit, import follow the same logic and avoid data drift.
- Import and export are **safe, staged, and repeatable**, we validate first, resolve conflicts, then apply changes, which prevents silent bad data.
- Logs are **immutable** (no delete), which satisfies the client’s audit requirement and keeps a reliable history for investigations.
- The repo is **handoff‑ready**, conventions, templates, and automated issue checks reduce onboarding time and prevent workflow mistakes.
- Remaining Stage‑B risks are mostly **external dependencies** (deployment and integration), not core functionality, which means Stage A is stable.
