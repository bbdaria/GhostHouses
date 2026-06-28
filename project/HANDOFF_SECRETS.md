# Handoff Secrets and Local Files

This repository does not contain production secrets or local TLS keys.

The deployment maintainer must receive these files through the team shared drive or secure mail:

- `.env`
- `certs/dev.crt`
- `certs/dev.key`

Place the files here before running Docker Compose:

```text
project/.env
project/certs/dev.crt
project/certs/dev.key
```

Use `project/.env.example` as the template for `.env`. The real `.env` must contain the PostgreSQL credentials, pgAdmin credentials, and JWT signing key.

These paths are intentionally ignored by Git:

- `project/.env`
- `project/certs/`
- `*.tar`

The deployment server is expected to have outbound internet access so Docker can pull base images and build dependencies from the public registries.

Clean run from `project/`:

```bash
docker compose down -v && docker compose up -d --build
```

Do not commit real secrets or generated private keys.

On a clean database, the backend creates one initial administrator account:

- Username: `admin`
- Password: `admin`

Give this account only to the department owner during handoff. They should sign in, create the real municipality users, and then change or disable the initial admin account according to their internal policy.
