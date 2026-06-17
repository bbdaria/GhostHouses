# Authentication and Authorization

---

## Overview

The system uses secure authentication and role-based authorization to control access to municipal data about abandoned buildings.  
Users must successfully complete two stages before entering the system:

1. **Login with email and password**  
2. **Two-Factor Authentication (OTP)**  

Once authenticated, the user’s role determines which pages and actions they are allowed to access.

---

## Login Flow

### Step 1 — Credentials  
Users enter:
- Email  
- Password  

The backend verifies the credentials using the `AuthController`.

If valid, the system sends an OTP code to the user’s email.

---

### Step 2 — One-Time Password (OTP)
The user is redirected to the OTP page.  
They must enter the 6-digit code emailed to them.

If the OTP is correct:
- A JWT token is generated  
- The user is logged in  
- All subsequent API calls include the token  

If incorrect:
- The user receives an error  
- A new OTP may be requested  

---

## Roles and Permissions

The system includes three main roles:

### **Viewer**
- View buildings  
- View building details  

### **Editor**
- All Viewer permissions  
- Create new buildings  
- Edit building information  
- Delete buildings (optional depending on policy)  

### **Admin**
- Full system access  
- Manage users  
- View system logs  
- Assign roles  
- Access all buildings and audit activity  

---

## Authorization Enforcement

The frontend enforces access using:

- **RequireAuth**  
  Redirects unauthenticated users to the login page.

- **RoleGate**  
  Controls visibility of buttons, pages, and admin features.

The backend enforces authorization using attributes and manual role checks inside controllers.

This ensures users cannot bypass restrictions even if they manipulate the frontend.

---

## Token Handling

The system uses **JWT tokens**:
- Stored in memory (not localStorage) to avoid security issues  
- Automatically attached to API calls by the frontend client  
- Verified on every backend request  

Tokens encode:
- User ID  
- User role  
- Expiration time  

If a token expires, the user must log in again.

---

## Security Notes

- Passwords are stored hashed (never in plain text).  
- OTP codes expire quickly and cannot be reused.  
- Audit logs record important actions (edits, deletions, user changes).  

---

## Conclusion

The authentication flow ensures only authorized municipal employees gain access.  
Role-based permissions guarantee each user can only perform actions appropriate to their responsibilities.

---
