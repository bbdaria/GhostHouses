# GhostHouses Team Conventions

This document defines the team’s working conventions for the GhostHouses project.
It is written in English and kept in the repo to stay versioned with the code.

## 1) General
- Keep documentation and communication in English unless the UI text must be Hebrew.
- Prefer consistency with existing patterns over introducing new styles or tools.

## 2) Repository Structure
- `project/web-server/backend`: ASP.NET Core backend.
- `project/web-server/frontend`: React frontend.
- `docs/`: project documentation and submission files.
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
- Branch naming: `Issues/#<issue-number>-<short-slug>` (example: `Issues/#19-convention-doc`).
- One issue per branch. No mixing unrelated changes.
- Commit messages must mention the issue number so it links in GitHub.
  Example: `Issue #19: add conventions document`.
- Merge to `dev` only after the issue is **Done** and approved.
- If an issue is **Canceled**, close it and do not merge.

## 5) Issue Management
### Automated checks (comment-only)
- Issue guard comments when **Label** or **Milestone** is missing.
- For **non‑User Story** issues, issue guard comments if **Parent** is missing or not a User Story.
- For **User Story** issues, issue guard comments if extra labels are present.
- On **User Story close**, issue guard comments if any child issues remain open.
- On **child reopen**, issue guard comments on both child + parent if the parent User Story is closed.

### User Story Issues
- Must be labeled `User Story` **only** (no additional labels).
- Describe the user value in plain language.
- Do **not** set a parent issue (they are the parent).
- Can have multiple implementation issues linked as sub-issues.
- Must still include milestone, status, and time-tracked updates.
- Must not have any Development branch linked.
- If a child issue is reopened, the User Story should be reopened.
- If **any** child issue is still open, the User Story must remain open (cannot be Done/Closed).
- If **all** child issues are closed, the User Story **may** stay open if more work is expected.

### Implementation Issues (Non‑User Story)
- Must have a **User Story** parent issue (and that parent must be labeled `User Story`).
- Title and body must be clear and specific.
- Milestone is required.
- Labels are required.
- Must have a linked Development branch.
- Time-tracked updates are required on every progress comment.

### Status Usage (Project)
- **Backlog**: not planned for the current sprint.
- **Current Sprint**: planned to be picked up this sprint.
- **Doing**: actively being worked on.
- **Candidate**: finished, waiting approval.
- **Done**: finished and approved (merge to `dev`).
- **Canceled**: canceled (close the issue).

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
- Keep docs in `docs/`.
- If a document is required for submission, keep the source in Markdown when possible
  and export to other formats only if needed by the course.
