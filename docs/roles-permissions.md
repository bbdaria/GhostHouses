# Roles and Permissions

## Overview
The system assigns each user a specific role that determines what actions they are allowed to perform.  
Role-based permissions ensure that sensitive operations—such as modifying buildings or managing users—are only accessible to authorized personnel.

There are three main roles: **Viewer**, **Editor**, and **Admin**.

---

## Roles

### **Viewer**
The most basic role.  
Viewers can:
- View the list of buildings  
- View building details  

Viewers cannot modify any data.

---

### **Editor**
Editors have permissions to update building information.  
Editors can:
- View all buildings  
- Create new buildings  
- Edit building details  
- Update building status  

Editors cannot:
- Delete buildings  
- Manage users  
- View system logs

---

### **Admin**
The highest-level role with full system access.  
Admins can:
- Perform all Viewer and Editor actions  
- Delete buildings  
- Manage users (create, edit roles, delete)  
- View system logs (audit records)  
- Oversee system-wide operations  

---

## Permission Matrix

| Action                      | Viewer | Editor | Admin |
|-----------------------------|--------|--------|-------|
| View buildings             | ✓      | ✓      | ✓     |
| View building details      | ✓      | ✓      | ✓     |
| Create building            | ✗      | ✓      | ✓     |
| Edit building              | ✗      | ✓      | ✓     |
| Delete building            | ✗      | ✗      | ✓     |
| Manage users               | ✗      | ✗      | ✓     |
| View audit logs            | ✗      | ✗      | ✓     |

---

## Frontend Enforcement

### **RequireAuth**
Ensures that only authenticated users can access the application.

### **RoleGate**
Controls the visibility of UI elements and pages based on role.  
Examples:
- “Add Building” button appears only for Editors/Admins.  
- “Manage Users” page appears only for Admins.  
- Logs page is hidden for non-admin roles.  

---

## Backend Enforcement

Authorization is also enforced in controllers using role checks.  
Even if a user manipulates the frontend, the backend prevents unauthorized operations.

Examples:
- Creating a building requires Editor or Admin role.  
- Deleting a building requires Admin role.  
- Viewing logs requires Admin role.

This ensures security at the API level.

---

## Summary
Roles and permissions define a clear structure of user capabilities:  
- **Viewers** can only read data.  
- **Editors** can modify building data.  
- **Admins** control the entire system.  

Both frontend and backend enforce these rules to maintain a secure and well-structured workflow.

