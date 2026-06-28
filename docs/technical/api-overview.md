# API Overview

## Overview
The backend exposes a REST API that allows the frontend to authenticate users, manage buildings, view logs, and perform administrative actions.  
All endpoints require authentication, and some require elevated permissions based on the user’s role.

The API is organized into the following modules:
- **Authentication**
- **Users**
- **Buildings**
- **Logs**

All responses use DTOs defined in the project.

---

# Authentication API

## `POST /api/auth/login`
Authenticates a user with email and password.  
Triggers sending a 2FA code.

**Returns:**  
- Temporary token  
- User ID (for OTP step)

---

## `POST /api/auth/verify`
Validates the 2FA code and completes login.

**Returns:**  
- JWT token  
- User info (email, role)

---

## `POST /api/auth/refresh` *(optional if implemented)*  
Refreshes authentication token.

---

# Users API (Admin Only)

## `GET /api/users`
Returns a list of all users.

---

## `GET /api/users/{id}`
Returns details for a specific user.

---

## `POST /api/users`
Creates a new user.  
Admin assigns:
- Email  
- Password  
- Role  

---

## `PUT /api/users/{id}`
Updates a user’s role or other metadata.

---

## `DELETE /api/users/{id}`
Deletes a user.

---

# Buildings API

## `GET /api/buildings`
Returns a list of buildings, with optional filters.

---

## `GET /api/buildings/{id}`
Returns detailed info for a single building.

---

## `POST /api/buildings`  
**Permissions:** Editor, Admin  
Creates a new building record.

---

## `PUT /api/buildings/{id}`  
**Permissions:** Editor, Admin  
Updates building fields.

---

## `DELETE /api/buildings/{id}`  
**Permissions:** Admin  
Deletes a building (soft or hard delete depending on implementation).

---

# Logs API (Admin Only)

## `GET /api/logs`
Returns audit log entries.

**Optional filters:**
- `?userId=`
- `?buildingId=`

---

# Response Format

All endpoints return JSON DTOs, typically containing:
- Identifiers  
- Timestamps  
- Entity-specific metadata

Error responses follow standard patterns:
- `400` invalid data  
- `401` unauthenticated  
- `403` insufficient permissions  
- `404` not found

---

# Security

- All endpoints require a valid JWT token.  
- Admin-only endpoints are protected using role checks in controllers.  
- Sensitive data is never exposed in responses (e.g., passwords).  

---

# Summary
This overview describes the structure of the backend API used by the system.  
Modules are separated clearly, and each follows consistent REST conventions.  
Permissions are enforced at both authentication and authorization levels.

