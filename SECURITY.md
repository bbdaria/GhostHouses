# Security Policy

## Supported Scope

Security reports should focus on the delivered GhostHouses system:

- ASP.NET Core backend under `project/web-server/backend`.
- React frontend under `project/web-server/frontend`.
- Docker Compose deployment under `project/docker-compose.yml`.
- Database administration deployment files under `project/db-server`.
- GitHub Actions workflows under `.github/workflows`.

Stage A and Stage B documentation under `docs/` is kept for course submission evidence and is not a production runtime component.

## Reporting a Vulnerability

Please do not disclose vulnerabilities publicly before the maintainers have had time to review and remediate them.

Report suspected vulnerabilities through GitHub Security Advisories when available, or contact the project supervisor/maintainers through the private course and municipality communication channel used for the GhostHouses handoff.

Include:

- Affected component and file path, if known.
- Steps to reproduce.
- Expected and actual behavior.
- Potential impact.
- Suggested remediation, if available.

## Security Review Status

The repository includes automated build/test checks, CodeQL static analysis configuration, and Dependabot configuration. These controls help detect source-code and dependency issues early, but they do not replace a full manual security code review or an application penetration test on a running deployment.

The delivered system uses mocked two-factor authentication for handoff. A production deployment should replace the mocked OTP provider through the existing `ITwoFactorService` boundary and rotate all deployment secrets before use.

## Secret Handling

Do not commit secrets, `.env` files, TLS private keys, tokens, passwords, or municipality credentials.

Local deployment secrets belong in:

- `project/.env`
- `project/certs/dev.crt`
- `project/certs/dev.key`

These paths are excluded by `.gitignore` and must be exchanged only through an approved private channel.
