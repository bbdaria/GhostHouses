# Deployment Security Readiness

This note summarizes the repository security-readiness work prepared for deployment and handoff.

## Security-Readiness Context

Static code analysis, dependency monitoring, and GitHub security controls are useful preparation for reviewing a system before deployment. They help catch common code, dependency, and secret-handling issues early, but they are not substitutes for a full security code review or an application penetration test on a running system.

A practical security-readiness sequence is:

1. Keep a runnable backend environment that can be reviewed and tested.
2. Run static code and dependency analysis.
3. Review and triage findings.
4. Later, perform an application penetration test on the complete deployed system.

## Implemented Repository Controls

| Control | Status | Evidence |
| --- | --- | --- |
| Backend build and tests | Implemented | `.github/workflows/ci.yml` |
| Frontend production build | Implemented | `.github/workflows/ci.yml` |
| Docker Compose build readiness | Implemented | `.github/workflows/ci.yml` |
| CodeQL static analysis configuration | Implemented | `.github/workflows/codeql.yml` |
| Dependabot update configuration | Implemented | `.github/dependabot.yml` |
| Vulnerability reporting policy | Implemented | `SECURITY.md` |
| Secret exclusion from Git | Implemented | `.gitignore` excludes `.env`, `.env.*`, and `project/certs/` |

## GitHub Settings That Require Repository Administration

These controls are recommended by the security PDFs but must be enabled or verified in GitHub repository settings by a repository administrator or owner:

| Control | Required action |
| --- | --- |
| Dependabot alerts | Enable under repository security settings. |
| Dependabot security updates | Enable under repository security settings after `.github/dependabot.yml` is merged. |
| Code scanning alerts | Verify that the CodeQL workflow has run and alerts are visible under the Security tab. |
| Secret scanning | Enable for the public repository where supported. |
| Push protection | Enable to block accidental secret pushes. |
| Branch protection / rulesets | Protect `main`, `develop`, and release branches and require passing CI/security checks before merge. |
| Repository visibility justification | Document why the course project remains public, or make it private if the municipality requires that. |
| 2FA for collaborators | Verify that all contributors use two-factor authentication. |

## Static Analysis Scope

CodeQL is configured for:

- C# backend code.
- JavaScript/TypeScript frontend code.

Dependabot is configured for:

- GitHub Actions.
- NuGet packages in the backend.
- npm packages in the frontend.
- Docker base images in the devcontainer Dockerfiles.

## Verification Commands

If `dotnet` and `npm` are installed locally, these checks can be run from the repository root:

```bash
dotnet test tests/WebServer.Tests/WebServer.Tests.csproj --configuration Release
```

```bash
cd project/web-server/frontend
npm ci
npm run build
```

```bash
cd project
docker compose config
docker compose build backend frontend
```

For dependency vulnerability checks:

```bash
dotnet list project/web-server/backend/WebServer.csproj package --vulnerable --include-transitive
```

```bash
cd project/web-server/frontend
npm audit
```

## Verification Results

The following checks were run on July 2, 2026 from WSL using Docker, because the local WSL environment did not have `dotnet` or `npm` installed directly. Docker Compose was given CI-safe dummy environment variables directly through the command environment.

| Check | Result |
| --- | --- |
| Backend tests through .NET SDK container | Passed: 19 tests, 0 failed |
| NuGet vulnerability check through .NET SDK container | Passed: no vulnerable packages reported |
| Frontend npm audit from `package-lock.json` through Node container | Passed: 0 vulnerabilities |
| Docker Compose configuration validation | Passed |
| Docker Compose backend/frontend image build | Passed |

Commands used:

```bash
docker run --rm -v "$PWD:/src" -w /src mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet test tests/WebServer.Tests/WebServer.Tests.csproj --configuration Release
```

```bash
docker run --rm -v "$PWD:/src" -w /src mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet list project/web-server/backend/WebServer.csproj package --vulnerable --include-transitive
```

```bash
docker run --rm -v "$PWD/project/web-server/frontend:/app" -w /app node:20 \
  npm audit --package-lock-only
```

```bash
cd project
POSTGRES_DB=ghosthouses \
POSTGRES_USER=ghosthouses_ci \
POSTGRES_PASSWORD=ghosthouses-ci-password \
PGADMIN_DEFAULT_EMAIL=ci@example.com \
PGADMIN_DEFAULT_PASSWORD=ghosthouses-ci-pgadmin-password \
JWT_ISSUER=ghosthouses \
JWT_AUDIENCE=ghosthouses-clients \
JWT_SIGNING_KEY=ghosthouses-ci-signing-key-at-least-32-chars \
JWT_EXPIRATION_MINUTES=120 \
docker compose config >/dev/null
```

```bash
cd project
POSTGRES_DB=ghosthouses \
POSTGRES_USER=ghosthouses_ci \
POSTGRES_PASSWORD=ghosthouses-ci-password \
PGADMIN_DEFAULT_EMAIL=ci@example.com \
PGADMIN_DEFAULT_PASSWORD=ghosthouses-ci-pgadmin-password \
JWT_ISSUER=ghosthouses \
JWT_AUDIENCE=ghosthouses-clients \
JWT_SIGNING_KEY=ghosthouses-ci-signing-key-at-least-32-chars \
JWT_EXPIRATION_MINUTES=120 \
docker compose build backend frontend
```

## Known Limitations

- Mocked two-factor authentication remains in the delivered handoff version.
- Static analysis is not a penetration test.
- Security review is most useful when the backend is running and testable.
- Customer-side deployment still depends on municipality VM, Docker, WSL/nested-virtualization, and security/IT approval.
- Real production secrets and certificates must be supplied through an approved private handoff channel, not through Git.
