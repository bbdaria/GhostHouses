# GhostHouses Team Conventions

This document defines the team’s working conventions for the GhostHouses project.
It is written in English and kept in the repo to stay versioned with the code.

## 1) General
- Keep documentation and communication in English unless the UI text must be Hebrew.
- Prefer consistency with existing patterns over introducing new styles or tools.

## 2) Repository Structure
- `project/web-server/backend`: ASP.NET Core backend.
- `project/web-server/frontend`: React frontend.
- `docs/`: project documentation, organized by purpose.
- `docs/technical/`: current technical/project documentation.
- `docs/data-and-templates/`: Excel/PPTX templates and sample/reference data.
- `docs/hld/`: original HLD material.
- `docs/notes/`: meeting/client notes.
- `docs/submissions/stage-a/`: Stage A submission, rehearsal, and UML artifacts.
- `tests/`: automated tests (if/when added).

## 3) Coding Conventions
### Backend (C# / ASP.NET Core)
- Use PascalCase for public members, camelCase for locals/parameters.
- DTOs live in `Dtos/` (if present) and represent API contracts only.
- Controllers should stay thin; prefer moving logic into services/helpers.
- Use explicit null checks where needed to avoid runtime errors.
- Keep API responses consistent (status codes + error messages).

### Frontend (React)
- Use functional components and hooks.
- Keep components focused; extract helpers when logic grows.
- Follow existing RTL layout and Hebrew labels where required.
- Avoid introducing new UI libraries without team approval.

### UI/RTL
- Labels are Hebrew, but code/comments remain English.
- Maintain RTL alignment and spacing consistency.

## 4) Git Workflow
- Permanent branches: `main` and `develop`.
- `main` is reserved for final production/handover history. Do not merge directly into `main` until final approval.
- `develop` is the integration branch for delivered work.
- Feature group branches use `feature/<feature>/main`.
  Example: `feature/gis/main`.
- Issue branches use `feature/<feature>/#<issue-number>-<short-slug>`.
  Example: `feature/gis/#88-gis-map-page`.
- Release checkpoint branches use `release/stage-a/sprint-<number>-<name>` or `release/stage-b/sprint-<number>-<name>`.
  Example: `release/stage-b/sprint-2-gis`.
- One implementation issue per issue branch. No mixing unrelated changes.
- Commit messages must mention the issue number so it links in GitHub.
  Example: `Issue #19: add conventions document`.
- Normal flow: `develop` -> `feature/<feature>/main` -> `feature/<feature>/#<issue>-<slug>` -> `feature/<feature>/main` -> `develop`.
- Release branches are checkpoints from `develop`; they are not used for day-to-day development.
- Merge to `develop` only after the issue is **Done** and approved.
- If an issue is **Canceled**, close it and do not merge.
- Issue guard enforces that non‑User‑Story issues have exactly one linked Development branch matching `feature/<feature>/#<issue-number>-<short-slug>`.
- User Story issues must not have a linked Development branch.
- Proof or scalability branches that should not reach production, such as the OpenStreetMap provider proof, must stay outside `develop`, `release/*`, and `main`.

## 5) Issue Management
### Automated checks (comment-only)
- Issue guard comments when **Label** or **Milestone** is missing.
- Issue guard comments when **Project (GhostHouses)** or **Status** is missing.
- Issue guard comments if an issue is linked to any project other than **GhostHouses**.
- For **non‑User Story** issues, issue guard comments if **Parent** is missing or not a User Story.
- For **User Story** issues, issue guard comments if extra labels are present.
- On **User Story close**, issue guard comments if any child issues remain open.
- On **child reopen**, issue guard comments on both child + parent if the parent User Story is closed.
- Issue guard comments if **Status** is Done/Canceled while the issue is open, or if the issue is closed while **Status** is not Done/Canceled.
- Issue guard runs on issue events and once daily (scheduled). It uses a PAT secret `ISSUE_GUARD_PAT` (classic scopes: `repo`, `read:org`, `project`) to read GhostHouses project data.

### User Story Issues
- Must be labeled `User Story` **only** (no additional labels).
- Describe the user value in plain language.
- Body must start with `## Acceptance Criteria` and include at least one checkbox item.
  Example:
  ```
  ## Acceptance Criteria
  - [ ] When a duplicate address is submitted, the user sees a clear warning.
  - [x] Exporting buildings generates an Excel file with headers.
  ```
- If the User Story is **open**, at least one Acceptance Criteria checkbox must be **unchecked**.
- If the User Story is **closed** and Status is **Done**, **all** Acceptance Criteria checkboxes must be **checked**.
- If the User Story is **closed** and Status is **Canceled**, its Acceptance Criteria should still be checked, but the text should describe the cancellation decision or the replacement approach instead of pretending the original feature was implemented.
- Do **not** set a parent issue (they are the parent).
- Can have multiple implementation issues linked as sub-issues.
- Must still include milestone, status, and time-tracked updates.
- Must be in the **GhostHouses** project with a **Status** value.
- Must not have any Development branch linked.
- If a child issue is reopened, the User Story should be reopened.
- If **any** child issue is still open, the User Story must remain open (cannot be Done/Closed).
- If **all** child issues are closed, the User Story **may** stay open if more work is expected.

### Implementation Issues (Non‑User Story)
- Must have a **User Story** parent issue (and that parent must be labeled `User Story`).
- Title and body must be clear and specific.
- Body must use exactly these top-level sections:
  ```
  ## Summary
  ...

  ## Scope
  - ...

  ## Notes
  - ...
  ```
- Do not write `Parent User Story` inside the body. Use GitHub's parent/sub-issue relationship instead.
- Milestone is required.
- Labels are required.
- Must be in the **GhostHouses** project with a **Status** value.
- Must have **exactly one** linked Development branch named `feature/<feature>/#<issue-number>-<short-slug>`.
- Time-tracked updates are required on every progress comment.
- A closed non-User-Story issue must have at least one closing/progress comment with a short description line followed by `Time spent: ...`.
  Example:
  ```
  Implemented the filtered Excel export and verified that the exported file matches the visible table data.

  Time spent: 2 hours
  ```
- Cross-cutting work should be linked to the correct User Story. For example, deployment work belongs under a deployment User Story, while GIS or external-system integration work belongs under the relevant map/integration User Story.

### Status Usage (Project)
- **Backlog**: not planned for the current sprint.
- **Current Sprint**: planned to be picked up this sprint.
- **Doing**: actively being worked on.
- **Candidate**: finished, waiting approval.
- **Done**: finished and approved (merge to `develop`). Non‑User Story issues moved to Done must have an assignee.
- **Canceled**: canceled (close the issue). Assignee not required.

### Updates and Time Tracking
- Every update comment must include time spent (e.g., “Time spent: ~2 hours”).

### Blocking / Blocked
- If work is blocked, add a **Blocked by:** section in the issue body with links.
- If an issue blocks others, add a **Blocks:** section.
- Create missing issues when needed instead of tracking work in comments only.

## 6) Approvals
- Approval is by the team supervisor or by majority of the team.
- Issues move from Candidate → Done only after approval.

## 7) Documentation Standards
- Keep docs in `docs/`, under the folder that matches their purpose.
- Put stable technical documentation in `docs/technical/`.
- Put import/export samples and templates in `docs/data-and-templates/`.
- Put HLD material in `docs/hld/`.
- Put meeting/client notes in `docs/notes/`.
- Put course submission artifacts in `docs/submissions/<stage>/`.
- If a document is required for submission, keep the source in Markdown when possible
  and export to other formats only if needed by the course.
