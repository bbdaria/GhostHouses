# System Design

---

## Overview

The GhostHouses system is a web platform designed for municipalities to
locate, document, update, and manage abandoned buildings.  
The system provides secure authentication (including 2FA), role-based access,
building search and filtering, detailed building pages, update logs, and tools
to modify or delete records according to user permissions.

The project is built with a **React frontend** and an **ASP.NET Core backend**,  
connected to a SQL database and optionally synchronized with external systems.

---

## Architecture

The system uses a standard client–server structure:

- **Frontend (React + Vite)**
  - UI components, pages, and hooks  
  - Handles login, 2FA verification, search, building pages, and admin actions  
  - Communicates with backend API using a modular API layer

- **Backend (ASP.NET Core Web API)**
  - Authentication / JWT / 2FA  
  - CRUD operations for buildings  
  - User management & permissions  
  - Audit logging  
  - External data synchronization worker  
  - Centralized services for clean logic separation

- **Database**
  - Stores users, buildings, statuses, audit entries, and snapshots  
  - Managed through Entity Framework

---

## Main Components

### **1. Authentication & Authorization**
- Email + password login  
- Time-based one-time code (2FA)  
- JWT token issued after successful verification  
- Role-based route access (Admin, Editor, Viewer)  

Used across the whole system to ensure sensitive data is protected.

---

### **2. Buildings Module**
Handles all abandoned-building records:
- List & search buildings  
- Filter by city, status, or other fields  
- View full building details  
- Add, edit, and delete buildings (role-restricted)  
- Logs every update for auditing  

This is the core feature of the platform.

---

### **3. Users & Roles**
Admins can:
- View the list of users  
- Inspect user details  
- Change roles  
- Manage permissions  

The frontend controls access through **RequireAuth** + **RoleGate**.

---

### **4. Audit Logs**
Every important action produces an audit entry:
- Building edits  
- Role changes  
- Login attempts  
- Deletions  

Admins can view logs in the Logs page.

---

### **5. External System Sync (Optional)**
The backend includes a background worker that:
- Fetches data from an external provider  
- Saves snapshots  
- Updates internal records if needed  

This keeps the system aligned with external databases if required.

---

## Data Flow

1. User logs in → backend validates → returns token  
2. User completes 2FA → receives access token  
3. Frontend stores token and sends it with all API requests  
4. Backend checks permissions on every request  
5. Database operations run through services and EF models  
6. Audit entry is written for each change  
7. Response returns to frontend for UI updates

---

## Frontend Structure

- **pages/**  
  BuildingsPage, LoginPage, UsersListPage, LogsPage, OtpPage, etc.

- **components/**  
  Reusable UI components (tables, forms, cards).

- **context/**  
  Authentication context with user and role state.

- **api/**  
  All backend API calls in one place.

- **hooks/**  
  Custom logic (document title, form helpers, etc.)

---

## Backend Structure

- **Controllers**  
  Handle API requests (Auth, Buildings, Users, Logs)

- **Services**  
  Core logic (TokenService, TwoFactorService, AuditService, etc.)

- **Models**  
  Entities, DTOs, status objects, snapshots

- **Data**  
  Database context and seed data

- **Utilities**  
  Background workers and helper classes

This structure keeps the project clean and maintainable.

---

## Conclusion

The system is designed for clarity, security, and easy municipal operation.  
Each module is separated to simplify future maintenance and team development.

---
