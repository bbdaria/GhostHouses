# Users & Roles

---

## Overview

The Users module allows the municipality to manage the accounts that access the system.  
Each user has a defined role that controls what actions they can perform.

The system supports three roles:
- **Viewer**
- **Editor**
- **Admin**

Only Admins can modify user roles or access detailed user information.

---

## User List

Admins can access the Users page, which displays:
- Full name  
- Email address  
- Role (Viewer, Editor, Admin)  
- Last login date (if available)

Clicking a user opens the user details page.

---

## User Details

The user profile page contains:
- Name  
- Email  
- Assigned role  
- Account creation date  
- Recent actions (optional shortcut to logs)

Admins can update the user’s role directly from this page.

---

## Roles & Permissions

### **Viewer**
- Can log into the system  
- Can view buildings and building details  
- Cannot create, edit, or delete anything  

### **Editor**
- Everything a Viewer can do  
- Can add new buildings  
- Can edit building details  
- Cannot delete users or buildings  

### **Admin**
- Full permissions across the system  
- Manage all users  
- Change roles  
- Delete buildings  
- View full logs and history  

---

## Creating New Users

Currently, users are added through:
- Database seed on first run  
- Admin panel (if enabled later by the team)  
- Import from external systems (optional extension)

Once created, a user must complete **OTP verification** during login.

---

## Editing User Roles

Only Admins can change a user’s role.

Changes take effect immediately and are tracked in the audit log.

Example scenarios:
- Promote a Viewer to Editor  
- Restrict an Editor to Viewer  
- Grant Admin access to a trusted manager  

---

## Deleting Users

User deletion is restricted to Admins.  
Once deleted:
- The account can no longer log in  
- Past actions remain preserved in the logs  

---

## Authentication Flow Integration

User accounts are part of the login flow:
1. User enters email & password  
2. System checks credentials  
3. A 6-digit OTP is sent  
4. After verification, permissions are loaded according to the user’s role  

---

## Permissions Summary

| Role | View Data | Edit Buildings | Add Buildings | Delete Buildings | Manage Users | View Logs |
|------|-----------|----------------|---------------|------------------|--------------|-----------|
| Viewer | ✔ | ✖ | ✖ | ✖ | ✖ | Limited |
| Editor | ✔ | ✔ | ✔ | ✖ | ✖ | ✔ |
| Admin | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |

---

## Conclusion

The Users module provides a structured permission system that keeps municipal data secure.  
Admins control access levels, while Editors and Viewers operate within safe, predefined boundaries.

---
