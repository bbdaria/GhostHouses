# API Overview

## Overview

GhostHouses exposes an ASP.NET Core REST API used by the React frontend for authentication, building management, streets, logs, users, imports, exports, GIS-backed building cards, and health checks.

The generated OpenAPI/Swagger documentation is the source of truth for request and response schemas.

When Swagger is enabled, it is available at:

- Swagger UI: `/swagger/`
- OpenAPI JSON: `/swagger/v1/swagger.json`

In Docker Compose, Swagger can be enabled or disabled with:

- `SWAGGER_ENABLED=true`
- `SWAGGER_ENABLED=false`

For the municipality test server, Swagger can be enabled on the internal network so information security can inspect the API documentation directly.

## Authentication

Most API endpoints require a JWT bearer token:

```http
Authorization: Bearer <token>
```

Authentication flow:

1. `POST /api/Auth/login`
   - Authenticates username/password.
   - Returns a temporary 2FA challenge token and mocked development 2FA code.

2. `POST /api/Auth/verify-2fa`
   - Validates the 2FA challenge.
   - Returns the JWT access token and authenticated user details.

3. Authenticated requests include the returned JWT in the `Authorization` header.

## Roles

The backend uses role-based authorization policies:

- `Viewer`: read-only workflows.
- `Editor`: create/update/delete operational building and street data.
- `Admin`: user management, imports, exports, and administrative operations.

`Editor` includes `Viewer` access. `Admin` includes `Editor` and `Viewer` access.

## Main Endpoint Groups

### Health

| Method | Endpoint | Auth | Purpose |
|---|---|---|---|
| `GET` | `/api/Health/db` | Public | Verifies database connectivity. |

### Authentication

| Method | Endpoint | Auth | Purpose |
|---|---|---|---|
| `POST` | `/api/Auth/login` | Public | Starts login and 2FA challenge. |
| `POST` | `/api/Auth/verify-2fa` | Public | Completes login and returns JWT. |
| `GET` | `/api/Auth/me` | Viewer | Returns the current authenticated user. |
| `PUT` | `/api/Auth/me` | Viewer | Updates the current user's profile data. |
| `POST` | `/api/Auth/me/password` | Viewer | Changes the current user's password. |

### Buildings

| Method | Endpoint | Auth | Purpose |
|---|---|---|---|
| `GET` | `/api/Buildings` | Viewer | Lists buildings with paging/filtering. |
| `GET` | `/api/Buildings/gis-candidates` | Viewer | Lists buildings that can be shown on GIS maps. |
| `GET` | `/api/Buildings/{id}` | Viewer | Returns detailed building data. |
| `GET` | `/api/Buildings/template` | Viewer | Returns the dynamic building-field template. |
| `GET` | `/api/Buildings/{id}/card` | Viewer | Exports one building card PowerPoint. |
| `POST` | `/api/Buildings/export-cards` | Viewer | Exports selected building cards. |
| `GET` | `/api/Buildings/export` | Admin | Exports buildings table by filters. |
| `POST` | `/api/Buildings/export` | Admin | Exports selected buildings table. |
| `POST` | `/api/Buildings/convert-template` | Admin | Converts legacy building template format to the current import/export format. |
| `POST` | `/api/Buildings/import` | Admin | Imports buildings from a file. |
| `POST` | `/api/Buildings/import/preview` | Admin | Previews building import rows. |
| `POST` | `/api/Buildings/import/validate` | Admin | Validates one building import row. |
| `POST` | `/api/Buildings/import/apply` | Admin | Applies a validated building import. |
| `POST` | `/api/Buildings` | Editor | Creates a building. |
| `PUT` | `/api/Buildings/{id}` | Editor | Updates a building. |
| `PUT` | `/api/Buildings/{id}/fields` | Editor | Updates dynamic building fields. |
| `DELETE` | `/api/Buildings/{id}` | Editor | Removes a building with audit context. |
| `POST` | `/api/Buildings/restore/{logId}` | Editor | Restores a removed building from an audit log snapshot. |

### Streets

| Method | Endpoint | Auth | Purpose |
|---|---|---|---|
| `GET` | `/api/Streets` | Viewer | Lists streets, optionally filtered by search text. |
| `GET` | `/api/Streets/{id}` | Viewer | Returns one street. |
| `POST` | `/api/Streets` | Editor | Creates a street. |
| `PUT` | `/api/Streets/{id}` | Editor | Updates a street. |
| `DELETE` | `/api/Streets/{id}` | Editor | Deletes a street when allowed by validation rules. |
| `GET` | `/api/Streets/export` | Viewer | Exports all streets. |
| `POST` | `/api/Streets/export` | Viewer | Exports selected streets. |
| `POST` | `/api/Streets/import/preview` | Admin | Previews street import rows. |
| `POST` | `/api/Streets/import/validate` | Admin | Validates one street import row. |
| `POST` | `/api/Streets/import/apply` | Admin | Applies a validated street import. |
| `POST` | `/api/Streets/convert-template` | Admin | Converts legacy street template format. |

### Logs

| Method | Endpoint | Auth | Purpose |
|---|---|---|---|
| `GET` | `/api/Logs` | Viewer | Lists building/audit logs with filters. |
| `GET` | `/api/Logs/building/{buildingId}` | Viewer | Lists logs for one building. |
| `POST` | `/api/Logs/building/{buildingId}` | Editor | Creates a log entry for a building. |
| `PUT` | `/api/Logs/{logId}` | Editor | Updates a log entry. |

### Users

All user-management endpoints require `Admin`.

| Method | Endpoint | Purpose |
|---|---|---|
| `GET` | `/api/Users` | Lists users. |
| `GET` | `/api/Users/{id}` | Returns one user. |
| `POST` | `/api/Users` | Creates a user. |
| `PUT` | `/api/Users/{id}` | Updates a user. |
| `POST` | `/api/Users/{id}/reset-2fa` | Resets a user's 2FA state. |
| `POST` | `/api/Users/{id}/password` | Sets a user's password. |

### Select Tables

| Method | Endpoint | Auth | Purpose |
|---|---|---|---|
| `GET` | `/api/select-tables` | Viewer | Returns all select-table option sets. |
| `GET` | `/api/select-tables/{name}` | Viewer | Returns one select-table option set. |

## Response Format

The API returns JSON for normal application requests. File export endpoints return the relevant generated file, such as PowerPoint or spreadsheet content.

Common HTTP responses:

- `200 OK`: successful read/update/export.
- `201 Created`: successful create.
- `400 Bad Request`: invalid request data or validation failure.
- `401 Unauthorized`: missing or invalid JWT.
- `403 Forbidden`: authenticated user lacks the required role.
- `404 Not Found`: requested entity or template was not found.

## Security Notes

- JWT authentication protects the application endpoints.
- Role policies enforce Viewer, Editor, and Admin permissions at the controller level.
- Backend and database services are internal to Docker in the deployment topology.
- Production secrets and certificates are supplied through deployment environment variables or secure handover, not committed to the repository.
- Swagger is controlled by configuration so it can be enabled for the municipality test server and disabled later if required.
